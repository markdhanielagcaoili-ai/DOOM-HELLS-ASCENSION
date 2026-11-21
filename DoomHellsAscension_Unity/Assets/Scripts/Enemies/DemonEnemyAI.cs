using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Demonic Enemy & Unique Realm Boss AI (Levels 1-100):
    /// - Non-Boss Levels (1-9, 11-19, ..., 91-99): Uses the iconic DOOM Demon sprite billboard with lively
    ///   stalking/weaving stride animation, obstacle steering, direct melee claw lunges ("sila mismo mag-aatake"),
    ///   and a Toxic Acid Spit splash-stream ability (NO round balls!) that corrodes and drains player HP over time!
    /// - Final Boss Levels (10, 20, 30, ..., 100): Keeps all 10 unique giant 3D Realm Final Bosses with
    ///   distinct 3D geometry (wings, halo rings, shoulder cannons), unique boss patterns, and multi-phase summons!
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

        private static Texture2D cachedDoomDemonTex;

        private BossProfile bossProfile;
        private float cooldownTimer;
        private float acidCooldownTimer;
        private float acidWindupTimer;
        private float lungeTimer;
        private Vector3 lungeDir;
        private float hitFlashTimer;
        private float strafeDir = 1f;
        private float weaveTimer;
        private float walkPhase;
        private bool triggeredPhase2;
        private bool triggeredPhase3;
        private float gatlingBurstRemaining;
        private float gatlingBurstTick;
        private Color projColor = new Color(1f, 0.45f, 0.1f);
        private float projSpeed = 20f;
        private int acidTickDmg = 4;

        // Non-Boss Animated DOOM Sprite Demon visuals
        private bool usesSpriteDemon;
        private Transform spritePivot;
        private Transform spriteQuad;
        private Material spriteMat;
        private Color origSpriteTint = new Color(1f, 0.96f, 0.94f, 1f);
        private float baseSpriteW = 1.82f;
        private float baseSpriteH = 2.46f;

        // Articulated 3D Boss parts
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

        /// <summary>
        /// Initialize a non-boss demon (used on all non-boss levels) using the uploaded DOOM Demon visual,
        /// lively stride/weave movement, direct melee rush lunges, and Toxic Acid Spit ability (no round balls!).
        /// </summary>
        public void Initialize(EnemyType type, float difficultyMult, int campaignLevel)
        {
            enemyType = type;
            isBoss = false;
            usesSpriteDemon = true;
            isRanged = false; // Charges directly at player for melee claw attacks + spits toxic acid ability!
            strafeDir = Random.value < 0.5f ? 1f : -1f;
            walkPhase = Random.Range(0f, 6.28f);
            weaveTimer = Random.Range(1.0f, 2.6f);

            switch (type)
            {
                case EnemyType.AshWretch:
                    displayName = "ACID STALKER"; hp = 75f; moveSpeed = 7.2f; attackDamage = 11f;
                    attackRange = 3.0f; attackCooldown = 0.95f; scoreValue = 160; Scale = 0.96f;
                    origSpriteTint = new Color(1.0f, 0.95f, 0.92f, 1f);
                    break;
                case EnemyType.CinderImp:
                    displayName = "VENOM IMP"; hp = 100f; moveSpeed = 6.4f; attackDamage = 13f;
                    attackRange = 3.1f; attackCooldown = 1.05f; scoreValue = 240; Scale = 1.04f;
                    origSpriteTint = new Color(1.0f, 0.98f, 0.90f, 1f);
                    break;
                case EnemyType.IronGorger:
                    displayName = "BILE BRUTE"; hp = 215f; moveSpeed = 5.2f; attackDamage = 18f;
                    attackRange = 3.3f; attackCooldown = 1.15f; scoreValue = 420; Scale = 1.24f;
                    origSpriteTint = new Color(0.95f, 0.88f, 0.86f, 1f);
                    break;
                case EnemyType.VoidCantor:
                    displayName = "BLIGHT FIEND"; hp = 145f; moveSpeed = 6.8f; attackDamage = 15f;
                    attackRange = 3.1f; attackCooldown = 1.00f; canBlink = true; scoreValue = 360; Scale = 1.08f;
                    origSpriteTint = new Color(0.96f, 1.0f, 0.92f, 1f);
                    break;
            }

            // Smooth Level 1 -> 100 scaling
            float lvlHpScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.055f;
            float lvlDmgScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.012f;
            hp = Mathf.Round(hp * difficultyMult * lvlHpScale);
            maxHp = hp;
            attackDamage = Mathf.Round(attackDamage * difficultyMult * lvlDmgScale);
            acidTickDmg = Mathf.Clamp(Mathf.RoundToInt((3.5f + campaignLevel * 0.06f) * difficultyMult), 3, 9);
            moveSpeed *= (1f + Mathf.Min(0.36f, campaignLevel * 0.0038f));
            cooldownTimer = Random.Range(0.25f, 0.75f);
            acidCooldownTimer = Random.Range(1.6f, 3.6f);

            BuildDoomSpriteDemon();
        }

        /// <summary>Initialize one of the 10 unique Realm Final Bosses (Level 10, 20, ..., 100).</summary>
        public void InitializeUniqueBoss(BossProfile profile, float difficultyMult)
        {
            bossProfile = profile;
            enemyType = EnemyType.ArchonMalakor;
            isBoss = true;
            usesSpriteDemon = false;
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

        private void BuildDoomSpriteDemon()
        {
            float s = Scale;
            if (cachedDoomDemonTex == null)
            {
                cachedDoomDemonTex = Resources.Load<Texture2D>("Enemies/doom_demon");
                if (cachedDoomDemonTex != null) cachedDoomDemonTex.filterMode = FilterMode.Point;
            }

            // Pivot for dynamic walking bounce, lunge lean, and stride squash/stretch
            GameObject pivGo = new GameObject("DemonSpritePivot");
            pivGo.transform.SetParent(transform, false);
            pivGo.transform.localPosition = Vector3.zero;
            spritePivot = pivGo.transform;

            // Ground Contact Shadow Disc so the demon sits firmly grounded on the arena tiles
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "DemonShadow";
            Destroy(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localScale = new Vector3(1.18f * s, 0.01f, 1.18f * s);
            shadow.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeUnlitMat(new Color(0.05f, 0.03f, 0.03f, 0.85f));

            // Transparent Camera-Facing DOOM Demon Sprite Quad (exact 265:360 aspect ratio!)
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DemonSpriteVisual";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(spritePivot, false);
            baseSpriteH = 2.46f * s;
            baseSpriteW = baseSpriteH * (265f / 360f);
            quad.transform.localPosition = new Vector3(0f, baseSpriteH * 0.5f, 0f);
            quad.transform.localScale = new Vector3(baseSpriteW, baseSpriteH, 1f);
            spriteQuad = quad.transform;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            spriteMat = new Material(sh);
            if (cachedDoomDemonTex != null) spriteMat.mainTexture = cachedDoomDemonTex;
            spriteMat.color = origSpriteTint;
            quad.GetComponent<Renderer>().material = spriteMat;

            // Invisible 3D Torso & Headshot Hitbox Colliders for crisp FPS gunplay & 1-tap headshots!
            GameObject bodyHit = new GameObject("Torso");
            bodyHit.transform.SetParent(transform, false);
            BoxCollider bc = bodyHit.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.05f * s, 0f);
            bc.size = new Vector3(1.15f * s, 1.50f * s, 0.80f * s);

            GameObject headHit = new GameObject("HeadHitbox");
            headHit.transform.SetParent(transform, false);
            BoxCollider hc = headHit.AddComponent<BoxCollider>();
            hc.center = new Vector3(0f, 2.08f * s, 0f);
            hc.size = new Vector3(0.68f * s, 0.62f * s, 0.68f * s);

            // Floating 3D Health Bar above the demon
            CreateHealthBar(2.68f * s, new Color(0.32f, 0.96f, 0.18f));
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

            CreateHealthBar((isBoss ? 3.05f : 2.82f) * s, isBoss ? glowCol : new Color(1f, 0.23f, 0.13f));
        }

        private void CreateHealthBar(float heightY, Color fillColor)
        {
            float s = Scale;
            GameObject hbRoot = new GameObject("HealthBar");
            hbRoot.transform.SetParent(transform, false);
            hbRoot.transform.localPosition = new Vector3(0f, heightY, 0f);
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
            fill.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeUnlitMat(fillColor);
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
                if (hitFlashTimer <= 0f)
                {
                    if (skinMat != null) skinMat.color = origSkinColor;
                    if (spriteMat != null && acidWindupTimer <= 0f) spriteMat.color = origSpriteTint;
                }
            }

            if (FPSPlayerController.Instance == null) return;
            Vector3 playerPos = FPSPlayerController.Instance.transform.position;

            if (healthBarRoot != null && Camera.main != null)
                healthBarRoot.LookAt(Camera.main.transform);

            float timeScale = GameManager.Instance.OverdriveActive ? 0.55f : 1.0f;
            float dt = Time.deltaTime * timeScale;
            if (cooldownTimer > 0f) cooldownTimer -= dt;
            if (acidCooldownTimer > 0f) acidCooldownTimer -= dt;

            Vector3 toPlayer = playerPos - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.01f ? toPlayer / dist : Vector3.forward;

            // =========================================================================
            // NON-BOSS DOOM SPRITE DEMON AI:
            // Lively stalking stride + direct melee claw rush + Toxic Acid Spit stream!
            // =========================================================================
            if (usesSpriteDemon)
            {
                UpdateSpriteDemonAI(dt, dist, dir);
                return;
            }

            // =========================================================================
            // UNIQUE REALM FINAL BOSS 3D AI (Levels 10, 20, ..., 100)
            // =========================================================================
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (wingLeft != null && wingRight != null)
            {
                float flap = Mathf.Sin(Time.time * 4.5f) * 14f;
                wingLeft.localRotation = Quaternion.Euler(0f, 24f + flap, 22f);
                wingRight.localRotation = Quaternion.Euler(0f, -24f - flap, -22f);
            }
            if (haloRing != null) haloRing.Rotate(0f, 0f, 75f * dt);

            walkPhase += dt * 8.5f;
            float swing = Mathf.Sin(walkPhase) * 28f;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-swing * 0.7f, 0f, 0f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(swing * 0.7f, 0f, 0f);

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
                if (isBoss && bossProfile != null) ExecuteUniqueBossAttack(dir);
                if (canBlink && Random.value < 0.65f)
                {
                    Vector3 blink = new Vector3(-dir.z, 0f, dir.x) * strafeDir * 5.5f;
                    MoveInArena(blink);
                }
            }
        }

        private void UpdateSpriteDemonAI(float dt, float dist, Vector3 dir)
        {
            // Keep billboard facing camera horizontally just like classic DOOM / DOOM 64!
            if (Camera.main != null)
            {
                Vector3 toCam = Camera.main.transform.position - transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            }

            // Periodic weave direction shift so demons aggressively zig-zag while rushing
            weaveTimer -= dt;
            if (weaveTimer <= 0f)
            {
                weaveTimer = Random.Range(1.1f, 2.5f);
                strafeDir *= -1f;
            }

            bool isMoving = false;

            // 1. Active Forward Claw Lunge ("sila mismo mag aatake"!)
            if (lungeTimer > 0f)
            {
                lungeTimer -= dt;
                if (dist > 1.35f) MoveInArena(lungeDir * (moveSpeed * 2.1f) * dt);
                isMoving = true;
            }
            // 2. Active Acid Spit Windup Telegraph (flashes toxic neon-green before spitting!)
            else if (acidWindupTimer > 0f)
            {
                acidWindupTimer -= dt;
                if (spriteMat != null && hitFlashTimer <= 0f)
                {
                    float gPulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 28f);
                    spriteMat.color = Color.Lerp(origSpriteTint, new Color(0.38f, 1.0f, 0.22f, 1f), gPulse);
                }
                if (acidWindupTimer <= 0f)
                {
                    if (spriteMat != null) spriteMat.color = origSpriteTint;
                    SpitAcidStream(dir);
                }
            }
            else
            {
                // 3. Aggressive Hunting & Weaving Pursuit Toward Player
                if (dist > 2.0f)
                {
                    Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * strafeDir;
                    float weaveWeight = dist > 5.5f ? 0.36f : 0.16f;
                    Vector3 desiredMove = (dir + perp * weaveWeight).normalized;
                    // Smooth obstacle avoidance around tactical cover blocks
                    desiredMove = SteerAroundCover(desiredMove);
                    MoveInArena(desiredMove * moveSpeed * dt);
                    isMoving = true;
                }

                // 4. Direct Melee Claw Attack when close to player!
                if (dist <= attackRange && cooldownTimer <= 0f)
                {
                    cooldownTimer = attackCooldown;
                    lungeTimer = 0.24f;
                    lungeDir = dir;
                    if (GameManager.Instance != null)
                        GameManager.Instance.SpawnBurstParticles(FPSPlayerController.Instance.transform.position - dir * 0.5f, new Color(1f, 0.14f, 0.08f), 10);
                    FPSPlayerController.Instance.TakeDamage(Mathf.RoundToInt(attackDamage), displayName);
                }
                // 5. Toxic Acid Spit Ability at Mid-Range (NOT round balls — spitting corrosive acid stream!)
                else if (dist >= 4.2f && dist <= 18.5f && acidCooldownTimer <= 0f)
                {
                    acidCooldownTimer = Random.Range(2.9f, 4.4f);
                    acidWindupTimer = 0.36f;
                    if (GameManager.Instance != null)
                        GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * (1.92f * Scale), new Color(0.32f, 1.0f, 0.16f), 8);
                }
            }

            // =========================================================================
            // LIVELY DOOM SPRITE MOVEMENT ANIMATION (Footstep hop, predatory sway,
            // stride mirror flip, lunge scale-punch, acid spit inhale/snap!)
            // =========================================================================
            if (spritePivot != null && spriteQuad != null)
            {
                if (isMoving) walkPhase += dt * (moveSpeed * 1.48f);
                else walkPhase += dt * 3.2f; // Subtle menacing breathing idle

                float stepSin = Mathf.Sin(walkPhase);
                float hopY = isMoving ? Mathf.Abs(stepSin) * 0.18f * Scale : Mathf.Sin(walkPhase) * 0.03f * Scale;
                float swayZ = isMoving ? stepSin * 7.5f : Mathf.Sin(walkPhase) * 2.0f;
                float strideSign = (isMoving && Mathf.Cos(walkPhase * 0.5f) < 0f) ? -1f : 1f;

                float scaleXMult = 1f + (isMoving ? Mathf.Abs(stepSin) * 0.06f : 0f);
                float scaleYMult = 1f - (isMoving ? Mathf.Abs(stepSin) * 0.04f : Mathf.Sin(walkPhase) * 0.025f);
                float lungeFwdZ = 0f;

                if (lungeTimer > 0f)
                {
                    float lp = lungeTimer / 0.24f;
                    scaleXMult = 1.18f;
                    scaleYMult = 1.12f;
                    swayZ = (strafeDir > 0 ? -12f : 12f) * lp;
                    lungeFwdZ = -0.35f * lp; // Lunges forward toward camera!
                }
                else if (acidWindupTimer > 0f)
                {
                    // Inhales & puffs chest right before spitting toxic acid
                    float wp = 1f - (acidWindupTimer / 0.36f);
                    scaleXMult = 1f + wp * 0.14f;
                    scaleYMult = 1f - wp * 0.08f;
                    hopY = 0.06f * wp;
                }
                else if (hitFlashTimer > 0f)
                {
                    // Stagger recoil punch when shot
                    swayZ = 14f * (hitFlashTimer / 0.12f);
                    scaleYMult = 0.92f;
                }

                spritePivot.localPosition = new Vector3(0f, hopY, lungeFwdZ);
                spritePivot.localRotation = Quaternion.Euler(0f, 0f, swayZ);
                spriteQuad.localScale = new Vector3(baseSpriteW * scaleXMult, baseSpriteH * scaleYMult, 1f);
                if (spriteMat != null)
                {
                    spriteMat.mainTextureScale = new Vector2(strideSign, 1f);
                    spriteMat.mainTextureOffset = new Vector2(strideSign < 0f ? 1f : 0f, 0f);
                }
            }
        }

        /// <summary>
        /// Spits a directional stream of 3 elongated, irregular toxic-green acid splash globs
        /// (NOT round spheres!) with parabolic drop that corrode and drain player Health over time!
        /// </summary>
        private void SpitAcidStream(Vector3 dir)
        {
            if (FPSPlayerController.Instance == null) return;
            Vector3 mouthPos = transform.position + Vector3.up * (1.88f * Scale) + dir * 0.65f;
            Vector3 targetPos = FPSPlayerController.Instance.transform.position - Vector3.up * 0.08f;
            float dist = Vector3.Distance(mouthPos, targetPos);
            float speed = 18.5f;
            float flightTime = Mathf.Clamp(dist / speed, 0.15f, 1.2f);
            float grav = 7.2f;

            // Compute ballistic spit arc so acid lands right onto the player!
            Vector3 baseVel = (targetPos - mouthPos) / flightTime;
            baseVel.y += 0.5f * grav * flightTime;

            Color acidCore = new Color(0.32f, 1.0f, 0.14f);
            if (GameManager.Instance != null)
                GameManager.Instance.SpawnBurstParticles(mouthPos, acidCore, 10);

            // Spit 3 elongated splashing acid streaks (stretched jagged droplets, NEVER round balls!)
            for (int i = 0; i < 3; i++)
            {
                float spreadYaw = (i - 1) * 5.5f + Random.Range(-1.8f, 1.8f);
                float speedScale = 1.0f - i * 0.06f;
                Vector3 vel = Quaternion.Euler(0f, spreadYaw, 0f) * baseVel * speedScale;

                GameObject spit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spit.name = "DemonAcidSpit";
                Destroy(spit.GetComponent<Collider>());
                spit.transform.position = mouthPos + dir * (i * 0.24f);
                // Thin elongated liquid spit splash shape aligned along velocity (NOT round!)
                spit.transform.localScale = new Vector3(0.13f, 0.09f, 0.78f - i * 0.12f);
                spit.transform.rotation = Quaternion.LookRotation(vel.normalized);
                spit.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeEmissiveMat(
                    i == 0 ? new Color(0.45f, 1.0f, 0.18f) : acidCore, 2.9f);

                var proj = spit.AddComponent<Projectile3D>();
                proj.velocity = vel;
                proj.gravity = grav;
                proj.isAcidSpit = true;
                proj.damage = Mathf.Max(4f, attackDamage * 0.55f);
                proj.acidTickDamage = acidTickDmg;
                proj.fromPlayer = false;
                proj.sourceName = displayName;
            }
        }

        private Vector3 SteerAroundCover(Vector3 desiredDir)
        {
            if (GameManager.Instance == null || GameManager.Instance.worldBuilder == null) return desiredDir;
            Vector3 probe = transform.position + desiredDir * 1.6f;
            probe.y = 1.0f;
            foreach (var b in GameManager.Instance.worldBuilder.RadarObstacles)
            {
                // Ignore outer perimeter walls; only steer around inner tactical cover blocks
                if (b.size.x > 20f || b.size.z > 20f) continue;
                Bounds expanded = new Bounds(b.center, b.size + new Vector3(1.5f, 2f, 1.5f));
                if (expanded.Contains(probe))
                {
                    Vector3 side = new Vector3(-desiredDir.z, 0f, desiredDir.x) * strafeDir;
                    return (desiredDir * 0.25f + side).normalized;
                }
            }
            return desiredDir;
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
            orb.name = "BossProjectile";
            Destroy(orb.GetComponent<Collider>());
            orb.transform.position = spawnPos;
            orb.transform.localScale = Vector3.one * 0.62f;
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
            if (spriteMat != null) spriteMat.color = new Color(1.4f, 0.45f, 0.45f, 1f);

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
                    Color deathCol = isBoss ? projColor : new Color(0.36f, 0.96f, 0.18f);
                    GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * 1.4f, deathCol, isBoss ? 42 : 22);
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
