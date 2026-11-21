using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Demonic Enemy & Unique Realm Boss AI (Levels 1-100):
    /// - Non-Boss Levels (1-9, 11-19, ..., 91-99): Spawns ALL 13 distinct pixel-art demons from the
    ///   uploaded Hell-Legion roster (Bone Stalker, Spine Ravager, Twin-Blade Wraith, Horned Executioner,
    ///   Flying Trident Archfiend, Winged Blood-Imp, Crimson Halberdier, Greatsword Champion, Hell Lasher,
    ///   Crimson Ogre Brute, Fallen Death-Knight, Scimitar Fiend, and Hellfire Warlord) with exact native
    ///   aspect ratios, hovering wing motion for flyers, lively stalking/weaving stride movement, direct
    ///   melee rush lunges, and a Toxic Acid Spit splash-stream ability (NO round balls!).
    /// - Final Boss Levels (10, 20, 30, ..., 100): Keeps all 10 unique giant 3D Realm Final Bosses!
    /// </summary>
    public class DemonEnemyAI : MonoBehaviour
    {
        private struct DemonVariantDef
        {
            public string resourceName;
            public string displayName;
            public float baseHp;
            public float moveSpeed;
            public float attackDamage;
            public float attackCooldown;
            public float heightWorld;
            public int scoreValue;
            public bool isFlying;
            public bool canBlink;
        }

        private static readonly DemonVariantDef[] RosterDefs = new DemonVariantDef[]
        {
            // Row 1: Spiked Bone & Ash Demons (01-04)
            new DemonVariantDef { resourceName = "Enemies/demon_01", displayName = "BONE STALKER",       baseHp = 78f,  moveSpeed = 7.3f, attackDamage = 11f, attackCooldown = 0.95f, heightWorld = 2.38f, scoreValue = 170, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_02", displayName = "SPINE RAVAGER",      baseHp = 135f, moveSpeed = 6.1f, attackDamage = 15f, attackCooldown = 1.05f, heightWorld = 2.32f, scoreValue = 260, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_03", displayName = "TWIN-BLADE WRAITH",  baseHp = 115f, moveSpeed = 7.4f, attackDamage = 16f, attackCooldown = 0.88f, heightWorld = 2.55f, scoreValue = 320, isFlying = false, canBlink = true  },
            new DemonVariantDef { resourceName = "Enemies/demon_04", displayName = "HORNED EXECUTIONER", baseHp = 150f, moveSpeed = 6.0f, attackDamage = 18f, attackCooldown = 1.12f, heightWorld = 2.36f, scoreValue = 300, isFlying = false, canBlink = false },
            // Row 2: Winged Flying Crimson Fiends (05-06)
            new DemonVariantDef { resourceName = "Enemies/demon_05", displayName = "TRIDENT ARCHFIEND",  baseHp = 175f, moveSpeed = 6.8f, attackDamage = 17f, attackCooldown = 1.00f, heightWorld = 2.80f, scoreValue = 400, isFlying = true,  canBlink = true  },
            new DemonVariantDef { resourceName = "Enemies/demon_06", displayName = "WINGED BLOOD-IMP",   baseHp = 85f,  moveSpeed = 8.1f, attackDamage = 12f, attackCooldown = 0.90f, heightWorld = 2.05f, scoreValue = 230, isFlying = true,  canBlink = false },
            // Row 3: Crimson Hell-Legion Warriors (07-13)
            new DemonVariantDef { resourceName = "Enemies/demon_07", displayName = "CRIMSON HALBERDIER", baseHp = 120f, moveSpeed = 6.6f, attackDamage = 15f, attackCooldown = 1.00f, heightWorld = 2.38f, scoreValue = 270, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_08", displayName = "GREATSWORD CHAMPION",baseHp = 195f, moveSpeed = 5.7f, attackDamage = 20f, attackCooldown = 1.15f, heightWorld = 2.62f, scoreValue = 420, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_09", displayName = "HELL LASHER",        baseHp = 74f,  moveSpeed = 7.8f, attackDamage = 11f, attackCooldown = 0.85f, heightWorld = 2.32f, scoreValue = 180, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_10", displayName = "CRIMSON OGRE BRUTE", baseHp = 240f, moveSpeed = 5.0f, attackDamage = 22f, attackCooldown = 1.25f, heightWorld = 2.75f, scoreValue = 460, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_11", displayName = "FALLEN DEATH-KNIGHT",baseHp = 165f, moveSpeed = 6.4f, attackDamage = 16f, attackCooldown = 0.95f, heightWorld = 2.24f, scoreValue = 350, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_12", displayName = "SCIMITAR FIEND",     baseHp = 110f, moveSpeed = 7.1f, attackDamage = 14f, attackCooldown = 0.92f, heightWorld = 2.35f, scoreValue = 260, isFlying = false, canBlink = false },
            new DemonVariantDef { resourceName = "Enemies/demon_13", displayName = "HELLFIRE WARLORD",   baseHp = 185f, moveSpeed = 6.5f, attackDamage = 19f, attackCooldown = 1.02f, heightWorld = 2.45f, scoreValue = 410, isFlying = false, canBlink = true  }
        };

        private static Texture2D[] cachedRosterTextures;
        private static int[] rosterBag;
        private static int rosterBagIndex;

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

        // Non-Boss 13-Demon Roster Sprite Visuals
        private bool usesSpriteDemon;
        private bool isFlyingDemon;
        private float hoverBaseY;
        private Transform spritePivot;
        private Transform spriteQuad;
        private Material spriteMat;
        private Color origSpriteTint = Color.white;
        private float baseSpriteW = 1.85f;
        private float baseSpriteH = 2.40f;

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

        private static int DrawNextRosterIndex()
        {
            if (rosterBag == null || rosterBag.Length != RosterDefs.Length)
            {
                rosterBag = new int[RosterDefs.Length];
                for (int i = 0; i < rosterBag.Length; i++) rosterBag[i] = i;
                rosterBagIndex = rosterBag.Length;
            }
            if (rosterBagIndex >= rosterBag.Length)
            {
                // Fisher-Yates shuffle so all 13 demons appear evenly with no immediate duplicates!
                for (int i = rosterBag.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int tmp = rosterBag[i];
                    rosterBag[i] = rosterBag[j];
                    rosterBag[j] = tmp;
                }
                rosterBagIndex = 0;
            }
            return rosterBag[rosterBagIndex++];
        }

        private static Texture2D GetRosterTexture(int variantIdx)
        {
            if (cachedRosterTextures == null || cachedRosterTextures.Length != RosterDefs.Length)
            {
                cachedRosterTextures = new Texture2D[RosterDefs.Length];
                for (int i = 0; i < RosterDefs.Length; i++)
                {
                    Texture2D tex = Resources.Load<Texture2D>(RosterDefs[i].resourceName);
                    if (tex != null)
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                    }
                    cachedRosterTextures[i] = tex;
                }
            }
            int idx = Mathf.Clamp(variantIdx, 0, cachedRosterTextures.Length - 1);
            return cachedRosterTextures[idx];
        }

        /// <summary>
        /// Initialize a non-boss demon using ALL 13 pixel-art demons from the uploaded roster,
        /// lively stride/hover movement, direct melee rush lunges, and Toxic Acid Spit ability!
        /// </summary>
        public void Initialize(EnemyType type, float difficultyMult, int campaignLevel)
        {
            enemyType = type;
            isBoss = false;
            usesSpriteDemon = true;
            isRanged = false;
            strafeDir = Random.value < 0.5f ? 1f : -1f;
            walkPhase = Random.Range(0f, 6.28f);
            weaveTimer = Random.Range(1.0f, 2.4f);

            int variantIdx = DrawNextRosterIndex();
            DemonVariantDef def = RosterDefs[variantIdx];

            displayName = def.displayName;
            isFlyingDemon = def.isFlying;
            hoverBaseY = def.isFlying ? 0.65f : 0f;
            canBlink = def.canBlink;
            Scale = def.heightWorld / 2.40f;
            scoreValue = def.scoreValue;
            attackRange = def.isFlying ? 3.4f : 3.1f;
            attackCooldown = def.attackCooldown;
            origSpriteTint = Color.white;

            // Smooth Level 1 -> 100 scaling
            float lvlHpScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.055f;
            float lvlDmgScale = 1f + Mathf.Max(0, campaignLevel - 1) * 0.012f;
            hp = Mathf.Round(def.baseHp * difficultyMult * lvlHpScale);
            maxHp = hp;
            attackDamage = Mathf.Round(def.attackDamage * difficultyMult * lvlDmgScale);
            acidTickDmg = Mathf.Clamp(Mathf.RoundToInt((3.5f + campaignLevel * 0.06f) * difficultyMult), 3, 9);
            moveSpeed = def.moveSpeed * (1f + Mathf.Min(0.36f, campaignLevel * 0.0038f));
            cooldownTimer = Random.Range(0.25f, 0.75f);
            acidCooldownTimer = Random.Range(1.7f, 3.8f);

            BuildRosterSpriteDemon(variantIdx, def);
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

        private void BuildRosterSpriteDemon(int variantIdx, DemonVariantDef def)
        {
            Texture2D tex = GetRosterTexture(variantIdx);
            float aspect = (tex != null && tex.height > 0) ? ((float)tex.width / tex.height) : 0.78f;

            // Pivot for dynamic walking bounce / flying hover, lunge lean, and squash/stretch
            GameObject pivGo = new GameObject("DemonSpritePivot");
            pivGo.transform.SetParent(transform, false);
            pivGo.transform.localPosition = new Vector3(0f, hoverBaseY, 0f);
            spritePivot = pivGo.transform;

            // Ground Contact Shadow Disc
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "DemonShadow";
            Destroy(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            float shScale = def.isFlying ? 1.35f : 1.15f * Scale;
            shadow.transform.localScale = new Vector3(shScale, 0.01f, shScale);
            shadow.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeUnlitMat(new Color(0.05f, 0.02f, 0.02f, 0.82f));

            // Transparent Pixel-Art Camera-Facing Demon Sprite Quad (Exact Native Aspect Ratio!)
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DemonSpriteVisual";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(spritePivot, false);
            baseSpriteH = def.heightWorld;
            baseSpriteW = baseSpriteH * aspect;
            quad.transform.localPosition = new Vector3(0f, baseSpriteH * 0.5f, 0f);
            quad.transform.localScale = new Vector3(baseSpriteW, baseSpriteH, 1f);
            spriteQuad = quad.transform;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            spriteMat = new Material(sh);
            if (tex != null) spriteMat.mainTexture = tex;
            spriteMat.color = origSpriteTint;
            quad.GetComponent<Renderer>().material = spriteMat;

            // Invisible 3D Torso & Headshot Hitbox Colliders scaled to exact sprite dimensions!
            float hitW = Mathf.Clamp(baseSpriteW * 0.66f, 0.95f, 1.95f);
            GameObject bodyHit = new GameObject("Torso");
            bodyHit.transform.SetParent(transform, false);
            BoxCollider bc = bodyHit.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, hoverBaseY + baseSpriteH * 0.42f, 0f);
            bc.size = new Vector3(hitW, baseSpriteH * 0.62f, 0.85f);

            GameObject headHit = new GameObject("HeadHitbox");
            headHit.transform.SetParent(transform, false);
            BoxCollider hc = headHit.AddComponent<BoxCollider>();
            hc.center = new Vector3(0f, hoverBaseY + baseSpriteH * 0.83f, 0f);
            hc.size = new Vector3(Mathf.Max(0.68f, hitW * 0.62f), baseSpriteH * 0.28f, 0.72f);

            // Floating 3D Health Bar cleanly above the demon's head/wings
            CreateHealthBar(hoverBaseY + baseSpriteH + 0.28f, new Color(0.95f, 0.22f, 0.18f));
        }

        private void BuildArticulated3DModel(Color armorCol, Color skinCol, Color glowCol,
                                             bool withWings, bool withHalo, bool withCannons)
        {
            float s = Scale;
            Material armorMat = CitadelWorldBuilder.MakeStdMat(armorCol, 0.70f, 0.58f);
            skinMat = CitadelWorldBuilder.MakeStdMat(skinCol, 0.18f, 0.32f);
            origSkinColor = skinCol;
            Material glowMat = CitadelWorldBuilder.MakeEmissiveMat(glowCol, 2.6f);

            leftLeg = CreatePart("LeftLeg", PrimitiveType.Cube, new Vector3(-0.22f * s, 0.42f * s, 0f), new Vector3(0.28f * s, 0.85f * s, 0.28f * s), armorMat, false).transform;
            rightLeg = CreatePart("RightLeg", PrimitiveType.Cube, new Vector3(0.22f * s, 0.42f * s, 0f), new Vector3(0.28f * s, 0.85f * s, 0.28f * s), armorMat, false).transform;

            CreatePart("Torso", PrimitiveType.Cube, new Vector3(0f, 1.32f * s, 0f), new Vector3(0.82f * s, 1.05f * s, 0.48f * s), skinMat, true);
            CreatePart("ChestPlate", PrimitiveType.Cube, new Vector3(0f, 1.44f * s, 0f), new Vector3(0.88f * s, 0.62f * s, 0.52f * s), armorMat, false);
            CreatePart("HellCore", PrimitiveType.Cube, new Vector3(0f, 1.42f * s, 0f), new Vector3(0.28f * s, 0.28f * s, 0.56f * s), glowMat, false);

            CreatePart("HeadHitbox", PrimitiveType.Cube, new Vector3(0f, 2.08f * s, 0f), new Vector3(0.48f * s, 0.48f * s, 0.48f * s), skinMat, true);
            CreatePart("EyeL", PrimitiveType.Cube, new Vector3(-0.11f * s, 2.12f * s, 0.25f * s), new Vector3(0.1f * s, 0.06f * s, 0.05f * s), glowMat, false);
            CreatePart("EyeR", PrimitiveType.Cube, new Vector3(0.11f * s, 2.12f * s, 0.25f * s), new Vector3(0.1f * s, 0.06f * s, 0.05f * s), glowMat, false);

            GameObject hornL = CreatePart("HornL", PrimitiveType.Cylinder, new Vector3(-0.26f * s, 2.44f * s, 0f), new Vector3(0.09f * s, 0.36f * s, 0.09f * s), glowMat, false);
            hornL.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            GameObject hornR = CreatePart("HornR", PrimitiveType.Cylinder, new Vector3(0.26f * s, 2.44f * s, 0f), new Vector3(0.09f * s, 0.36f * s, 0.09f * s), glowMat, false);
            hornR.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);

            leftArm = CreatePart("LeftArm", PrimitiveType.Cube, new Vector3(-0.58f * s, 1.35f * s, 0.1f * s), new Vector3(0.24f * s, 0.85f * s, 0.24f * s), skinMat, false).transform;
            rightArm = CreatePart("RightArm", PrimitiveType.Cube, new Vector3(0.58f * s, 1.35f * s, 0.1f * s), new Vector3(0.24f * s, 0.85f * s, 0.24f * s), skinMat, false).transform;
            GameObject blade = CreatePart("HellBlade", PrimitiveType.Cube, new Vector3(0f, -0.35f * s, 0.28f * s), new Vector3(0.08f * s, 0.14f * s, 0.65f * s), glowMat, false);
            blade.transform.SetParent(rightArm, false);

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

            CreateHealthBar(3.05f * s, glowCol);
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

            if (usesSpriteDemon)
            {
                UpdateSpriteDemonAI(dt, dist, dir);
                return;
            }

            // UNIQUE REALM FINAL BOSS 3D AI (Levels 10, 20, ..., 100)
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
            // Keep billboard facing camera horizontally cleanly
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

            weaveTimer -= dt;
            if (weaveTimer <= 0f)
            {
                weaveTimer = Random.Range(1.0f, 2.4f);
                strafeDir *= -1f;
            }

            bool isMoving = false;

            if (lungeTimer > 0f)
            {
                lungeTimer -= dt;
                if (dist > 1.35f) MoveInArena(lungeDir * (moveSpeed * 2.15f) * dt);
                isMoving = true;
            }
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
                if (dist > 2.0f)
                {
                    Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * strafeDir;
                    float weaveWeight = isFlyingDemon ? 0.50f : (dist > 5.5f ? 0.36f : 0.16f);
                    Vector3 desiredMove = (dir + perp * weaveWeight).normalized;
                    if (!isFlyingDemon) desiredMove = SteerAroundCover(desiredMove);
                    MoveInArena(desiredMove * moveSpeed * dt);
                    isMoving = true;
                }

                // Direct Melee Weapon / Claw Rush Attack ("sila mismo mag aatake"!)
                if (dist <= attackRange && cooldownTimer <= 0f)
                {
                    cooldownTimer = attackCooldown;
                    lungeTimer = 0.24f;
                    lungeDir = dir;
                    if (GameManager.Instance != null)
                        GameManager.Instance.SpawnBurstParticles(FPSPlayerController.Instance.transform.position - dir * 0.5f, new Color(1f, 0.14f, 0.08f), 10);
                    FPSPlayerController.Instance.TakeDamage(Mathf.RoundToInt(attackDamage), displayName);
                }
                // Toxic Acid Spit Ability at Mid-Range (NOT round balls — corrosive acid stream!)
                else if (dist >= 4.2f && dist <= 18.5f && acidCooldownTimer <= 0f)
                {
                    acidCooldownTimer = Random.Range(2.9f, 4.4f);
                    acidWindupTimer = 0.36f;
                    if (GameManager.Instance != null)
                        GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * (hoverBaseY + baseSpriteH * 0.78f), new Color(0.32f, 1.0f, 0.16f), 8);
                }
            }

            // LIVELY MOVEMENT ANIMATION (Running stride hop + sway, or soaring hover wing-bob for flyers!)
            if (spritePivot != null && spriteQuad != null)
            {
                walkPhase += dt * (isFlyingDemon ? 6.2f : (isMoving ? moveSpeed * 1.48f : 3.2f));

                float stepSin = Mathf.Sin(walkPhase);
                float hopY = isFlyingDemon
                    ? hoverBaseY + Mathf.Sin(walkPhase) * 0.30f
                    : (isMoving ? Mathf.Abs(stepSin) * 0.18f * Scale : Mathf.Sin(walkPhase) * 0.03f * Scale);
                float swayZ = isFlyingDemon
                    ? Mathf.Cos(walkPhase * 0.7f) * 9.5f
                    : (isMoving ? stepSin * 7.5f : Mathf.Sin(walkPhase) * 2.0f);
                float strideSign = (isMoving && !isFlyingDemon && Mathf.Cos(walkPhase * 0.5f) < 0f) ? -1f : 1f;

                float scaleXMult = 1f + (isFlyingDemon ? Mathf.Sin(walkPhase * 2f) * 0.07f : (isMoving ? Mathf.Abs(stepSin) * 0.06f : 0f));
                float scaleYMult = 1f - (isFlyingDemon ? Mathf.Sin(walkPhase * 2f) * 0.05f : (isMoving ? Mathf.Abs(stepSin) * 0.04f : Mathf.Sin(walkPhase) * 0.025f));
                float lungeFwdZ = 0f;

                if (lungeTimer > 0f)
                {
                    float lp = lungeTimer / 0.24f;
                    scaleXMult = 1.18f;
                    scaleYMult = 1.12f;
                    swayZ = (strafeDir > 0 ? -13f : 13f) * lp;
                    lungeFwdZ = -0.38f * lp;
                }
                else if (acidWindupTimer > 0f)
                {
                    float wp = 1f - (acidWindupTimer / 0.36f);
                    scaleXMult = 1f + wp * 0.15f;
                    scaleYMult = 1f - wp * 0.08f;
                    hopY += 0.06f * wp;
                }
                else if (hitFlashTimer > 0f)
                {
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

        private void SpitAcidStream(Vector3 dir)
        {
            if (FPSPlayerController.Instance == null) return;
            Vector3 mouthPos = transform.position + Vector3.up * (hoverBaseY + baseSpriteH * 0.78f) + dir * 0.65f;
            Vector3 targetPos = FPSPlayerController.Instance.transform.position - Vector3.up * 0.08f;
            float dist = Vector3.Distance(mouthPos, targetPos);
            float speed = 18.5f;
            float flightTime = Mathf.Clamp(dist / speed, 0.15f, 1.2f);
            float grav = 7.2f;

            Vector3 baseVel = (targetPos - mouthPos) / flightTime;
            baseVel.y += 0.5f * grav * flightTime;

            Color acidCore = new Color(0.32f, 1.0f, 0.14f);
            if (GameManager.Instance != null)
                GameManager.Instance.SpawnBurstParticles(mouthPos, acidCore, 10);

            for (int i = 0; i < 3; i++)
            {
                float spreadYaw = (i - 1) * 5.5f + Random.Range(-1.8f, 1.8f);
                float speedScale = 1.0f - i * 0.06f;
                Vector3 vel = Quaternion.Euler(0f, spreadYaw, 0f) * baseVel * speedScale;

                GameObject spit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spit.name = "DemonAcidSpit";
                Destroy(spit.GetComponent<Collider>());
                spit.transform.position = mouthPos + dir * (i * 0.24f);
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
                case BossAttackPattern.Fan3: FireFan(dir, 3, 24f); break;
                case BossAttackPattern.Fan5Mortar: FireFan(dir, 5, 40f); break;
                case BossAttackPattern.IceBlinkBurst: FireFan(dir, 3, 14f); break;
                case BossAttackPattern.Radial8Nova: FireRadialRing(8, Time.time * 25f); break;
                case BossAttackPattern.VoidTwinSpiral:
                    FireFan(dir, 4, 28f);
                    if (BossPhase >= 2) FireRadialRing(6, Time.time * 40f);
                    break;
                case BossAttackPattern.GatlingStream:
                    gatlingBurstRemaining = BossPhase >= 2 ? 6f : 4f;
                    gatlingBurstTick = 0f;
                    break;
                case BossAttackPattern.BloodSeraph7: FireFan(dir, 7, 52f); break;
                case BossAttackPattern.StormHyperVolley: FireFan(dir, 5, 24f); break;
                case BossAttackPattern.SolarRing10:
                    FireRadialRing(10, Time.time * 30f);
                    FireFan(dir, 3, 18f);
                    break;
                case BossAttackPattern.MalakorApocalypse:
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
            if (spriteMat != null) spriteMat.color = new Color(1.45f, 0.42f, 0.42f, 1f);

            if (GameManager.Instance != null) GameManager.Instance.AddResonance(resonanceBonus);

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
                    Color deathCol = isBoss ? projColor : new Color(0.92f, 0.18f, 0.16f);
                    GameManager.Instance.SpawnBurstParticles(transform.position + Vector3.up * (hoverBaseY + 1.3f), deathCol, isBoss ? 42 : 22);
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
