using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Full 3D Unity CharacterController FPS Player with VALORANT-style tactical gunplay:
    /// 5 distinct firearms (Vandal, Phantom, Judge, Odin w/ RPM spin-up, Operator w/ full scope),
    /// crisp 1-tap headshots, per-weapon tactical reloads ([R]), inspect ([Y]), 3D melee ([V]),
    /// Special Abilities ([X]), Abyssal Phase-Dash ([SHIFT]), and Chrono-Thermal Overdrive ([Q/E]).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSPlayerController : MonoBehaviour
    {
        public static FPSPlayerController Instance { get; private set; }

        public Camera playerCamera;
        public WeaponViewmodel3D viewmodel;

        // Player Vitals & Economy
        public int hp = 100;
        public int maxHp = 100;
        public int armor = 50;
        public int maxArmor = 100;
        public int credits = 500;
        public int shells = 32;
        public int maxShells = 64;
        public int cells = 200;
        public int maxCells = 400;

        public List<WeaponDef> weapons;
        public List<MeleeDef> melees;
        public List<SpecialAbilityDef> abilities;
        public int[] mags;
        public int weaponIndex = 0;
        public int meleeIndex = 0;
        public string equippedAbility = "nova";
        public Dictionary<string, int> abilityCharges = new Dictionary<string, int>
        {
            { "nova", 1 }, { "aegis", 0 }, { "meteor", 0 }
        };

        public bool IsADS { get; private set; }
        public bool IsSniperScoped => IsADS && weapons != null && weaponIndex < weapons.Count && weapons[weaponIndex].isSniper && !IsReloading;
        public bool IsReloading { get; private set; }
        public float DashCooldownRatio => Mathf.Clamp01(1f - dashCooldown / 1.4f);
        public bool DashReady => dashCooldown <= 0f;
        public float HitmarkerTimer { get; private set; }
        public bool LastHitWasHeadshot { get; private set; }
        public float DamageFlashTimer { get; private set; }
        public float AcidFlashTimer { get; private set; }
        public float AcidBurnTimer { get; private set; }
        public bool IsAcidBurned => AcidBurnTimer > 0f;
        public float PickupFlashTimer { get; private set; }
        public float DashFlashTimer { get; private set; }

        private float acidTickTimer;
        private int acidTickDamage = 4;
        private string acidAttacker = "DEMON ACID";

        private CharacterController controller;
        private float yaw;
        private float pitch;
        private float verticalVel;
        private float bobPhase;

        private float fireCooldown;
        private float odinSpinup; // Ramps Odin RPM just like Valorant!
        private float meleeCooldown;
        private float dashCooldown;
        private float dashTimer;
        private Vector3 dashVel;
        private float invulnTimer;
        private float vampiricTimer;
        private float reloadTimer;
        private float activeReloadDuration = 1.55f;
        private bool prevADS;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            weapons = GameCatalog.CreateDefaultWeapons();
            melees = GameCatalog.CreateDefaultMelees();
            abilities = GameCatalog.CreateDefaultAbilities();
            ResetLoadoutMags();
        }

        public void ResetLoadoutMags()
        {
            mags = new int[weapons.Count];
            for (int i = 0; i < weapons.Count; i++) mags[i] = weapons[i].magSize;
        }

        public void ResetForNewRun()
        {
            hp = 100;
            armor = 50;
            credits = Mathf.Max(credits, 500);
            shells = 32;
            cells = 200;
            ResetLoadoutMags();
            weaponIndex = 0;
            IsReloading = false;
            IsADS = false;
            vampiricTimer = 0f;
            invulnTimer = 0f;
            odinSpinup = 0f;
            AcidBurnTimer = 0f;
            AcidFlashTimer = 0f;
            acidTickTimer = 0f;
            SelectWeapon(0);
            TeleportToSpawn();
        }

        public void TeleportToSpawn()
        {
            if (controller != null) controller.enabled = false;
            transform.position = new Vector3(0f, 1.05f, 13f);
            yaw = 180f;
            pitch = -2f;
            verticalVel = 0f;
            if (controller != null) controller.enabled = true;
            ApplyCameraTransform();
        }

        public void SelectWeapon(int idx)
        {
            if (idx < 0 || idx >= weapons.Count) return;
            if (!weapons[idx].owned)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.AddKillfeed($"LOCKED: {weapons[idx].name} - PRESS [B] TO BUY IN SHOP!");
                return;
            }
            weaponIndex = idx;
            IsReloading = false;
            odinSpinup = 0f;
            if (viewmodel != null) viewmodel.SetActiveWeaponModel(idx);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayEquip();
        }

        private void Update()
        {
            if (HitmarkerTimer > 0f) HitmarkerTimer -= Time.deltaTime;
            if (DamageFlashTimer > 0f) DamageFlashTimer -= Time.deltaTime;
            if (AcidFlashTimer > 0f) AcidFlashTimer -= Time.deltaTime;
            if (PickupFlashTimer > 0f) PickupFlashTimer -= Time.deltaTime;
            if (DashFlashTimer > 0f) DashFlashTimer -= Time.deltaTime;

            if (GameManager.Instance == null || !GameManager.Instance.IsRunning || GameManager.Instance.IsPaused)
            {
                IsADS = false;
                return;
            }

            float dt = Time.deltaTime;
            if (fireCooldown > 0f) fireCooldown -= dt;
            if (meleeCooldown > 0f) meleeCooldown -= dt;
            if (dashCooldown > 0f) dashCooldown -= dt;
            if (invulnTimer > 0f) invulnTimer -= dt;
            if (vampiricTimer > 0f) vampiricTimer -= dt;

            // Toxic Demon Acid Burn Damage-Over-Time (drains player health continuously!)
            if (AcidBurnTimer > 0f)
            {
                AcidBurnTimer -= dt;
                acidTickTimer -= dt;
                if (acidTickTimer <= 0f)
                {
                    acidTickTimer = 0.70f;
                    if (invulnTimer <= 0f)
                    {
                        hp = Mathf.Max(0, hp - acidTickDamage);
                        AcidFlashTimer = 0.30f;
                        if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayDamage();
                        if (hp <= 0)
                        {
                            AcidBurnTimer = 0f;
                            GameManager.Instance.TriggerGameOver($"DISSOLVED BY {acidAttacker}'S TOXIC ACID");
                            return;
                        }
                    }
                }
            }

            if (IsReloading)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f) FinishReload();
            }

            HandleLook(dt);
            HandleMovement(dt);
            HandleCombatInput(dt);

            // Smooth FOV zoom on Right-Click ADS / Operator Scope
            if (playerCamera != null)
            {
                float targetFov = IsADS ? weapons[weaponIndex].adsFov : 70f;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, dt * 16f);
            }

            if (viewmodel != null)
            {
                float rProg = IsReloading ? 1f - (reloadTimer / Mathf.Max(0.1f, activeReloadDuration)) : 0f;
                viewmodel.UpdateViewmodel(dt, IsADS, IsSniperScoped, IsReloading, rProg, bobPhase);
            }

            if (GameManager.Instance.BeaconUnlocked)
            {
                float distToPortal = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(0f, -30.5f));
                if (distToPortal < 3.6f)
                {
                    GameManager.Instance.OnPortalEntered();
                }
            }
        }

        private void HandleLook(float dt)
        {
            float sens = IsSniperScoped ? 0.65f : (IsADS ? 1.35f : 2.3f);
            yaw += Input.GetAxis("Mouse X") * sens;
            pitch -= Input.GetAxis("Mouse Y") * sens;

            if (Input.GetKey(KeyCode.LeftArrow)) yaw -= 110f * dt;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += 110f * dt;

            pitch = Mathf.Clamp(pitch, -72f, 72f);
            ApplyCameraTransform();
        }

        private void ApplyCameraTransform()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (playerCamera != null)
            {
                float bobY = Mathf.Sin(bobPhase) * (IsADS ? 0.012f : 0.042f);
                playerCamera.transform.localPosition = new Vector3(0f, 0.72f + bobY, 0f);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement(float dt)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                TriggerDash();
            }

            Vector3 horizMove;
            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                horizMove = dashVel;
            }
            else
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 dir = (transform.forward * v + transform.right * h).normalized;
                float speed = (GameManager.Instance.OverdriveActive ? 13.5f : 10.2f) * (IsADS ? 0.65f : 1.0f);
                horizMove = dir * speed;
                if (dir.sqrMagnitude > 0.01f)
                {
                    bobPhase += dt * (IsADS ? 6.5f : 11.5f);
                }
            }

            if (controller.isGrounded && verticalVel < 0f) verticalVel = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded) verticalVel = 7.8f;
            verticalVel -= 19.5f * dt;

            controller.Move((horizMove + Vector3.up * verticalVel) * dt);

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -23.0f, 23.0f);
            p.z = Mathf.Clamp(p.z, -32.8f, 18.2f);
            if (p.y < 1.0f) p.y = 1.0f;
            transform.position = p;
        }

        private void HandleCombatInput(float dt)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectWeapon(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SelectWeapon(4);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.05f)
            {
                int dir = scroll > 0 ? 1 : -1;
                SelectWeapon((weaponIndex + dir + weapons.Count) % weapons.Count);
            }

            IsADS = Input.GetMouseButton(1) && !IsReloading;
            if (IsADS != prevADS)
            {
                prevADS = IsADS;
                if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayADS(IsADS);
            }

            if (Input.GetKeyDown(KeyCode.R)) StartReload();
            if (Input.GetKeyDown(KeyCode.Y) && viewmodel != null && !IsReloading) viewmodel.TriggerInspect();
            if (Input.GetKeyDown(KeyCode.V)) TriggerMeleeAttack();
            if (Input.GetKeyDown(KeyCode.X)) TriggerSpecialAbility();
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
                GameManager.Instance.TriggerAscensionOverdrive();

            WeaponDef w = weapons[weaponIndex];
            bool wantsFire = w.isAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

            if (w.isOdinSpinup && Input.GetMouseButton(0) && !IsReloading)
                odinSpinup = Mathf.Min(1f, odinSpinup + dt * 1.1f);
            else
                odinSpinup = Mathf.Max(0f, odinSpinup - dt * 1.8f);

            if (wantsFire) AttemptFire();
        }

        private void TriggerDash()
        {
            if (dashCooldown > 0f) return;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = (transform.forward * v + transform.right * h);
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dashVel = dir.normalized * 25f;
            dashTimer = 0.2f;
            invulnTimer = Mathf.Max(invulnTimer, 0.28f);
            dashCooldown = 1.4f;
            DashFlashTimer = 0.2f;
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayDash();
        }

        public void StartReload()
        {
            if (IsReloading) return;
            WeaponDef w = weapons[weaponIndex];
            if (mags[weaponIndex] >= w.magSize) return;
            if (w.ammoType == "shells" && shells <= 0) { GameManager.Instance.AddKillfeed("OUT OF SHELLS / AP SLUGS!"); return; }
            if (w.ammoType == "cells" && cells <= 0) { GameManager.Instance.AddKillfeed("OUT OF ODIN BELT CELLS!"); return; }

            IsReloading = true;
            activeReloadDuration = w.reloadTime;
            reloadTimer = activeReloadDuration;
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayReload(w.id);
        }

        private void FinishReload()
        {
            WeaponDef w = weapons[weaponIndex];
            int needed = w.magSize - mags[weaponIndex];
            if (w.ammoType == "infinite") mags[weaponIndex] = w.magSize;
            else if (w.ammoType == "shells") { int take = Mathf.Min(needed, shells); shells -= take; mags[weaponIndex] += take; }
            else if (w.ammoType == "cells") { int take = Mathf.Min(needed, cells); cells -= take; mags[weaponIndex] += take; }
            IsReloading = false;
        }

        private void AttemptFire()
        {
            if (fireCooldown > 0f || IsReloading) return;
            WeaponDef w = weapons[weaponIndex];

            if (mags[weaponIndex] <= 0)
            {
                StartReload();
                return;
            }
            mags[weaponIndex]--;
            if (mags[weaponIndex] == 0) Invoke(nameof(StartReload), 0.16f);

            float rofMult = GameManager.Instance.OverdriveActive ? 0.55f : 1.0f;
            if (w.isOdinSpinup) rofMult *= Mathf.Lerp(1.0f, 0.52f, odinSpinup);
            fireCooldown = w.cooldown * rofMult;
            pitch = Mathf.Clamp(pitch - w.recoilKick * (IsADS ? 15f : 30f), -72f, 72f);

            if (viewmodel != null) viewmodel.TriggerRecoil(w);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayShoot(w.id, mags[weaponIndex], w.magSize);

            float dmgMult = GameManager.Instance.OverdriveActive ? 2.0f : 1.0f;
            float sp = IsADS ? w.adsSpread : w.spread;
            bool hitAny = false;
            bool anyHeadshot = false;

            for (int p = 0; p < w.pellets; p++)
            {
                Vector3 dir = playerCamera.transform.forward
                    + playerCamera.transform.right * Random.Range(-sp, sp)
                    + playerCamera.transform.up * Random.Range(-sp, sp);
                dir.Normalize();

                Vector3 tracerEnd = playerCamera.transform.position + dir * 110f;
                if (Physics.Raycast(playerCamera.transform.position, dir, out RaycastHit hit, 180f))
                {
                    tracerEnd = hit.point;
                    DemonEnemyAI enemy = hit.collider.GetComponentInParent<DemonEnemyAI>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        hitAny = true;
                        bool isHeadshot = hit.collider.name == "HeadHitbox";
                        if (isHeadshot) anyHeadshot = true;
                        float hsMult = isHeadshot ? w.headshotMult : 1.0f;
                        enemy.TakeDamage(w.damage * dmgMult * hsMult, w.resonanceGain / w.pellets, isHeadshot);
                        if (vampiricTimer > 0f) hp = Mathf.Min(maxHp, hp + 10);
                        GameManager.Instance.SpawnBurstParticles(hit.point, isHeadshot ? Color.yellow : new Color(1f, 0.14f, 0.08f), isHeadshot ? 12 : 6);
                    }
                    else
                    {
                        GameManager.Instance.SpawnBurstParticles(hit.point, new Color(1f, 0.78f, 0.25f), 5);
                        GameManager.Instance.SpawnBulletDecal(hit.point, hit.normal);
                    }
                }
                if (viewmodel != null && (p == 0 || p % 3 == 0))
                {
                    viewmodel.SpawnTracerBeam(tracerEnd, w.tracerColor);
                }
            }

            if (hitAny)
            {
                HitmarkerTimer = anyHeadshot ? 0.20f : 0.12f;
                LastHitWasHeadshot = anyHeadshot;
            }
        }

        public void TriggerMeleeAttack()
        {
            if (meleeCooldown > 0f) return;
            MeleeDef m = melees[meleeIndex];
            meleeCooldown = m.cooldown;
            if (viewmodel != null) viewmodel.TriggerMeleeSwing(m.bladeColor);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayMelee(m.id);

            int hits = 0;
            foreach (var enemy in GameManager.Instance.ActiveEnemies.ToArray())
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 toE = enemy.transform.position - transform.position;
                toE.y = 0f;
                float dist = toE.magnitude;
                if (dist > m.range) continue;

                if (!m.isShockwave)
                {
                    float dot = Vector3.Dot(transform.forward, toE.normalized);
                    if (dot < 0.2f) continue;
                }

                hits++;
                float dmg = m.damage * (GameManager.Instance.OverdriveActive ? 2.0f : 1.0f);
                enemy.TakeDamage(dmg, 14f, false);
                GameManager.Instance.SpawnBurstParticles(enemy.transform.position + Vector3.up * 1.2f, m.bladeColor, 10);
                if (m.lifesteal > 0f) hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(m.lifesteal));
            }

            if (m.isShockwave)
            {
                GameManager.Instance.SpawnBurstParticles(transform.position + transform.forward * 2f, m.bladeColor, 26);
                GameManager.Instance.AddKillfeed($"WARHAMMER SEISMIC SLAM! ({hits} DEMONS CRUSHED)");
            }
            else if (hits > 0)
            {
                HitmarkerTimer = 0.14f;
                LastHitWasHeadshot = false;
                GameManager.Instance.AddKillfeed($"{m.name} CLEAVED {hits} HOSTILE(S)!");
            }
        }

        public void TriggerSpecialAbility()
        {
            int charges = abilityCharges.ContainsKey(equippedAbility) ? abilityCharges[equippedAbility] : 0;
            if (charges <= 0)
            {
                GameManager.Instance.AddKillfeed($"NO {equippedAbility.ToUpper()} CHARGES - PRESS [B] TO BUY IN SHOP!");
                return;
            }
            abilityCharges[equippedAbility] = charges - 1;
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayOverdrive();

            if (equippedAbility == "nova")
            {
                for (int i = 0; i < 12; i++)
                {
                    float ang = (i / 12f) * Mathf.PI * 2f;
                    Vector3 d = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                    GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(orb.GetComponent<Collider>());
                    orb.transform.position = transform.position + d * 1.2f + Vector3.up * 0.4f;
                    orb.transform.localScale = Vector3.one * 0.55f;
                    orb.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.35f, 0.08f), 3.2f);
                    var pr = orb.AddComponent<Projectile3D>();
                    pr.velocity = d * 24f;
                    pr.damage = 260f;
                    pr.fromPlayer = true;
                    pr.isBFG = true;
                }
                GameManager.Instance.AddKillfeed("UNLEASHED HELLFIRE ORBITAL NOVA (12 MISSILES)!");
            }
            else if (equippedAbility == "aegis")
            {
                vampiricTimer = 6.0f;
                invulnTimer = 6.0f;
                armor = maxArmor;
                PickupFlashTimer = 0.45f;
                GameManager.Instance.AddKillfeed("VAMPIRIC BLOOD-AEGIS ACTIVE (6s INVULN + LIFESTEAL)!");
            }
            else if (equippedAbility == "meteor")
            {
                foreach (var enemy in GameManager.Instance.ActiveEnemies.ToArray())
                {
                    if (enemy == null || enemy.IsDead) continue;
                    GameManager.Instance.SpawnBurstParticles(enemy.transform.position + Vector3.up * 4f, new Color(1f, 0.67f, 0.14f), 16);
                    GameManager.Instance.SpawnBurstParticles(enemy.transform.position + Vector3.up * 1.2f, new Color(1f, 0.14f, 0.05f), 16);
                    enemy.TakeDamage(380f, 15f, true);
                }
                GameManager.Instance.AddKillfeed("BRIMSTONE METEOR STRIKE OBLITERATED THE PLAZA!");
            }
        }

        public void ApplyAcidSpit(int impactDamage, float burnDuration, int tickDmg, string attacker)
        {
            if (invulnTimer > 0f) return;
            AcidFlashTimer = 0.45f;
            AcidBurnTimer = Mathf.Max(AcidBurnTimer, burnDuration);
            acidTickDamage = Mathf.Max(3, tickDmg);
            acidTickTimer = 0.65f;
            acidAttacker = attacker;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddKillfeed($"CORRODED BY {attacker}'S ACID SPIT! (-HP BURN)");
            }
            TakeDamage(impactDamage, $"{attacker} (ACID)");
        }

        public void TakeDamage(int rawDmg, string attacker)
        {
            if (invulnTimer > 0f) return;
            int hpLoss = rawDmg;
            if (armor > 0)
            {
                int absorbed = Mathf.Min(armor, Mathf.CeilToInt(rawDmg * 0.65f));
                armor -= absorbed;
                hpLoss = Mathf.Max(1, rawDmg - absorbed);
            }
            hp = Mathf.Max(0, hp - hpLoss);
            DamageFlashTimer = 0.28f;
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayDamage();
            if (hp <= 0)
            {
                GameManager.Instance.TriggerGameOver($"VESSEL SHATTERED BY {attacker}");
            }
        }

        public void CollectPickup(PickupKind kind)
        {
            PickupFlashTimer = 0.2f;
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayPickup();
            switch (kind)
            {
                case PickupKind.Health: hp = Mathf.Min(maxHp, hp + 30); GameManager.Instance.AddKillfeed("HARVESTED VITALITY ICHOR (+30 HP)"); break;
                case PickupKind.Armor: armor = Mathf.Min(maxArmor, armor + 35); GameManager.Instance.AddKillfeed("EQUIPPED AEGIS SHARD (+35 ARMOR)"); break;
                case PickupKind.Shells: shells = Mathf.Min(maxShells, shells + 16); GameManager.Instance.AddKillfeed("COLLECTED 12G / .50 SLUGS (+16)"); break;
                case PickupKind.Cells: cells = Mathf.Min(maxCells, cells + 80); GameManager.Instance.AddKillfeed("COLLECTED ODIN BELT AMMO (+80)"); break;
            }
        }
    }
}
