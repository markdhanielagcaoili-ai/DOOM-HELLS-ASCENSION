using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Articulated 3D Demonic Enemy & Unique Realm Boss AI (Levels 1-100):
    /// Supports standard demon archetypes scaling by campaign level AND all 10 unique Realm Final Bosses
    /// (Levels 10, 20, 30, 40, 50, 60, 70, 80, 90, 100) with distinct 3D geometry (wings, halo rings,
    /// shoulder cannons), unique bullet-pattern attacks, and multi-phase enraged summons!
    /// </summary>
    public class DemonEnemyAI : MonoBehaviour
    {
        public EnemyType enemyType;
        public string displayName;
        public string bossTitle;
        public float hp;
        public float maxHp;
        public float moveSpeed;
        public float attackDamage;
        public float attackRange;
        public float attackCooldown;
        public bool isRanged;
        public bool canBlink;
        public bool isBoss;
        public int scoreValue;
        public int customCreditBounty;
        public float Scale { get; private set; } = 1f;
        public bool IsDead { get; private set; }
        public int BossPhase { get; private set; } = 1;

        private BossProfile bossProfile;
        private float cooldownTimer;
        private float hitFlashTimer;
        private float strafeDir = 1f;
        private float walkPhase;
        private bool triggeredPhase2;
        private bool triggeredPhase3;
        private float gatlingBurstRemaining;
        private float gatlingBurstTick;
        private Color projColor = new Color(1f, 0.45f, 0.1f);
        private float projSpeed = 20f;

        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftArm;
        private Transform rightArm;
        private Transform wingLeft;
        private Transform wingRight;
        private Transform haloRing;
        private Transform healthBarFill;
        private Transform healthBarRoot;
        private Material skinMat;
        private Color origSkinColor;

        /// <summary>Initialize a regular demon scaled smoothly to campaign level 1..100.</summary>
        public void Initialize(EnemyType type, float difficultyMult, int campaignLevel)
        {
            enemyType = type;
            strafeDir = Random.value < 0.5f ? 1f : -1f;
            walkPhase = Random.Range(0f, 6.28f);

            Color armorCol = new Color(0.22f, 0.1f, 0.08f);
            Color skinCol = new Color(0.78f, 0.24f, 0.17f);
            Color glowCol = new Color(1f, 0.67f, 0.13f);

            switch (type)
            {
                case EnemyType.AshWretch:
                    displayName = "ASH-WRETCH"; hp = 70f; moveSpeed = 7.4f; attackDamage = 11f;
                    attackRange = 2.6f; attackCooldown = 0.95f; isRanged = false; scoreValue = 150; Scale = 0.92f;
                    break;
                case EnemyType.CinderImp:
                    displayName = "CINDER-IMP"; hp = 95f; moveSpeed = 5.4f; attackDamage = 13f;
                    attackRange = 28f; attackCooldown = 1.65f; isRanged = true; projSpeed = 21f; scoreValue = 250; Scale = 1.0f;
                    armorCol = new Color(0.16f, 0.08f, 0.1f); skinCol = new Color(0.62f, 0.16f, 0.11f); glowCol = new Color(0.14f, 0.85f, 1f);
                    projColor = new Color(1f, 0.42f, 0.1f);
                    break;
                case EnemyType.IronGorger:
                    displayName = "IRON-GORGER"; hp = 230f; moveSpeed = 3.9f; attackDamage = 20f;
                    attackRange = 26f; attackCooldown = 1.90f; isRanged = true; projSpeed = 19f; scoreValue = 450; Scale = 1.38f;
                    armorCol = new Color(0.29f, 0.26f, 0.24f); skinCol = new Color(0.36f, 0.13f, 0.09f); glowCol = new Color(1f, 0.23f, 0.13f);
                    projColor = new Color(1f, 0.18f, 0.1f);
                    break;
                case EnemyType.VoidCantor:
                    displayName = "VOID-CANTOR"; hp = 150f; moveSpeed = 5.8f; attackDamage = 17f;
                    attackRange = 32f; attackCooldown = 1.45f; isRanged = true; canBlink = true; projSpeed = 25f; scoreValue = 380; Scale = 1.08f;
                    armorCol = new Color(0.11f, 0.05f, 0.19f); skinCol = new Color(0.38f, 0.13f, 0.61f); glowCol = new Color(1f, 0.82f, 0.36f);
                    projColor = new Color(0.72f, 0.36f, 1f);
                    break;
            }

            // Smooth Level 1 -> 100 difficulty scaling
            float lvlHpScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.055f;
            float lvlDmgScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.012f;
            hp = Mathf.Round(hp * difficultyMult * lvlHpScale);
            maxHp = hp;
            attackDamage = Mathf.Round(attackDamage * difficultyMult * lvlDmgScale);
            moveSpeed *= (1f + Mathf.Min(0.40f, campaignLevel * 0.004f));
            cooldownTimer = Random.Range(0.3f, 1.1f);

            BuildArticulated3DModel(armorCol, skinCol, glowCol, false, false, false);
        }

        /// <summary>Initialize one of the 10 unique Realm Final Bosses (Level 10, 20, ..., 100).</summary>
        public void InitializeUniqueBoss(BossProfile profile, float difficultyMult)
        {
            bossProfile = profile;
            enemyType = EnemyType.ArchonMalakor;
            isBoss = true;
            isRanged = true;
            displayName = profile.bossName;
            bossTitle = profile.bossTitle;
            Scale = profile.scale;
            canBlink = profile.canBlink;
            moveSpeed = profile.moveSpeed;
            attackDamage = Mathf.Round(profile.attackDamage * difficultyMult);
            attackRange = 42f;
            attackCooldown = profile.attackCooldown;
            projSpeed = profile.projSpeed;
            projColor = profile.projColor;
            hp = Mathf.Round(profile.baseHp * difficultyMult);
            maxHp = hp;
            scoreValue = 1500 + profile.bossLevel * 45;
            customCreditBounty = profile.creditBounty;
            cooldownTimer = 0.8f;
            strafeDir = Random.value < 0.5f ? 1f : -1f;

            BuildArticulated3DModel(profile.armorColor, profile.skinColor, profile.glowColor,
                profile.hasWings, profile.hasHalo, profile.hasShoulderCannons);
        }

        private void BuildArticulated3DModel(Color armorCol, Color skinCol, Color glowCol,
                                             bool withWings, bool withHalo, bool withCannons)
        {
            float s = Scale;
            Material armorMat = CitadelWorldBuilder.MakeStdMat(armorCol, 0.70f, 0.58f);
            skinMat = CitadelWorldBuilder.MakeStdMat(skinCol, 0.18f, 0.32f);
            origSkinColor = skinCol;
            Material glowMat = CitadelWorldBuilder.MakeEmissiveMat(glowCol, 2.6f);

            // Legs
            leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, new Vector3(-0.22f * s, 0.42f * s, 0f), new Vector3(0.28f * s, 0.85f * s, 0.28f * s), armorMat, false).transform;
            rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, new Vector3(0.22f * s, 0.42f * s, 0f), new Vector3(0.28f * s, 0.85f * s, 0.28f * s), armorMat, false).transform;

            // Armored Torso + Body Collider
            CreatePart("Torso", PrimitiveType.Cube, new Vector3(0f, 1.32f * s, 0f), new Vector3(0.82f * s, 1.05f * s, 0.48f * s), skinMat, true);
            CreatePart("ChestPlate", PrimitiveType.Cube, new Vector3(0f, 1.44f * s, 0f), new Vector3(0.88f * s, 0.62f * s, 0.52f * s), armorMat, false);
            CreatePart("HellCore", PrimitiveType.Cube, new Vector3(0f, 1.42f * s, 0f), new Vector3(0.28f * s, 0.28f * s, 0.56f * s), glowMat, false);

            // Head + Headshot Hitbox Collider!
            CreatePart("HeadHitbox", PrimitiveType.Cube, new Vector3(0f, 2.08f * s, 0f), new Vector3(0.48f * s, 0.48f * s, 0.48f * s), skinMat, true);
            CreatePart("EyeL", PrimitiveType.Cube, new Vector3(-0.11f * s, 2.12f * s, 0.25f * s), new Vector3(0.1f * s, 0.06f * s, 0.05f * s), glowMat, false);
            CreatePart("EyeR", PrimitiveType.Cube, new Vector3(0.11f * s, 2.12f * s, 0.25f * s), new Vector3(0.1f * s, 0.06f * s, 0.05f * s), glowMat, false);

            // Horns (Bosses get grand curved crown horns)
            GameObject hornL = CreatePart("HornL", PrimitiveType.Cylinder, new Vector3(-0.26f * s, 2.44f * s, 0f), new Vector3(0.09f * s, (isBoss ? 0.36f : 0.26f) * s, 0.09f * s), isBoss ? glowMat : armorMat, false);
            hornL.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            GameObject hornR = CreatePart("HornR", PrimitiveType.Cylinder, new Vector3(0.26f * s, 2.44f * s, 0f), new Vector3(0.09f * s, (isBoss ? 0.36f : 0.26f) * s, 0.09f * s), isBoss ? glowMat : armorMat, false);
            hornR.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);

            // Articulated Arms & Hell-Blade
            leftArm = CreatePart("LeftArm", PrimitiveType.Cube, new Vector3(-0.58f * s, 1.35f * s, 0.1f * s), new Vector3(0.24f * s, 0.85f * s, 0.24f * s), skinMat, false).transform;
            rightArm = CreatePart("RightArm", PrimitiveType.Cube, new Vector3(0.58f * s, 1.35f * s, 0.1f * s), new Vector3(0.24f * s, 0.85f * s, 0.24f * s), skinMat, false).transform;
            GameObject blade = CreatePart("HellBlade", PrimitiveType.Cube, new Vector3(0f, -0.35f * s, 0.28f * s), new Vector3(0.08f * s, 0.14f * s, 0.65f * s), glowMat, false);
            blade.transform.SetParent(rightArm, false);

            // UNIQUE BOSS 3D ACCESSORIES: Demon Wings, Orbiting Halo Crown, Twin Shoulder Cannons!
            if (withWings)
            {
                GameObject wL = CreatePart("BossWingL", PrimitiveType.Cube, new Vector3(-0.92f * s, 1.65f * s, -0.32f * s), new Vector3(1.15f * s, 0.55f * s, 0.08f * s), armorMat, false);
                wL.transform.localRotation = Quaternion.Euler(0f, 24f, 22f);
                CreatePart("WingGlowL", PrimitiveType.Cube, Vector3.zero, new Vector3(1.0f, 0.22f, 1.4f), glowMat, false).transform.SetParent(wL.transform, false);
                wingLeft = wL.transform;

                GameObject wR = CreatePart("BossWingR", PrimitiveType.Cube, new Vector3(0.92f * s, 1.65f * s, -0.32f * s), new Vector3(1.15f * s, 0.55f * s, 0.08f * s), armorMat, false);
                wR.transform.localRotation = Quaternion.Euler(0f, -24f, -22f);
                CreatePart("WingGlowR", PrimitiveType.Cube, Vector3.zero, new Vector3(1.0f, 0.22f, 1.4f), glowMat, false).transform.SetParent(wR.transform, false);
                wingRight = wR.transform;
            }
            if (withHalo)
            {
                GameObject haloRoot = new GameObject("BossHaloRing");
                haloRoot.transform.SetParent(transform, false);
                haloRoot.transform.localPosition = new Vector3(0f, 2.65f * s, -0.15f * s);
                for (int k = 0; k < 6; k++)
                {
                    float a = (k / 6f) * Mathf.PI * 2f;
                    GameObject seg = CreatePart($"HaloSeg_{k}", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.55f * s, Mathf.Sin(a) * 0.55f * s, 0f), new Vector3(0.24f * s, 0.08f * s, 0.08f * s), glowMat, false);
                    seg.transform.SetParent(haloRoot.transform, false);
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                }
                haloRing = haloRoot.transform;
            }
            if (withCannons)
            {
                GameObject cL = CreatePart("ShoulderCannonL", PrimitiveType.Cylinder, new Vector3(-0.62f * s, 1.98f * s, 0.15f * s), new Vector3(0.22f * s, 0.48f * s, 0.22f * s), armorMat, false);
                cL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject cR = CreatePart("ShoulderCannonR", PrimitiveType.Cylinder, new Vector3(0.62f * s, 1.98f * s, 0.15f * s), new Vector3(0.22f * s, 0.48f * s, 0.22f * s), armorMat, false);
                cR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Floating 3D Billboard Health Bar
            GameObject hbRoot = new GameObject("HealthBar");
            hbRoot.transform.SetParent(transform, false);
            hbRoot.transform.localPosition = new Vector3(0f, (isBoss ? 3.05f : 2.82f) * s, 0f);
            healthBarRoot = hbRoot.transform;

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bg.GetComponent<Collider>());
            bg.transform.SetParent(healthBarRoot, false);
            bg.transform.localScale = new Vector3(1.25f * s, 0.14f * s, 1f);
            bg.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeUnlitMat(Color.black);

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(fill.GetComponent<Collider>());
            fill.transform.SetParent(healthBarRoot, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(1.18f * s, 0.10f * s, 1f);
            fill.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeUnlitMat(isBoss ? glowCol : new Color(1f, 0.23f, 0.13f));
            healthBarFill = fill.transform;
        }

        private GameObject CreatePart(string partName, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(prim);
            go.name = partName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().material = mat;
            if (!keepCollider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void Update()
        {
            if (IsDead || GameManager.Instance == null || GameManager.Instance.IsPaused || !GameManager.Instance.IsRunning)
                return;

            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f && skinMat != null) skinMat.color = origSkinColor;
            }

            if (FPSPlayerController.Instance == null) return;
            Vector3 playerPos = FPSPlayerController.Instance.transform.position;

            if (healthBarRoot != null && Camera.main != null)
                healthBarRoot.LookAt(Camera.main.transform);

            float timeScale = GameManager.Instance.OverdriveActive ? 0.55f : 1.0f;
            float dt = Time.deltaTime * timeScale;
            if (cooldownTimer > 0f) cooldownTimer -= dt;

            // Animate Boss Wings & Rotating Halo Crown
            if (wingLeft != null && wingRight != null)
            {
                float flap = Mathf.Sin(Time.time * 4.5f) * 14f;
                wingLeft.localRotation = Quaternion.Euler(0f, 24f + flap, 22f);
                wingRight.localRotation = Quaternion.Euler(0f, -24f - flap, -22f);
            }
            if (haloRing != null) haloRing.Rotate(0f, 0f, 75f * dt);

            Vector3 toPlayer = playerPos - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.01f ? toPlayer / dist : Vector3.forward;

            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Stride animation
            walkPhase += dt * 8.5f;
            float swing = Mathf.Sin(walkPhase) * 28f;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-swing * 0.7f, 0f, 0f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(swing * 0.7f, 0f, 0f);

            // Handle Ferrum Titan Gatling Sub-Burst
            if (gatlingBurstRemaining > 0f)
            {
                gatlingBurstTick -= dt;
                if (gatlingBurstTick <= 0f)
                {
                    gatlingBurstTick = 0.13f;
                    gatlingBurstRemaining -= 1f;
                    FireProjectile(dir, Random.Range(-4.5f, 4.5f));
                }
            }

            if (!isRanged)
            {
                if (dist > 1.9f) MoveInArena(dir * moveSpeed * dt);
                if (dist <= attackRange && cooldownTimer <= 0f)
                {
                    cooldownTimer = attackCooldown;
                    FPSPlayerController.Instance.TakeDamage(Mathf.RoundToInt(attackDamage), displayName);
                }
            }
            else
            {
                if (dist > 14f)
                {
                    MoveInArena(dir * moveSpeed * dt);
                }
                else
                {
                    Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * strafeDir;
                    MoveInArena(perp * moveSpeed * 0.78f * dt);
                    if (Random.value < 0.018f) strafeDir *= -1f;
                }

                if (dist <= attackRange && cooldownTimer <= 0f)
                {
                    float phaseCdMult = BossPhase >= 3 ? 0.72f : (BossPhase == 2 ? 0.85f : 1.0f);
                    cooldownTimer = attackCooldown * phaseCdMult;

                    if (isBoss && bossProfile != null)
                    {
                        ExecuteUniqueBossAttack(dir);
                    }
                    else
                    {
                        FireProjectile(dir, 0f);
                    }

                    if (canBlink && Random.value < (isBoss ? 0.65f : 0.45f))
                    {
                        Vector3 blink = new Vector3(-dir.z, 0f, dir.x) * strafeDir * (isBoss ? 5.5f : 3.5f);
                        MoveInArena(blink);
                    }
                }
            }
        }

        private void ExecuteUniqueBossAttack(Vector3 dir)
        {
            switch (bossProfile.pattern)
            {
                case BossAttackPattern.Fan3: // Lvl 10 Pyroclast
                    FireFan(dir, 3, 24f);
                    break;
                case BossAttackPattern.Fan5Mortar: // Lvl 20 Ignis Rex
                    FireFan(dir, 5, 40f);
                    break;
                case BossAttackPattern.IceBlinkBurst: // Lvl 30 Glacieron
                    FireFan(dir, 3, 14f);
                    break;
                case BossAttackPattern.Radial8Nova: // Lvl 40 Pestilor
                    FireRadialRing(8, Time.time * 25f);
                    break;
                case BossAttackPattern.VoidTwinSpiral: // Lvl 50 Umbrathor
                    FireFan(dir, 4, 28f);
                    if (BossPhase >= 2) FireRadialRing(6, Time.time * 40f);
                    break;
                case BossAttackPattern.GatlingStream: // Lvl 60 Ferrum Titan
                    gatlingBurstRemaining = BossPhase >= 2 ? 6f : 4f;
                    gatlingBurstTick = 0f;
                    break;
                case BossAttackPattern.BloodSeraph7: // Lvl 70 Sanguinus
                    FireFan(dir, 7, 52f);
                    break;
                case BossAttackPattern.StormHyperVolley: // Lvl 80 Fulguron
                    FireFan(dir, 5, 24f);
                    break;
                case BossAttackPattern.SolarRing10: // Lvl 90 Astralis
                    FireRadialRing(10, Time.time * 30f);
                    FireFan(dir, 3, 18f);
                    break;
                case BossAttackPattern.MalakorApocalypse: // Lvl 100 Archon Malakor (3 Phases!)
                    if (BossPhase == 1) FireFan(dir, 5, 38f);
                    else if (BossPhase == 2) { FireRadialRing(9, Time.time * 35f); FireFan(dir, 3, 20f); }
                    else { FireRadialRing(12, Time.time * 50f); FireFan(dir, 5, 32f); }
                    break;
            }
        }

        private void FireFan(Vector3 baseDir, int count, float totalArcDeg)
        {
            if (count <= 1) { FireProjectile(baseDir, 0f); return; }
            float step = totalArcDeg / (count - 1);
            float start = -totalArcDeg * 0.5f;
            for (int i = 0; i < count; i++)
                FireProjectile(baseDir, start + step * i);
        }

        private void FireRadialRing(int count, float angleOffsetDeg)
        {
            for (int i = 0; i < count; i++)
            {
                float deg = angleOffsetDeg + (360f / count) * i;
                Vector3 dir = Quaternion.Euler(0f, deg, 0f) * Vector3.forward;
                FireProjectile(dir, 0f, true);
            }
        }

        private void MoveInArena(Vector3 delta)
        {
            Vector3 next = transform.position + delta;
            next.x = Mathf.Clamp(next.x, -22.5f, 22.5f);
            next.z = Mathf.Clamp(next.z, -31.5f, 17.5f);
            next.y = 0f;
            transform.position = next;
        }

        private void FireProjectile(Vector3 baseDir, float angleOffsetDeg, bool flatRing = false)
        {
            Vector3 launchDir = Quaternion.Euler(0f, angleOffsetDeg, 0f) * baseDir;
            Vector3 spawnPos = transform.position + Vector3.up * (1.45f * Scale) + launchDir * 1.1f;
            Vector3 aim;
            if (flatRing)
            {
                aim = new Vector3(launchDir.x, -0.05f, launchDir.z).normalized;
            }
            else
            {
                Vector3 targetPos = FPSPlayerController.Instance.transform.position - Vector3.up * 0.15f;
                aim = Quaternion.Euler(0f, angleOffsetDeg, 0f) * (targetPos - spawnPos).normalized;
            }

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "DemonProjectile";
            Destroy(orb.GetComponent<Collider>());
            orb.transform.position = spawnPos;
            orb.transform.localScale = Vector3.one * (isBoss ? 0.62f : 0.42f);
            orb.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeEmissiveMat(projColor, 2.8f);

            var proj = orb.AddComponent<Projectile3D>();
            proj.velocity = aim * projSpeed;
            proj.damage = attackDamage;
            proj.fromPlayer = false;
            proj.sourceName = displayName;
        }

        public void TakeDamage(float amount, float resonanceBonus, bool isHeadshot)
        {
            if (IsDead) return;
            hp -= amount;
            hitFlashTimer = 0.12f;
            if (skinMat != null) skinMat.color = Color.white;

            if (GameManager.Instance != null) GameManager.Instance.AddResonance(resonanceBonus);

            // Multi-phase Boss Transitions!
            if (isBoss && bossProfile != null && hp > 0f)
            {
                float ratio = hp / maxHp;
                float p2Thresh = bossProfile.pattern == BossAttackPattern.MalakorApocalypse ? 0.66f : 0.50f;
                if (!triggeredPhase2 && ratio <= p2Thresh)
                {
                    triggeredPhase2 = true;
                    BossPhase = 2;
                    if (bossProfile.healsOnEnrage) hp = Mathf.Min(maxHp, hp + maxHp * 0.12f);
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SpawnSingleEnemy(bossProfile.summonTypeA, new Vector3(-11f, 0f, -21f));
                        GameManager.Instance.SpawnSingleEnemy(bossProfile.summonTypeB, new Vector3(11f, 0f, -21f));
                        GameManager.Instance.AddKillfeed($"BOSS PHASE 2: {displayName} ENRAGED{(bossProfile.healsOnEnrage ? " & DRAINED BLOOD!" : " - SUMMONED GUARDS!")}");
                    }
                }
                if (bossProfile.pattern == BossAttackPattern.MalakorApocalypse && !triggeredPhase3 && ratio <= 0.33f)
                {
                    triggeredPhase3 = true;
                    BossPhase = 3;
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SpawnSingleEnemy(EnemyType.VoidCantor, new Vector3(-14f, 0f, -18f));
                        GameManager.Instance.SpawnSingleEnemy(EnemyType.IronGorger, new Vector3(14f, 0f, -18f));
                        GameManager.Instance.AddKillfeed("FINAL PHASE 3: ARCHON MALAKOR UNLEASHES APOCALYPSE!");
                    }
                }
            }

            if (healthBarFill != null)
            {
                float ratio = Mathf.Clamp01(hp / maxHp);
                healthBarFill.localScale = new Vector3(1.18f * Scale * ratio, 0.10f * Scale, 1f);
            }

            if (hp <= 0f)
            {
                hp = 0f;
                IsDead = true;
                int streak = GameManager.Instance != null ? GameManager.Instance.NextKillStreakCount() : 1;
                if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayHit(true, isHeadshot, streak);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * 1.4f, isBoss ? projColor : new Color(1f, 0.23f, 0.1f), isBoss ? 42 : 18);
                    GameManager.Instance.OnEnemyKilled(this, isHeadshot);
                }
                Destroy(gameObject);
            }
            else
            {
                if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayHit(false, isHeadshot, 1);
            }
        }
    }
}
