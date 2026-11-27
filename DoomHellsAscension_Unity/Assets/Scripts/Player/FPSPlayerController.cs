using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Full 3D Unity CharacterController FPS Player with VALORANT-style tactical gunplay:
    /// 5 distinct firearms (Vandal, Phantom, Judge, Odin w/ RPM spin-up, Operator w/ full scope),
    /// crisp 1-tap headshots, per-weapon tactical reloads ([R]), inspect ([Y]), 3D melee ([V]),
    /// Abyssal Phase-Dash ([SHIFT]), and Chrono-Thermal Overdrive ([Q/E]).
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
        public float mouseSensitivity = 1.0f;

        public List<WeaponDef> weapons;
        public List<MeleeDef> melees;
        public List<WeaponSkinDef> skins;
        public int[] mags;
        public int weaponIndex = 0;
        public int meleeIndex = 0;

        public bool IsMeleeEquipped { get; private set; }
        public bool IsADS { get; private set; }
        public bool IsSniperScoped => !IsMeleeEquipped && IsADS && weapons != null && weaponIndex < weapons.Count && weapons[weaponIndex].isSniper && !IsReloading;
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
        private float reloadTimer;
        private float activeReloadDuration = 1.55f;
        private bool prevADS;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            weapons = GameCatalog.CreateDefaultWeapons();
            melees = GameCatalog.CreateDefaultMelees();
            skins = GameCatalog.CreateDefaultSkins();
            LoadSavedSkins();
            ResetLoadoutMags();
        }

        private void Start()
        {
            ApplyAllEquippedSkinsToViewmodel();
        }

        private void LoadSavedSkins()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                int saved = Mathf.Clamp(PlayerPrefs.GetInt($"DoomWeaponSkin_{i}", 0), 0, skins.Count - 1);
                weapons[i].skinIndex = saved;
                if (saved > 0)
                {
                    weapons[i].accentColor = skins[saved].glowColor;
                    weapons[i].tracerColor = skins[saved].tracerColor;
                }
            }
            for (int i = 0; i < melees.Count; i++)
            {
                int saved = Mathf.Clamp(PlayerPrefs.GetInt($"DoomMeleeSkin_{i}", 0), 0, skins.Count - 1);
                melees[i].skinIndex = saved;
                if (saved > 0) melees[i].bladeColor = skins[saved].glowColor;
            }
        }

        public void ApplyAllEquippedSkinsToViewmodel()
        {
            if (viewmodel == null || skins == null || skins.Count == 0) return;
            for (int i = 0; i < weapons.Count; i++)
            {
                int s = Mathf.Clamp(weapons[i].skinIndex, 0, skins.Count - 1);
                if (s > 0) viewmodel.ApplyWeaponSkin(i, skins[s]);
            }
            for (int i = 0; i < melees.Count; i++)
            {
                int s = Mathf.Clamp(melees[i].skinIndex, 0, skins.Count - 1);
                if (s > 0) viewmodel.ApplyMeleeSkin(i, skins[s]);
            }
        }

        public void SetWeaponSkin(int weaponId, int skinIdx)
        {
            if (weaponId < 0 || weaponId >= weapons.Count || skins == null || skins.Count == 0) return;
            int clamped = Mathf.Clamp(skinIdx, 0, skins.Count - 1);
            weapons[weaponId].skinIndex = clamped;
            WeaponSkinDef sk = skins[clamped];
            weapons[weaponId].accentColor = sk.glowColor;
            weapons[weaponId].tracerColor = sk.tracerColor;
            PlayerPrefs.SetInt($"DoomWeaponSkin_{weaponId}", clamped);
            PlayerPrefs.Save();
            if (viewmodel != null) viewmodel.ApplyWeaponSkin(weaponId, sk);
        }

        public void SetMeleeSkin(int meleeId, int skinIdx)
        {
            if (meleeId < 0 || meleeId >= melees.Count || skins == null || skins.Count == 0) return;
            int clamped = Mathf.Clamp(skinIdx, 0, skins.Count - 1);
            melees[meleeId].skinIndex = clamped;
            WeaponSkinDef sk = skins[clamped];
            melees[meleeId].bladeColor = sk.glowColor;
            PlayerPrefs.SetInt($"DoomMeleeSkin_{meleeId}", clamped);
            PlayerPrefs.Save();
            if (viewmodel != null) viewmodel.ApplyMeleeSkin(meleeId, sk);
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
            IsMeleeEquipped = false;
            IsReloading = false;
            odinSpinup = 0f;
            if (viewmodel != null) viewmodel.SetActiveWeaponModel(idx);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayEquip();
        }

        public void SelectMelee()
        {
            if (melees == null || melees.Count == 0) return;
            IsMeleeEquipped = true;
            IsReloading = false;
            IsADS = false;
            odinSpinup = 0f;
            MeleeDef m = melees[meleeIndex];
            if (viewmodel != null) viewmodel.SetMeleeMode(true, m.id, m.bladeColor);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayMeleeEquip(m.id);
        }

        public void EquipMeleeFromShop(int idx)
        {
            if (melees == null || idx < 0 || idx >= melees.Count) return;
            meleeIndex = idx;
            MeleeDef m = melees[meleeIndex];
            if (viewmodel != null) viewmodel.SetActiveMeleeVariant(m.id, m.bladeColor);
            SelectMelee();
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
            float sens = (IsSniperScoped ? 0.65f : (IsADS ? 1.35f : 2.3f)) * Mathf.Clamp(mouseSensitivity, 0.35f, 3.0f);
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
                float meleeSpeedBoost = IsMeleeEquipped ? 1.12f : 1.0f;
                float speed = (GameManager.Instance.OverdriveActive ? 13.5f : 10.2f) * (IsADS ? 0.65f : meleeSpeedBoost);
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
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) SelectMelee();

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.05f)
            {
                int totalSlots = weapons.Count + 1; // 0..4 firearms + slot 5 = Melee ([0])
                int curSlot = IsMeleeEquipped ? weapons.Count : weaponIndex;
                int dir = scroll > 0 ? 1 : -1;
                int nextSlot = (curSlot + dir + totalSlots) % totalSlots;
                if (nextSlot == weapons.Count) SelectMelee();
                else SelectWeapon(nextSlot);
            }

            IsADS = !IsMeleeEquipped && Input.GetMouseButton(1) && !IsReloading;
            if (IsADS != prevADS)
            {
                prevADS = IsADS;
                if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayADS(IsADS);
            }

            if (Input.GetKeyDown(KeyCode.R) && !IsMeleeEquipped) StartReload();
            if (Input.GetKeyDown(KeyCode.Y) && viewmodel != null && !IsReloading) viewmodel.TriggerInspect();
            if (Input.GetKeyDown(KeyCode.V)) TriggerMeleeAttack(false);
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
                GameManager.Instance.TriggerAscensionOverdrive();

            if (IsMeleeEquipped)
            {
                odinSpinup = Mathf.Max(0f, odinSpinup - dt * 1.8f);
                if (Input.GetMouseButtonDown(0)) TriggerMeleeAttack(false);
                else if (Input.GetMouseButtonDown(1)) TriggerMeleeAttack(true);
                return;
            }

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

            if (viewmodel != null) viewmodel.TriggerRecoil(w, IsADS);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayShoot(w.id, mags[weaponIndex], w.magSize);

            float dmgMult = GameManager.Instance.OverdriveActive ? 2.0f : 1.0f;
            float sp = IsADS ? w.adsSpread : w.spread;
            bool hitAny = false;
            bool anyHeadshot = false;
            bool isReaverVandal = (w.id == 0);

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
                        Color impactCol = isHeadshot
                            ? (isReaverVandal ? new Color(0.86f, 0.56f, 1.0f) : Color.yellow)
                            : (isReaverVandal ? new Color(0.62f, 0.22f, 1.0f) : new Color(1f, 0.14f, 0.08f));
                        GameManager.Instance.SpawnBurstParticles(hit.point, impactCol, isHeadshot ? 14 : 7);
                    }
                    else
                    {
                        Color wallSparkCol = isReaverVandal ? new Color(0.66f, 0.30f, 1.0f) : new Color(1f, 0.78f, 0.25f);
                        GameManager.Instance.SpawnBurstParticles(hit.point, wallSparkCol, isReaverVandal ? 7 : 5);
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

        public void TriggerMeleeAttack(bool isHeavy = false)
        {
            if (meleeCooldown > 0f) return;
            MeleeDef m = melees[meleeIndex];
            meleeCooldown = isHeavy ? (m.cooldown * 1.38f) : m.cooldown;
            if (viewmodel != null) viewmodel.TriggerMeleeSwing(m.bladeColor, m.id, isHeavy);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayMelee(m.id);

            float attackRange = isHeavy ? (m.range * 1.15f) : m.range;
            float heavyMult = isHeavy ? 1.45f : 1.0f;

            int hits = 0;
            foreach (var enemy in GameManager.Instance.ActiveEnemies.ToArray())
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 toE = enemy.transform.position - transform.position;
                toE.y = 0f;
                float dist = toE.magnitude;
                if (dist > attackRange) continue;

                if (!m.isShockwave)
                {
                    float dot = Vector3.Dot(transform.forward, toE.normalized);
                    if (dot < 0.2f) continue;
                }

                hits++;
                float dmg = m.damage * heavyMult * (GameManager.Instance.OverdriveActive ? 2.0f : 1.0f);
                enemy.TakeDamage(dmg, 14f, isHeavy);
                GameManager.Instance.SpawnBurstParticles(enemy.transform.position + Vector3.up * 1.2f, m.bladeColor, isHeavy ? 16 : 10);
                if (m.lifesteal > 0f) hp = Mathf.Min(maxHp, hp + Mathf.RoundToInt(m.lifesteal));
            }

            if (m.isShockwave)
            {
                GameManager.Instance.SpawnBurstParticles(transform.position + transform.forward * 2f, m.bladeColor, 26);
                GameManager.Instance.AddKillfeed($"WARHAMMER SEISMIC SLAM! ({hits} DEMONS CRUSHED)");
            }
            else if (hits > 0)
            {
                HitmarkerTimer = isHeavy ? 0.18f : 0.14f;
                LastHitWasHeadshot = isHeavy;
                GameManager.Instance.AddKillfeed($"{m.name} {(isHeavy ? "HEAVY STABBED" : "SLASHED")} {hits} HOSTILE(S)!");
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
