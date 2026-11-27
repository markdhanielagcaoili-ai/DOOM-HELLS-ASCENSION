using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// 5 Distinct VALORANT-Inspired Demonic 3D Weapon Models with Ultra-Detailed Sculpted Gothic
    /// REAVER VANDAL (deep midnight-indigo forged plates, polished gunmetal-silver filigree blades,
    /// dorsal spikes, 4-claw muzzle crown, glowing carved electric-violet runes, gothic holo sight,
    /// drifting dark-purple shadow wisps, spring-physics Valorant recoil, spectral star muzzle flash,
    /// telekinetic floating-magazine reload, and 2-stage inspect showcase), plus Inferno Phantom,
    /// Hell-Judge Drum Shotgun, Soul-Shredder Odin, Reaver Operator, and 3D Melee Swings ([V]).
    /// </summary>
    public class WeaponViewmodel3D : MonoBehaviour
    {
        public Transform weaponRig;
        public Transform gunPivot;
        public GameObject[] weaponModels = new GameObject[5];
        public Transform[] weaponMags = new Transform[5];
        public Transform[] weaponBolts = new Transform[5];
        public Transform[] muzzlePoints = new Transform[5];
        public Transform odinTopCover;
        public GameObject muzzleFlashObj;
        public Light muzzleLight;
        public Transform meleeGroup;

        [SerializeField] private Material meleeBladeMat;

        // Reaver Vandal dedicated animated references
        private Transform leftHandRig;
        private Transform rightHandRig;
        private Transform reaverMuzzleFlash;
        private Material reaverRuneMat;
        private Material reaverCoreGlowMat;
        private Material reaverSightDotMat;
        private Material reaverMuzzleFlashMat;
        private ParticleSystem reaverAmbientMistPS;
        private ParticleSystem reaverMuzzleSmokePS;
        private ParticleSystem reaverReloadAuraPS;

        // Dedicated Articulated 3D Melee Hand + Realistic Knife/Blade System ([0] Equip!)
        private GameObject[] meleeModels = new GameObject[4];
        private Transform meleeRightArmPivot;
        private Transform meleeLeftArmPivot;
        private Transform meleeWeaponSocket;
        private Transform karambitRingPivot;
        private Transform chainswordTeethRoot;
        private GameObject meleeSlashArcObj;
        private Material meleeSlashArcMat;
        private Material karambitRuneMat;
        private ParticleSystem karambitMistPS;

        private bool isMeleeEquipped;
        private int activeMeleeId = 0;
        private float meleeEquipTimer;
        private float meleeEquipDuration = 0.72f;
        private float meleeInspectTimer;
        private float meleeInspectDuration = 2.6f;
        private float meleeSwingDuration = 0.36f;
        private int meleeComboIndex = 0;
        private bool meleeLastHeavy = false;
        private bool quickMeleeActive = false;

        // Base poses
        private readonly Vector3 hipPos = new Vector3(0.245f, -0.225f, 0.455f);
        private readonly Vector3 hipEuler = new Vector3(1.2f, -3.8f, 0.8f);
        private readonly Vector3 adsPos = new Vector3(0.0f, -0.146f, 0.355f);
        private readonly Vector3 adsEuler = Vector3.zero;
        private readonly Vector3 meleeRigPos = new Vector3(0.0f, -0.19f, 0.44f);
        private readonly Vector3 meleeRightHandBasePos = new Vector3(0.23f, -0.045f, 0.02f);
        private readonly Vector3 meleeRightHandBaseEuler = new Vector3(10f, -18f, 12f);
        private readonly Vector3 meleeLeftHandBasePos = new Vector3(-0.26f, -0.11f, -0.02f);
        private readonly Vector3 meleeLeftHandBaseEuler = new Vector3(18f, 24f, -18f);

        private readonly Vector3 leftHandBasePos = new Vector3(-0.036f, -0.044f, 0.235f);
        private readonly Vector3 leftHandBaseEuler = Vector3.zero;
        private readonly Vector3 vandalMagBasePos = new Vector3(0f, -0.092f, 0.088f);

        private int activeWeaponId = 0;
        private Color activeVandalGlowColor = new Color(0.68f, 0.30f, 1.00f);

        // Damped spring recoil physics for authentic snappy VALORANT Vandal kick & spray feel
        private float recoilPosZ;
        private float recoilVelZ;
        private float recoilPitch;
        private float recoilPitchVel;
        private float recoilYaw;
        private float recoilYawVel;
        private float recoilRoll;
        private float recoilRollVel;
        private float sprayHeat;
        private int shotCounter;

        // Timers & VFX states
        private float muzzleTimer;
        private float muzzleDuration = 0.055f;
        private float equipTimer;
        private float inspectTimer;
        private float inspectDuration = 2.1f;
        private float boltCycleTimer;
        private float vandalBoltKick;
        private float meleeSwingTimer;
        private float reaverRuneFlare;
        private bool reloadLockFlared;
        private bool reloadBoltFlared;

        // Cached procedural assets (reused cleanly across shots/rebuilds)
        private static Texture2D cachedWispTex;
        private static Texture2D cachedMuzzleStarTex;
        private static Texture2D cachedReaverBodyTex;
        private static Texture2D cachedReaverSilverTex;
        private static Texture2D cachedReaverRuneTex;
        private static Texture2D cachedReaverCrestTex;
        private static Texture2D cachedGripTex;
        private static Material cachedSpectralTracerCoreMat;
        private static Material cachedSpectralTracerAuraMat;

        private void Start()
        {
            // Always build fresh at runtime so sculpted meshes, emissive runes, and mist wisps are active immediately
            BuildViewmodel();
        }

        public void BuildViewmodel()
        {
            if (weaponRig != null)
            {
                DestroySafe(weaponRig.gameObject);
                weaponRig = null;
            }
            if (meleeGroup != null)
            {
                DestroySafe(meleeGroup.gameObject);
                meleeGroup = null;
            }
            Transform staleRig = transform.Find("WeaponRig");
            if (staleRig != null) DestroySafe(staleRig.gameObject);
            Transform staleMelee = transform.Find("MeleeViewGroup");
            if (staleMelee != null) DestroySafe(staleMelee.gameObject);

            // Strip any legacy MuzzleLight gizmo objects if present
            if (muzzleLight != null)
            {
                DestroySafe(muzzleLight.gameObject);
                muzzleLight = null;
            }

            EnsureProceduralTextures();

            GameObject rigGo = new GameObject("WeaponRig");
            rigGo.transform.SetParent(transform, false);
            rigGo.transform.localPosition = hipPos;
            rigGo.transform.localEulerAngles = hipEuler;
            weaponRig = rigGo.transform;

            GameObject pivotGo = new GameObject("GunPivot");
            pivotGo.transform.SetParent(weaponRig, false);
            gunPivot = pivotGo.transform;

            // Shared high-craft PBR materials
            Material obsidianMetal = CitadelWorldBuilder.MakeStdMat(new Color(0.10f, 0.10f, 0.13f), 0.88f, 0.72f);
            Material darkPolymer = CitadelWorldBuilder.MakeStdMat(new Color(0.06f, 0.06f, 0.08f), 0.25f, 0.36f, cachedGripTex);
            Material silverChrome = CitadelWorldBuilder.MakeStdMat(new Color(0.70f, 0.74f, 0.80f), 0.94f, 0.84f, cachedReaverSilverTex);
            Material brassGold = CitadelWorldBuilder.MakeStdMat(new Color(0.92f, 0.72f, 0.25f), 0.90f, 0.76f);

            // Reaver Vandal authentic VALORANT palette (from HD reference: midnight indigo-purple + polished pewter silver + electric violet runes!)
            Material reaverIndigoPlate = CitadelWorldBuilder.MakeStdMat(Color.white, 0.56f, 0.64f, cachedReaverBodyTex);
            Material reaverDarkChassis = CitadelWorldBuilder.MakeStdMat(new Color(0.075f, 0.065f, 0.115f), 0.42f, 0.48f, cachedGripTex);
            Material reaverSilverFiligree = CitadelWorldBuilder.MakeStdMat(new Color(0.84f, 0.87f, 0.94f), 0.95f, 0.88f, cachedReaverSilverTex);
            Material reaverSteelDark = CitadelWorldBuilder.MakeStdMat(new Color(0.24f, 0.24f, 0.30f), 0.90f, 0.78f, cachedReaverSilverTex);

            // Carved glowing violet-indigo rune material (handguard scroll cutouts + mag windows)
            reaverRuneMat = MakeEmissiveTexturedMat(cachedReaverRuneTex, new Color(0.62f, 0.28f, 1.0f), 2.2f);
            Material reaverCrestMat = MakeEmissiveTexturedMat(cachedReaverCrestTex, new Color(0.68f, 0.32f, 1.0f), 2.1f);
            reaverCoreGlowMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.58f, 0.22f, 1.0f), 2.8f);
            reaverSightDotMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.80f, 0.48f, 1.0f), 3.6f);

            // Tactical armored gloves matching the dark lavender-slate + obsidian cuffs from the video
            Material gloveArmorMat = CitadelWorldBuilder.MakeStdMat(new Color(0.26f, 0.25f, 0.33f), 0.22f, 0.38f, cachedGripTex);
            Material gloveDarkMat = CitadelWorldBuilder.MakeStdMat(new Color(0.08f, 0.08f, 0.11f), 0.14f, 0.28f);
            Material cuffAccentMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.22f, 0.82f, 0.92f), 1.4f);

            // Other 4 weapon emissives
            Material phantomGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.14f, 0.88f, 1f), 2.8f);
            Material judgeGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.48f, 0.12f), 2.8f);
            Material odinGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.80f, 0.20f), 2.8f);
            Material opGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.72f, 0.24f, 1f), 3.2f);

            weaponModels = new GameObject[5];
            weaponMags = new Transform[5];
            weaponBolts = new Transform[5];
            muzzlePoints = new Transform[5];

            // =====================================================================
            // ARTICULATED TACTICAL HANDS RIG (Left hand detaches for Reaver telekinetic reload!)
            // =====================================================================
            GameObject handsRoot = new GameObject("HandsRig");
            handsRoot.transform.SetParent(gunPivot, false);

            GameObject lhGo = new GameObject("LeftHandRig");
            lhGo.transform.SetParent(handsRoot.transform, false);
            lhGo.transform.localPosition = leftHandBasePos;
            lhGo.transform.localEulerAngles = leftHandBaseEuler;
            leftHandRig = lhGo.transform;

            // Sculpted left palm + armored knuckle wrap + thumb + dark tactical forearm sleeve
            Part(leftHandRig, "LeftPalm", PrimitiveType.Cube, new Vector3(0.004f, -0.002f, 0.002f), new Vector3(0.068f, 0.052f, 0.092f), gloveDarkMat).transform.localRotation = Quaternion.Euler(4f, 0f, -15f);
            Part(leftHandRig, "LeftArmorBack", PrimitiveType.Cube, new Vector3(-0.018f, 0.006f, 0.004f), new Vector3(0.036f, 0.056f, 0.082f), gloveArmorMat).transform.localRotation = Quaternion.Euler(4f, 0f, -16f);
            Part(leftHandRig, "LeftThumb", PrimitiveType.Capsule, new Vector3(0.005f, 0.032f, 0.018f), new Vector3(0.022f, 0.030f, 0.024f), gloveDarkMat).transform.localRotation = Quaternion.Euler(52f, 16f, -32f);
            Part(leftHandRig, "LeftWristCuff", PrimitiveType.Cylinder, new Vector3(-0.036f, -0.044f, -0.072f), new Vector3(0.072f, 0.048f, 0.072f), gloveDarkMat).transform.localRotation = Quaternion.Euler(58f, 0f, 20f);
            Part(leftHandRig, "LeftCuffTrim", PrimitiveType.Cylinder, new Vector3(-0.050f, -0.064f, -0.104f), new Vector3(0.074f, 0.006f, 0.074f), cuffAccentMat).transform.localRotation = Quaternion.Euler(58f, 0f, 20f);
            Part(leftHandRig, "LeftForearm", PrimitiveType.Cylinder, new Vector3(-0.088f, -0.125f, -0.175f), new Vector3(0.095f, 0.145f, 0.095f), gloveDarkMat).transform.localRotation = Quaternion.Euler(58f, 0f, 22f);

            GameObject rhGo = new GameObject("RightHandRig");
            rhGo.transform.SetParent(handsRoot.transform, false);
            rhGo.transform.localPosition = new Vector3(0.012f, -0.078f, -0.118f);
            rightHandRig = rhGo.transform;
            Part(rightHandRig, "RightPalm", PrimitiveType.Cube, Vector3.zero, new Vector3(0.064f, 0.062f, 0.086f), gloveDarkMat).transform.localRotation = Quaternion.Euler(14f, 0f, 8f);
            Part(rightHandRig, "RightKnuckles", PrimitiveType.Cube, new Vector3(0.018f, 0.006f, 0.010f), new Vector3(0.034f, 0.050f, 0.074f), gloveArmorMat).transform.localRotation = Quaternion.Euler(14f, 0f, 8f);
            Part(rightHandRig, "RightForearm", PrimitiveType.Cylinder, new Vector3(0.048f, -0.090f, -0.115f), new Vector3(0.084f, 0.125f, 0.084f), gloveDarkMat).transform.localRotation = Quaternion.Euler(66f, 0f, -14f);

            // =====================================================================
            // MODEL 0: ULTRA-REALISTIC SCULPTED GOTHIC REAVER VANDAL (Matches 0:47-1:12!)
            // =====================================================================
            BuildReaverVandalModel(
                reaverIndigoPlate, reaverDarkChassis, reaverSilverFiligree,
                reaverSteelDark, reaverRuneMat, reaverCrestMat,
                reaverCoreGlowMat, reaverSightDotMat
            );

            // =====================================================================
            // MODEL 1: INFERNO PHANTOM (Sleek Integrated Suppressor + Cyan Holo Sight + Straight Mag)
            // =====================================================================
            GameObject phantom = new GameObject("Model_InfernoPhantom");
            phantom.transform.SetParent(gunPivot, false);
            weaponModels[1] = phantom;
            Part(phantom.transform, "Receiver", PrimitiveType.Cube, Vector3.zero, new Vector3(0.062f, 0.085f, 0.44f), obsidianMetal);
            Part(phantom.transform, "TopRail", PrimitiveType.Cube, new Vector3(0, 0.052f, 0.06f), new Vector3(0.040f, 0.016f, 0.50f), darkPolymer);
            Part(phantom.transform, "CyanCore", PrimitiveType.Cube, new Vector3(0, 0.005f, 0.14f), new Vector3(0.066f, 0.014f, 0.28f), phantomGlow);
            Part(phantom.transform, "Shroud", PrimitiveType.Cube, new Vector3(0, -0.002f, 0.30f), new Vector3(0.054f, 0.064f, 0.24f), obsidianMetal);
            Part(phantom.transform, "Suppressor", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.58f), new Vector3(0.048f, 0.19f, 0.048f), darkPolymer).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(phantom.transform, "SuppRing1", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.50f), new Vector3(0.052f, 0.008f, 0.052f), phantomGlow).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(phantom.transform, "SuppRing2", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.66f), new Vector3(0.052f, 0.008f, 0.052f), phantomGlow).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(phantom.transform, "HoloBoxL", PrimitiveType.Cube, new Vector3(-0.022f, 0.095f, -0.01f), new Vector3(0.005f, 0.044f, 0.065f), obsidianMetal);
            Part(phantom.transform, "HoloBoxR", PrimitiveType.Cube, new Vector3(0.022f, 0.095f, -0.01f), new Vector3(0.005f, 0.044f, 0.065f), obsidianMetal);
            Part(phantom.transform, "HoloBoxT", PrimitiveType.Cube, new Vector3(0, 0.116f, -0.01f), new Vector3(0.048f, 0.005f, 0.065f), obsidianMetal);
            Part(phantom.transform, "HoloReticle", PrimitiveType.Sphere, new Vector3(0, 0.096f, -0.01f), Vector3.one * 0.006f, phantomGlow);
            weaponBolts[1] = Part(phantom.transform, "PhantomBolt", PrimitiveType.Cube, new Vector3(0.036f, 0.014f, 0.02f), new Vector3(0.014f, 0.022f, 0.09f), silverChrome).transform;
            weaponMags[1] = Part(phantom.transform, "PhantomStraightMag", PrimitiveType.Cube, new Vector3(0, -0.11f, 0.08f), new Vector3(0.038f, 0.15f, 0.062f), darkPolymer).transform;
            Part(phantom.transform, "Grip", PrimitiveType.Cube, new Vector3(0, -0.085f, -0.12f), new Vector3(0.042f, 0.11f, 0.052f), darkPolymer).transform.localRotation = Quaternion.Euler(16f, 0, 0);
            muzzlePoints[1] = MakeEmpty(phantom.transform, "Muzzle1", new Vector3(0, 0.006f, 0.78f));

            // =====================================================================
            // MODEL 2: HELL-JUDGE SHOTGUN (Chunky Drum Magazine + Heavy Breacher Heat-Shield)
            // =====================================================================
            GameObject judge = new GameObject("Model_HellJudge");
            judge.transform.SetParent(gunPivot, false);
            weaponModels[2] = judge;
            Part(judge.transform, "Receiver", PrimitiveType.Cube, Vector3.zero, new Vector3(0.082f, 0.105f, 0.44f), obsidianMetal);
            Part(judge.transform, "HeatShield", PrimitiveType.Cube, new Vector3(0, 0.015f, 0.34f), new Vector3(0.072f, 0.078f, 0.32f), darkPolymer);
            Part(judge.transform, "EmberVents", PrimitiveType.Cube, new Vector3(0, 0.018f, 0.34f), new Vector3(0.076f, 0.018f, 0.24f), judgeGlow);
            Part(judge.transform, "ShotgunBarrel", PrimitiveType.Cylinder, new Vector3(0, 0.015f, 0.56f), new Vector3(0.044f, 0.14f, 0.044f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(judge.transform, "FrontPost", PrimitiveType.Cube, new Vector3(0, 0.078f, 0.46f), new Vector3(0.012f, 0.028f, 0.018f), judgeGlow);
            weaponBolts[2] = Part(judge.transform, "JudgeBolt", PrimitiveType.Cube, new Vector3(0.046f, 0.018f, 0.04f), new Vector3(0.022f, 0.022f, 0.09f), silverChrome).transform;
            GameObject drum = Part(judge.transform, "JudgeDrumMag", PrimitiveType.Cylinder, new Vector3(0, -0.10f, 0.12f), new Vector3(0.16f, 0.055f, 0.16f), darkPolymer);
            drum.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            weaponMags[2] = drum.transform;
            Part(judge.transform, "Grip", PrimitiveType.Cube, new Vector3(0, -0.09f, -0.12f), new Vector3(0.046f, 0.12f, 0.055f), darkPolymer).transform.localRotation = Quaternion.Euler(16f, 0, 0);
            muzzlePoints[2] = MakeEmpty(judge.transform, "Muzzle2", new Vector3(0, 0.015f, 0.72f));

            // =====================================================================
            // MODEL 3: SOUL-SHREDDER ODIN (Heavy LMG + Flip-Up Top Cover + Brass Ammo Belt!)
            // =====================================================================
            GameObject odin = new GameObject("Model_SoulShredderOdin");
            odin.transform.SetParent(gunPivot, false);
            weaponModels[3] = odin;
            Part(odin.transform, "HeavyBody", PrimitiveType.Cube, new Vector3(0, -0.01f, 0.04f), new Vector3(0.11f, 0.115f, 0.52f), obsidianMetal);
            GameObject coverPivot = new GameObject("OdinTopCoverPivot");
            coverPivot.transform.SetParent(odin.transform, false);
            coverPivot.transform.localPosition = new Vector3(0, 0.052f, 0.14f);
            Part(coverPivot.transform, "TopCoverPlate", PrimitiveType.Cube, new Vector3(0, 0.01f, -0.13f), new Vector3(0.095f, 0.025f, 0.28f), darkPolymer);
            Part(coverPivot.transform, "OdinReflex", PrimitiveType.Cube, new Vector3(0, 0.048f, -0.14f), new Vector3(0.068f, 0.042f, 0.05f), obsidianMetal);
            Part(coverPivot.transform, "OdinReflexDot", PrimitiveType.Sphere, new Vector3(0, 0.050f, -0.14f), Vector3.one * 0.008f, odinGlow);
            odinTopCover = coverPivot.transform;
            Part(odin.transform, "BarrelL", PrimitiveType.Cylinder, new Vector3(-0.024f, 0.01f, 0.48f), new Vector3(0.032f, 0.24f, 0.032f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(odin.transform, "BarrelR", PrimitiveType.Cylinder, new Vector3(0.024f, 0.01f, 0.48f), new Vector3(0.032f, 0.24f, 0.032f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(odin.transform, "OdinHeatCore", PrimitiveType.Cube, new Vector3(0, 0.01f, 0.38f), new Vector3(0.088f, 0.024f, 0.18f), odinGlow);
            weaponBolts[3] = Part(odin.transform, "OdinBolt", PrimitiveType.Cube, new Vector3(0.062f, 0.01f, 0.04f), new Vector3(0.024f, 0.024f, 0.09f), silverChrome).transform;
            GameObject boxMag = new GameObject("OdinBoxMag");
            boxMag.transform.SetParent(odin.transform, false);
            boxMag.transform.localPosition = new Vector3(-0.085f, -0.075f, 0.06f);
            Part(boxMag.transform, "AmmoBox", PrimitiveType.Cube, Vector3.zero, new Vector3(0.095f, 0.125f, 0.14f), darkPolymer);
            for (int b = 0; b < 5; b++)
            {
                Part(boxMag.transform, $"BeltBullet_{b}", PrimitiveType.Cylinder, new Vector3(0.04f, 0.075f, -0.04f + b * 0.022f), new Vector3(0.014f, 0.035f, 0.014f), brassGold).transform.localRotation = Quaternion.Euler(0, 0, 75f);
            }
            weaponMags[3] = boxMag.transform;
            muzzlePoints[3] = MakeEmpty(odin.transform, "Muzzle3", new Vector3(0, 0.01f, 0.74f));

            // =====================================================================
            // MODEL 4: REAVER OPERATOR (Ultra-Long Fluted Sniper + Giant Scope + Bolt-Action Handle!)
            // =====================================================================
            GameObject op = new GameObject("Model_ReaverOperator");
            op.transform.SetParent(gunPivot, false);
            weaponModels[4] = op;
            Part(op.transform, "SniperChassis", PrimitiveType.Cube, new Vector3(0, -0.005f, 0.02f), new Vector3(0.068f, 0.092f, 0.50f), reaverIndigoPlate);
            Part(op.transform, "OpEnergyCore", PrimitiveType.Cube, new Vector3(0, 0.012f, 0.10f), new Vector3(0.072f, 0.018f, 0.30f), opGlow);
            Part(op.transform, "OpBarrel", PrimitiveType.Cylinder, new Vector3(0, 0.016f, 0.58f), new Vector3(0.028f, 0.34f, 0.028f), reaverSilverFiligree).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "OpMuzzleBrake", PrimitiveType.Cube, new Vector3(0, 0.016f, 0.92f), new Vector3(0.056f, 0.042f, 0.095f), obsidianMetal);
            Part(op.transform, "ScopeMountF", PrimitiveType.Cube, new Vector3(0, 0.065f, 0.08f), new Vector3(0.026f, 0.045f, 0.024f), reaverSilverFiligree);
            Part(op.transform, "ScopeMountR", PrimitiveType.Cube, new Vector3(0, 0.065f, -0.08f), new Vector3(0.026f, 0.045f, 0.024f), reaverSilverFiligree);
            Part(op.transform, "ScopeBody", PrimitiveType.Cylinder, new Vector3(0, 0.105f, 0.02f), new Vector3(0.048f, 0.18f, 0.048f), darkPolymer).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "ScopeObjective", PrimitiveType.Cylinder, new Vector3(0, 0.105f, 0.18f), new Vector3(0.064f, 0.055f, 0.064f), reaverSilverFiligree).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "ScopeGlowLens", PrimitiveType.Sphere, new Vector3(0, 0.105f, -0.15f), new Vector3(0.042f, 0.042f, 0.01f), opGlow);
            GameObject opBoltPivot = new GameObject("OperatorBoltHandle");
            opBoltPivot.transform.SetParent(op.transform, false);
            opBoltPivot.transform.localPosition = new Vector3(0.034f, 0.02f, -0.06f);
            Part(opBoltPivot.transform, "BoltStem", PrimitiveType.Cube, new Vector3(0.026f, 0, 0), new Vector3(0.055f, 0.014f, 0.014f), silverChrome);
            Part(opBoltPivot.transform, "BoltKnob", PrimitiveType.Sphere, new Vector3(0.055f, -0.012f, 0), Vector3.one * 0.024f, silverChrome);
            weaponBolts[4] = opBoltPivot.transform;
            weaponMags[4] = Part(op.transform, "OpMag", PrimitiveType.Cube, new Vector3(0, -0.095f, 0.06f), new Vector3(0.042f, 0.11f, 0.085f), darkPolymer).transform;
            Part(op.transform, "Grip", PrimitiveType.Cube, new Vector3(0, -0.088f, -0.14f), new Vector3(0.042f, 0.11f, 0.052f), darkPolymer).transform.localRotation = Quaternion.Euler(16f, 0, 0);
            muzzlePoints[4] = MakeEmpty(op.transform, "Muzzle4", new Vector3(0, 0.016f, 0.98f));

            // Shared Muzzle Flash for Weapons 1-4 (No Point Light component so Unity Gizmos stay clean!)
            muzzleFlashObj = Part(gunPivot, "MuzzleFlash", PrimitiveType.Sphere, new Vector3(0f, 0.01f, 0.76f), new Vector3(0.11f, 0.11f, 0.24f), CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.80f, 0.25f), 4.5f));
            muzzleFlashObj.SetActive(false);
            muzzleLight = null;

            // =====================================================================
            // ULTRA-REALISTIC 3D MELEE WEAPONS ATTACHED TO ARTICULATED GLOVED HANDS ([0])
            // =====================================================================
            BuildArticulatedMeleeSystem(
                reaverIndigoPlate, reaverDarkChassis, reaverSilverFiligree,
                reaverSteelDark, reaverCoreGlowMat, gloveArmorMat,
                gloveDarkMat, cuffAccentMat, brassGold
            );

            SetActiveWeaponModel(activeWeaponId);
            if (Application.isPlaying && FPSPlayerController.Instance != null)
            {
                FPSPlayerController.Instance.ApplyAllEquippedSkinsToViewmodel();
            }
        }

        private void BuildArticulatedMeleeSystem(
            Material indigoPlate, Material darkChassis, Material silverFiligree,
            Material steelDark, Material violetGlow, Material gloveArmorMat,
            Material gloveDarkMat, Material cuffAccentMat, Material brassGold)
        {
            GameObject meleeGo = new GameObject("MeleeViewGroup");
            meleeGo.transform.SetParent(transform, false); // Mounted cleanly to Main Camera so Melee Rig moves independently of gun recoil!
            meleeGo.transform.localPosition = meleeRigPos;
            meleeGo.transform.localRotation = Quaternion.identity;
            meleeGroup = meleeGo.transform;

            meleeBladeMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.72f, 0.28f, 1.0f), 2.4f);
            karambitRuneMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.68f, 0.26f, 1.0f), 2.8f);
            Material crimsonGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1.0f, 0.14f, 0.12f), 2.8f);
            Material emberGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1.0f, 0.58f, 0.10f), 2.8f);
            Material goldGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1.0f, 0.82f, 0.24f), 2.8f);
            Material polishedBladeSteel = CitadelWorldBuilder.MakeStdMat(new Color(0.86f, 0.89f, 0.96f), 0.96f, 0.91f, cachedReaverSilverTex);
            Material darkBladeSpine = CitadelWorldBuilder.MakeStdMat(new Color(0.14f, 0.11f, 0.22f), 0.72f, 0.72f, cachedReaverBodyTex);

            Mesh finMesh = CreateGothicFinMesh();
            Mesh bevelMesh = CreateBeveledRailMesh();
            Mesh karambitBladeMesh = CreateKarambitBladeMesh();
            Mesh katanaBladeMesh = CreateKatanaBladeMesh();
            Mesh fingerRingMesh = CreateTorusRingMesh(0.0185f, 0.0046f, 20, 10);

            // -----------------------------------------------------------------
            // 1. ARTICULATED RIGHT GLOVED COMBAT HAND (Holds & grips all 4 melee weapons!)
            // -----------------------------------------------------------------
            GameObject rArmGo = new GameObject("MeleeRightArmPivot");
            rArmGo.transform.SetParent(meleeGroup, false);
            rArmGo.transform.localPosition = meleeRightHandBasePos;
            rArmGo.transform.localEulerAngles = meleeRightHandBaseEuler;
            meleeRightArmPivot = rArmGo.transform;

            // Sculpted Right Forearm + Wrist Cuff + Armored Tactical Palm
            Part(meleeRightArmPivot, "MeleeR_Forearm", PrimitiveType.Cylinder, new Vector3(0.055f, -0.105f, -0.175f), new Vector3(0.088f, 0.165f, 0.088f), gloveDarkMat).transform.localRotation = Quaternion.Euler(64f, -14f, -16f);
            Part(meleeRightArmPivot, "MeleeR_WristCuff", PrimitiveType.Cylinder, new Vector3(0.024f, -0.042f, -0.072f), new Vector3(0.072f, 0.044f, 0.072f), gloveDarkMat).transform.localRotation = Quaternion.Euler(64f, -12f, -12f);
            Part(meleeRightArmPivot, "MeleeR_CuffAccent", PrimitiveType.Cylinder, new Vector3(0.034f, -0.062f, -0.102f), new Vector3(0.074f, 0.006f, 0.074f), cuffAccentMat).transform.localRotation = Quaternion.Euler(64f, -12f, -12f);
            PartMesh(meleeRightArmPivot, "MeleeR_Palm", bevelMesh, new Vector3(0.006f, -0.004f, -0.002f), new Vector3(0.062f, 0.046f, 0.084f), new Vector3(6f, -8f, -14f), gloveDarkMat);
            PartMesh(meleeRightArmPivot, "MeleeR_KnuckleArmor", bevelMesh, new Vector3(0.018f, 0.014f, 0.006f), new Vector3(0.044f, 0.022f, 0.068f), new Vector3(6f, -8f, -18f), gloveArmorMat);

            // Individual Sculpted Gloved Fingers Curled Tightly Around Weapon Handle & Karambit Ring!
            // Index finger passes cleanly through the Karambit pommel ring!
            Part(meleeRightArmPivot, "MeleeR_IndexFinger", PrimitiveType.Capsule, new Vector3(-0.010f, 0.018f, 0.040f), new Vector3(0.016f, 0.026f, 0.016f), gloveArmorMat).transform.localRotation = Quaternion.Euler(22f, 12f, 78f);
            // Middle, Ring, and Pinky fingers tightly gripping the curved handle
            Part(meleeRightArmPivot, "MeleeR_MiddleFinger", PrimitiveType.Capsule, new Vector3(-0.016f, 0.001f, 0.026f), new Vector3(0.017f, 0.028f, 0.017f), gloveDarkMat).transform.localRotation = Quaternion.Euler(8f, 8f, 82f);
            Part(meleeRightArmPivot, "MeleeR_RingFinger", PrimitiveType.Capsule, new Vector3(-0.016f, -0.016f, 0.014f), new Vector3(0.016f, 0.026f, 0.016f), gloveDarkMat).transform.localRotation = Quaternion.Euler(2f, 6f, 84f);
            Part(meleeRightArmPivot, "MeleeR_PinkyFinger", PrimitiveType.Capsule, new Vector3(-0.012f, -0.031f, 0.004f), new Vector3(0.014f, 0.022f, 0.014f), gloveDarkMat).transform.localRotation = Quaternion.Euler(-4f, 4f, 86f);
            // Thumb locked along the bolster side of the handle
            Part(meleeRightArmPivot, "MeleeR_Thumb", PrimitiveType.Capsule, new Vector3(-0.024f, 0.014f, 0.006f), new Vector3(0.019f, 0.028f, 0.019f), gloveArmorMat).transform.localRotation = Quaternion.Euler(46f, -24f, 22f);

            // -----------------------------------------------------------------
            // 2. ARTICULATED LEFT GUARD HAND (Classic FPS tactical knife balance stance!)
            // -----------------------------------------------------------------
            GameObject lArmGo = new GameObject("MeleeLeftArmPivot");
            lArmGo.transform.SetParent(meleeGroup, false);
            lArmGo.transform.localPosition = meleeLeftHandBasePos;
            lArmGo.transform.localEulerAngles = meleeLeftHandBaseEuler;
            meleeLeftArmPivot = lArmGo.transform;
            Part(meleeLeftArmPivot, "MeleeL_Forearm", PrimitiveType.Cylinder, new Vector3(-0.055f, -0.095f, -0.155f), new Vector3(0.086f, 0.155f, 0.086f), gloveDarkMat).transform.localRotation = Quaternion.Euler(62f, 16f, 18f);
            Part(meleeLeftArmPivot, "MeleeL_CuffAccent", PrimitiveType.Cylinder, new Vector3(-0.032f, -0.055f, -0.088f), new Vector3(0.072f, 0.006f, 0.072f), cuffAccentMat).transform.localRotation = Quaternion.Euler(62f, 14f, 14f);
            PartMesh(meleeLeftArmPivot, "MeleeL_Palm", bevelMesh, Vector3.zero, new Vector3(0.060f, 0.044f, 0.082f), new Vector3(12f, 18f, 16f), gloveDarkMat);
            PartMesh(meleeLeftArmPivot, "MeleeL_Knuckles", bevelMesh, new Vector3(-0.014f, 0.012f, 0.010f), new Vector3(0.042f, 0.020f, 0.064f), new Vector3(12f, 18f, 18f), gloveArmorMat);
            Part(meleeLeftArmPivot, "MeleeL_Fingers", PrimitiveType.Cube, new Vector3(0.010f, 0.004f, 0.044f), new Vector3(0.044f, 0.022f, 0.040f), gloveArmorMat).transform.localRotation = Quaternion.Euler(26f, 12f, 10f);
            Part(meleeLeftArmPivot, "MeleeL_Thumb", PrimitiveType.Capsule, new Vector3(0.026f, 0.012f, 0.014f), new Vector3(0.018f, 0.026f, 0.018f), gloveDarkMat).transform.localRotation = Quaternion.Euler(52f, 20f, -26f);

            // -----------------------------------------------------------------
            // 3. WEAPON SOCKET ATTACHED DIRECTLY INSIDE RIGHT HAND PALM!
            // -----------------------------------------------------------------
            GameObject socketGo = new GameObject("MeleeWeaponSocket");
            socketGo.transform.SetParent(meleeRightArmPivot, false);
            socketGo.transform.localPosition = new Vector3(-0.012f, 0.018f, 0.040f); // Anchored cleanly on index finger & palm axis!
            socketGo.transform.localRotation = Quaternion.identity;
            meleeWeaponSocket = socketGo.transform;

            meleeModels = new GameObject[4];

            // =================================================================
            // MELEE 0: ULTRA-REALISTIC REAVER KARAMBIT (Curved Claw Blade + Real Finger Ring!)
            // =================================================================
            GameObject karambitRoot = new GameObject("Melee_ReaverKarambit");
            karambitRoot.transform.SetParent(meleeWeaponSocket, false);
            meleeModels[0] = karambitRoot;

            // Dedicated Spin Pivot centered right at the index finger ring!
            GameObject spinGo = new GameObject("KarambitRingPivot");
            spinGo.transform.SetParent(karambitRoot.transform, false);
            spinGo.transform.localPosition = Vector3.zero;
            spinGo.transform.localRotation = Quaternion.identity;
            karambitRingPivot = spinGo.transform;

            // Real Hollow 3D Sculpted Finger Ring encircling the gloved index finger!
            PartMesh(karambitRingPivot, "KarambitFingerRing", fingerRingMesh,
                Vector3.zero, Vector3.one, new Vector3(0f, 90f, 0f), silverFiligree);
            // Gothic spiked ring pommel crest
            PartMesh(karambitRingPivot, "KarambitRingSpike", finMesh,
                new Vector3(0f, 0.021f, -0.008f), new Vector3(0.008f, 0.016f, 0.024f),
                new Vector3(-30f, 0f, 0f), silverFiligree);
            Part(karambitRingPivot, "KarambitRingJewel", PrimitiveType.Sphere,
                new Vector3(0f, 0.019f, 0.004f), Vector3.one * 0.007f, karambitRuneMat);

            // Ergonomic Curved Ribbed Handle passing right down through the curled fingers!
            PartMesh(karambitRingPivot, "KarambitGripUpper", bevelMesh,
                new Vector3(0f, -0.036f, -0.010f), new Vector3(0.016f, 0.050f, 0.024f),
                new Vector3(14f, 0f, 0f), darkChassis);
            PartMesh(karambitRingPivot, "KarambitGripLower", bevelMesh,
                new Vector3(0f, -0.078f, -0.008f), new Vector3(0.018f, 0.048f, 0.026f),
                new Vector3(-12f, 0f, 0f), indigoPlate);
            // Forged Silver Gothic Handle Spine + Glowing Violet Channel
            PartMesh(karambitRingPivot, "KarambitGripSilver", bevelMesh,
                new Vector3(0f, -0.056f, -0.022f), new Vector3(0.010f, 0.088f, 0.009f),
                new Vector3(4f, 0f, 0f), silverFiligree);
            Part(karambitRingPivot, "KarambitGripRune", PrimitiveType.Cube,
                new Vector3(0f, -0.056f, -0.010f), new Vector3(0.019f, 0.058f, 0.007f), karambitRuneMat);

            // Forged Silver Gothic Bolster Guard connecting grip to curved talon blade!
            PartMesh(karambitRingPivot, "KarambitBolster", bevelMesh,
                new Vector3(0f, -0.104f, 0.004f), new Vector3(0.020f, 0.020f, 0.038f),
                new Vector3(-24f, 0f, 0f), silverFiligree);
            PartMesh(karambitRingPivot, "KarambitGuardFang", finMesh,
                new Vector3(0f, -0.102f, -0.018f), new Vector3(0.010f, 0.026f, 0.024f),
                new Vector3(-110f, 0f, 0f), silverFiligree);

            // Sculpted Curved Tiger-Claw Karambit Blade sweeping fiercely forward & upward in reverse grip!
            GameObject bladeAnchor = new GameObject("KarambitBladeAnchor");
            bladeAnchor.transform.SetParent(karambitRingPivot, false);
            bladeAnchor.transform.localPosition = new Vector3(0f, -0.108f, 0.012f);
            bladeAnchor.transform.localEulerAngles = new Vector3(-22f, 0f, 0f);
            // Gleaming Forged Steel Beveled Razor Blade
            PartMesh(bladeAnchor.transform, "KarambitSteelBlade", karambitBladeMesh,
                Vector3.zero, Vector3.one, Vector3.zero, polishedBladeSteel);
            // Deep Midnight-Indigo Forged Inner Spine Plate
            PartMesh(bladeAnchor.transform, "KarambitIndigoSpine", karambitBladeMesh,
                new Vector3(0f, 0.003f, -0.004f), new Vector3(1.18f, 0.88f, 0.86f), Vector3.zero, darkBladeSpine);
            // Glowing Electric-Violet Carved Arcane Fuller Vein along the curved blade!
            PartMesh(bladeAnchor.transform, "KarambitRuneVein", karambitBladeMesh,
                new Vector3(0f, 0.001f, 0.002f), new Vector3(1.28f, 0.60f, 0.78f), Vector3.zero, karambitRuneMat);
            // 3 Swept Silver Gothic Spine Thorns along outer curve of blade
            PartMesh(bladeAnchor.transform, "KarambitThorn1", finMesh, new Vector3(0f, -0.016f, 0.042f), new Vector3(0.006f, 0.016f, 0.026f), new Vector3(-160f, 0f, 0f), silverFiligree);
            PartMesh(bladeAnchor.transform, "KarambitThorn2", finMesh, new Vector3(0f, -0.012f, 0.095f), new Vector3(0.005f, 0.015f, 0.024f), new Vector3(-140f, 0f, 0f), silverFiligree);
            PartMesh(bladeAnchor.transform, "KarambitThorn3", finMesh, new Vector3(0f, 0.014f, 0.145f), new Vector3(0.004f, 0.013f, 0.022f), new Vector3(-115f, 0f, 0f), silverFiligree);

            // Drifting Soft Transparent Dark-Violet Spectral Smoke Wisps off the Karambit Blade!
            karambitMistPS = CreateWispParticleSystem(
                bladeAnchor.transform, "KarambitSpectralMist",
                new Vector3(0f, 0.01f, 0.11f),
                rateOverTime: 9.0f,
                lifeMin: 0.35f, lifeMax: 0.68f,
                speedMin: 0.06f, speedMax: 0.18f,
                sizeMin: 0.045f, sizeMax: 0.115f,
                shapeRadius: 0.06f,
                startColA: new Color(0.14f, 0.02f, 0.28f, 0.45f),
                startColB: new Color(0.62f, 0.24f, 1.0f, 0.38f),
                gravity: -0.25f
            );

            // =================================================================
            // MELEE 1: REALISTIC ONI BLOOD-KATANA (Curved Steel Blade + Ornate Tsuba!)
            // =================================================================
            GameObject katana = new GameObject("Melee_OniKatana");
            katana.transform.SetParent(meleeWeaponSocket, false);
            katana.transform.localPosition = new Vector3(0f, -0.04f, -0.01f);
            katana.transform.localEulerAngles = new Vector3(12f, 0f, 0f);
            meleeModels[1] = katana;

            // Diamond-wrapped Tsuka Hilt inside hand + Gold Pommel Kashira
            PartMesh(katana.transform, "KatanaHilt", bevelMesh, new Vector3(0f, -0.04f, 0f), new Vector3(0.022f, 0.18f, 0.030f), Vector3.zero, darkChassis);
            PartMesh(katana.transform, "KatanaPommel", bevelMesh, new Vector3(0f, -0.135f, 0f), new Vector3(0.026f, 0.020f, 0.034f), Vector3.zero, brassGold);
            // Ornate Gothic Tsuba Handguard + Brass Habaki Collar
            Part(katana.transform, "KatanaTsuba", PrimitiveType.Cylinder, new Vector3(0f, 0.056f, 0f), new Vector3(0.072f, 0.007f, 0.072f), darkChassis);
            Part(katana.transform, "KatanaTsubaRim", PrimitiveType.Cylinder, new Vector3(0f, 0.056f, 0f), new Vector3(0.076f, 0.004f, 0.076f), brassGold);
            PartMesh(katana.transform, "KatanaHabaki", bevelMesh, new Vector3(0f, 0.074f, 0f), new Vector3(0.016f, 0.026f, 0.026f), Vector3.zero, brassGold);
            // Curved Forged Steel Katana Blade + Glowing Crimson Blood-Vein Fuller!
            PartMesh(katana.transform, "KatanaSteelBlade", katanaBladeMesh, new Vector3(0f, 0.084f, 0f), Vector3.one, Vector3.zero, polishedBladeSteel);
            PartMesh(katana.transform, "KatanaBloodVein", katanaBladeMesh, new Vector3(0f, 0.088f, -0.002f), new Vector3(1.22f, 0.94f, 0.52f), Vector3.zero, crimsonGlow);

            // =================================================================
            // MELEE 2: REALISTIC RIPPER CHAINSWORD (Armored Saw-Housing + Animated Teeth!)
            // =================================================================
            GameObject chainsword = new GameObject("Melee_RipperChainsword");
            chainsword.transform.SetParent(meleeWeaponSocket, false);
            chainsword.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            chainsword.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
            meleeModels[2] = chainsword;

            PartMesh(chainsword.transform, "ChainGrip", bevelMesh, new Vector3(0f, -0.03f, 0f), new Vector3(0.028f, 0.14f, 0.036f), Vector3.zero, darkChassis);
            PartMesh(chainsword.transform, "ChainKnuckleGuard", bevelMesh, new Vector3(0f, -0.02f, 0.036f), new Vector3(0.016f, 0.14f, 0.012f), Vector3.zero, steelDark);
            PartMesh(chainsword.transform, "ChainMotorHousing", bevelMesh, new Vector3(0f, 0.095f, 0.010f), new Vector3(0.062f, 0.11f, 0.088f), Vector3.zero, steelDark);
            Part(chainsword.transform, "ChainCoreVent", PrimitiveType.Cube, new Vector3(0f, 0.095f, 0.010f), new Vector3(0.066f, 0.042f, 0.056f), emberGlow);
            PartMesh(chainsword.transform, "ChainBladeBar", bevelMesh, new Vector3(0f, 0.34f, 0.016f), new Vector3(0.022f, 0.44f, 0.064f), Vector3.zero, indigoPlate);
            PartMesh(chainsword.transform, "ChainSpineRail", bevelMesh, new Vector3(0f, 0.34f, -0.018f), new Vector3(0.032f, 0.45f, 0.022f), Vector3.zero, silverFiligree);

            GameObject teethRoot = new GameObject("ChainswordTeethRoot");
            teethRoot.transform.SetParent(chainsword.transform, false);
            teethRoot.transform.localPosition = new Vector3(0f, 0.34f, 0.052f);
            chainswordTeethRoot = teethRoot.transform;
            for (int t = 0; t < 10; t++)
            {
                float ty = -0.20f + t * 0.044f;
                PartMesh(chainswordTeethRoot, $"SawTooth_{t}", finMesh, new Vector3(0f, ty, 0f), new Vector3(0.014f, 0.022f, 0.022f), new Vector3(90f, 0f, 0f), silverFiligree);
            }

            // =================================================================
            // MELEE 3: REALISTIC HELL-BREAKER WARHAMMER (Reinforced Haft + Twin Rune Head!)
            // =================================================================
            GameObject hammer = new GameObject("Melee_HellBreakerWarhammer");
            hammer.transform.SetParent(meleeWeaponSocket, false);
            hammer.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            hammer.transform.localEulerAngles = new Vector3(14f, 0f, 0f);
            meleeModels[3] = hammer;

            Part(hammer.transform, "HammerShaft", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.028f, 0.30f, 0.028f), darkChassis);
            Part(hammer.transform, "HammerCollar", PrimitiveType.Cylinder, new Vector3(0f, 0.39f, 0f), new Vector3(0.044f, 0.035f, 0.044f), brassGold);
            PartMesh(hammer.transform, "HammerHeadMain", bevelMesh, new Vector3(0f, 0.45f, 0.01f), new Vector3(0.095f, 0.115f, 0.21f), Vector3.zero, steelDark);
            PartMesh(hammer.transform, "HammerFaceFront", bevelMesh, new Vector3(0f, 0.45f, 0.125f), new Vector3(0.105f, 0.125f, 0.030f), Vector3.zero, silverFiligree);
            PartMesh(hammer.transform, "HammerBackSpike", finMesh, new Vector3(0f, 0.45f, -0.115f), new Vector3(0.048f, 0.095f, 0.095f), new Vector3(-90f, 0f, 0f), silverFiligree);
            Part(hammer.transform, "HammerRuneCore", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.01f), new Vector3(0.102f, 0.036f, 0.12f), goldGlow);

            // -----------------------------------------------------------------
            // 4. GLOWING CURVED SLASH ARC TRAIL (Flashes dynamically during attacks!)
            // -----------------------------------------------------------------
            Shader spriteSh = Shader.Find("Sprites/Default");
            if (spriteSh == null) spriteSh = Shader.Find("Unlit/Transparent");
            meleeSlashArcMat = new Material(spriteSh);
            meleeSlashArcMat.mainTexture = CreateSlashArcTexture();
            meleeSlashArcMat.color = new Color(0.76f, 0.38f, 1.0f, 0f);
            meleeSlashArcObj = Part(meleeGroup, "MeleeSlashArc", PrimitiveType.Quad, new Vector3(0f, 0.05f, 0.14f), new Vector3(0.72f, 0.42f, 1f), meleeSlashArcMat);
            meleeSlashArcObj.SetActive(false);

            UpdateActiveMeleeMesh();
            meleeGroup.gameObject.SetActive(isMeleeEquipped);
        }

        private void UpdateActiveMeleeMesh()
        {
            if (meleeModels == null) return;
            for (int i = 0; i < meleeModels.Length; i++)
            {
                if (meleeModels[i] != null) meleeModels[i].SetActive(i == activeMeleeId);
            }
        }

        // =========================================================================
        // ULTRA-DETAILED SCULPTED 3D GOTHIC REAVER VANDAL (Matches Video 0:47-1:12!)
        // =========================================================================
        private void BuildReaverVandalModel(
            Material indigoPlate, Material darkChassis, Material silverFiligree,
            Material steelDark, Material runeMat, Material crestMat,
            Material coreGlow, Material sightDotMat)
        {
            GameObject vandal = new GameObject("Model_ReaverVandal");
            vandal.transform.SetParent(gunPivot, false);
            weaponModels[0] = vandal;

            Mesh finMesh = CreateGothicFinMesh();
            Mesh bevelRailMesh = CreateBeveledRailMesh();

            // 1. LAYERED FORGED RECEIVER & REAR STOCK CREST (Deep Indigo-Purple + Sculpted Silver Edges)
            Part(vandal.transform, "ReceiverCore", PrimitiveType.Cube, new Vector3(0f, -0.002f, 0.015f), new Vector3(0.056f, 0.078f, 0.39f), darkChassis);
            // Faceted Indigo Armor Side Plates
            PartMesh(vandal.transform, "ReceiverPlateL", bevelRailMesh, new Vector3(-0.029f, 0.004f, 0.01f), new Vector3(0.014f, 0.072f, 0.37f), Vector3.zero, indigoPlate);
            PartMesh(vandal.transform, "ReceiverPlateR", bevelRailMesh, new Vector3(0.029f, 0.004f, 0.01f), new Vector3(0.014f, 0.072f, 0.37f), Vector3.zero, indigoPlate);
            // Sloped Dust Cover Spine (Indigo Core + Gleaming Silver Ribbing)
            PartMesh(vandal.transform, "DustCover", bevelRailMesh, new Vector3(0f, 0.044f, -0.015f), new Vector3(0.054f, 0.026f, 0.35f), Vector3.zero, indigoPlate);
            PartMesh(vandal.transform, "SilverSpineRail", bevelRailMesh, new Vector3(0f, 0.058f, 0.01f), new Vector3(0.018f, 0.012f, 0.38f), Vector3.zero, silverFiligree);

            // Sweeping Forged Silver Side Filigree Wings (Signature bright metallic contour along both sides!)
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side * 0.0365f;
                // Upper sweeping blade rail
                PartMesh(vandal.transform, $"UpperFiligree_{side}", bevelRailMesh,
                    new Vector3(sx, 0.024f, 0.02f), new Vector3(0.009f, 0.015f, 0.42f),
                    new Vector3(-3.2f, 0f, side * 10f), silverFiligree);
                // Lower flared wing rail angling sharply forward around magwell
                PartMesh(vandal.transform, $"LowerFiligree_{side}", bevelRailMesh,
                    new Vector3(sx * 1.02f, -0.026f, 0.045f), new Vector3(0.010f, 0.016f, 0.36f),
                    new Vector3(4.5f, 0f, -side * 12f), silverFiligree);
                // Razor-pointed silver cheek-wing flare above magwell
                PartMesh(vandal.transform, $"MagwellFlare_{side}", finMesh,
                    new Vector3(sx * 1.08f, -0.034f, 0.135f), new Vector3(0.012f, 0.040f, 0.095f),
                    new Vector3(-162f, side * 8f, 0f), silverFiligree);
                // Recessed glowing violet energy vein running beneath the upper silver rail
                Part(vandal.transform, $"ReceiverVein_{side}", PrimitiveType.Cube,
                    new Vector3(side * 0.033f, 0.011f, 0.03f), new Vector3(0.005f, 0.009f, 0.28f), coreGlow);
            }

            // Buttstock & Iconic Carved Glowing Violet Medallion Crest (Clearly visible on rear left in 1st person!)
            PartMesh(vandal.transform, "StockBody", bevelRailMesh, new Vector3(0f, -0.014f, -0.255f), new Vector3(0.052f, 0.072f, 0.19f), new Vector3(5f, 0f, 0f), indigoPlate);
            PartMesh(vandal.transform, "StockTrimTop", bevelRailMesh, new Vector3(0f, 0.024f, -0.245f), new Vector3(0.034f, 0.014f, 0.19f), new Vector3(7f, 0f, 0f), silverFiligree);
            PartMesh(vandal.transform, "StockTrimBot", bevelRailMesh, new Vector3(0f, -0.050f, -0.255f), new Vector3(0.032f, 0.014f, 0.18f), new Vector3(3f, 0f, 0f), silverFiligree);
            // Glowing Reaver Circular Sigil Medallion on Left & Right Rear Receiver/Stock!
            Part(vandal.transform, "ReaverCrestL", PrimitiveType.Quad, new Vector3(-0.0315f, -0.002f, -0.155f), new Vector3(0.074f, 0.056f, 1f), crestMat).transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Part(vandal.transform, "ReaverCrestR", PrimitiveType.Quad, new Vector3(0.0315f, -0.002f, -0.155f), new Vector3(0.074f, 0.056f, 1f), crestMat).transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            // Silver gothic arch collar framing the glowing crest
            PartMesh(vandal.transform, "CrestArcL", finMesh, new Vector3(-0.035f, 0.022f, -0.145f), new Vector3(0.010f, 0.028f, 0.088f), new Vector3(12f, 0f, 16f), silverFiligree);
            PartMesh(vandal.transform, "CrestArcR", finMesh, new Vector3(0.035f, 0.022f, -0.145f), new Vector3(0.010f, 0.028f, 0.088f), new Vector3(12f, 0f, -16f), silverFiligree);

            // 2. SCULPTED GOTHIC HANDGUARD WITH GLOWING SWIRLING VIOLET RUNE CUTOUTS
            PartMesh(vandal.transform, "HandguardCore", bevelRailMesh, new Vector3(0f, -0.004f, 0.305f), new Vector3(0.056f, 0.064f, 0.245f), Vector3.zero, indigoPlate);
            // Glowing Carved Swirling Violet Rune Panels on BOTH sides of the handguard (signature glowing scroll inlays!)
            Part(vandal.transform, "HandguardRunesL", PrimitiveType.Quad, new Vector3(-0.0298f, -0.002f, 0.305f), new Vector3(0.215f, 0.044f, 1f), runeMat).transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Part(vandal.transform, "HandguardRunesR", PrimitiveType.Quad, new Vector3(0.0298f, -0.002f, 0.305f), new Vector3(0.215f, 0.044f, 1f), runeMat).transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            // Silver Gothic Ribbing Framing the Handguard Rune Cutouts
            for (int side = -1; side <= 1; side += 2)
            {
                PartMesh(vandal.transform, $"HgTopRail_{side}", bevelRailMesh, new Vector3(side * 0.030f, 0.027f, 0.305f), new Vector3(0.010f, 0.011f, 0.25f), Vector3.zero, silverFiligree);
                PartMesh(vandal.transform, $"HgBotRail_{side}", bevelRailMesh, new Vector3(side * 0.028f, -0.033f, 0.305f), new Vector3(0.010f, 0.012f, 0.24f), Vector3.zero, silverFiligree);
            }
            // Angled Silver Handguard Clamp Collar + 3 Forged Metallic Studs (matches HD reference!)
            PartMesh(vandal.transform, "FrontCollar", bevelRailMesh, new Vector3(0f, 0.002f, 0.422f), new Vector3(0.064f, 0.072f, 0.046f), Vector3.zero, silverFiligree);
            Part(vandal.transform, "CollarVioletRing", PrimitiveType.Cylinder, new Vector3(0f, -0.008f, 0.402f), new Vector3(0.062f, 0.009f, 0.062f), coreGlow).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int st = 0; st < 3; st++)
            {
                float sz = 0.408f + st * 0.013f;
                Part(vandal.transform, $"CollarStudL_{st}", PrimitiveType.Sphere, new Vector3(-0.033f, 0.018f - st * 0.004f, sz), Vector3.one * 0.007f, silverFiligree);
                Part(vandal.transform, $"CollarStudR_{st}", PrimitiveType.Sphere, new Vector3(0.033f, 0.018f - st * 0.004f, sz), Vector3.one * 0.007f, silverFiligree);
            }
            // Iconic Downward-Swept Silver Gothic Fangs Under Front Handguard Collar!
            PartMesh(vandal.transform, "UnderCollarFangL", finMesh, new Vector3(-0.022f, -0.046f, 0.426f), new Vector3(0.012f, 0.048f, 0.056f), new Vector3(158f, 0f, -12f), silverFiligree);
            PartMesh(vandal.transform, "UnderCollarFangR", finMesh, new Vector3(0.022f, -0.046f, 0.426f), new Vector3(0.012f, 0.048f, 0.056f), new Vector3(158f, 0f, 12f), silverFiligree);

            // 3. SIX GRADUATED SWEPT-BACK SILVER GOTHIC DORSAL THORNS ALONG THE TOP SPINE!
            float[] thornZ = { 0.07f, 0.145f, 0.215f, 0.285f, 0.352f, 0.415f };
            float[] thornH = { 0.017f, 0.020f, 0.023f, 0.025f, 0.022f, 0.020f };
            for (int i = 0; i < thornZ.Length; i++)
            {
                PartMesh(vandal.transform, $"DorsalThornL_{i}", finMesh,
                    new Vector3(-0.023f, 0.046f, thornZ[i]),
                    new Vector3(0.008f, thornH[i], 0.044f),
                    new Vector3(-22f, 0f, 22f), silverFiligree);
                PartMesh(vandal.transform, $"DorsalThornR_{i}", finMesh,
                    new Vector3(0.023f, 0.046f, thornZ[i]),
                    new Vector3(0.008f, thornH[i], 0.044f),
                    new Vector3(-22f, 0f, -22f), silverFiligree);
            }

            // 4. EXPOSED GAS TUBE, FLUTED DARK-STEEL BARREL & 4-CLAW REAVER MUZZLE CROWN!
            Part(vandal.transform, "GasTube", PrimitiveType.Cylinder, new Vector3(0f, 0.040f, 0.395f), new Vector3(0.019f, 0.135f, 0.019f), silverFiligree).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(vandal.transform, "GasBlockSpire", PrimitiveType.Cube, new Vector3(0f, 0.028f, 0.515f), new Vector3(0.028f, 0.046f, 0.036f), silverFiligree);
            PartMesh(vandal.transform, "GasBlockFang", finMesh, new Vector3(0f, -0.014f, 0.520f), new Vector3(0.014f, 0.042f, 0.048f), new Vector3(165f, 0f, 0f), silverFiligree);

            Part(vandal.transform, "FlutedBarrel", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0.555f), new Vector3(0.023f, 0.175f, 0.023f), steelDark).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(vandal.transform, "BarrelCollarMid", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0.612f), new Vector3(0.031f, 0.016f, 0.031f), silverFiligree).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Signature Reaver 4-Pronged Spiked Gothic Flash-Hider / Muzzle Crown!
            Part(vandal.transform, "MuzzleCrownBase", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0.688f), new Vector3(0.036f, 0.040f, 0.036f), silverFiligree).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(vandal.transform, "MuzzleInnerBore", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0.720f), new Vector3(0.020f, 0.014f, 0.020f), coreGlow).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int c = 0; c < 4; c++)
            {
                float ang = c * 90f + 45f;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 prongOff = new Vector3(Mathf.Cos(rad) * 0.017f, 0.008f + Mathf.Sin(rad) * 0.017f, 0.725f);
                PartMesh(vandal.transform, $"MuzzleClaw_{c}", finMesh, prongOff, new Vector3(0.009f, 0.022f, 0.068f), new Vector3(88f, 0f, ang - 90f), silverFiligree);
            }

            // 5. CARVED GOTHIC ARCH ADS REFLEX SIGHT + FLOATING VIOLET DIAMOND RETICLE (Matches 1:00 ADS!)
            PartMesh(vandal.transform, "SightBase", bevelRailMesh, new Vector3(0f, 0.064f, -0.025f), new Vector3(0.044f, 0.012f, 0.088f), Vector3.zero, darkChassis);
            PartMesh(vandal.transform, "SightWingL", finMesh, new Vector3(-0.022f, 0.095f, -0.025f), new Vector3(0.008f, 0.048f, 0.068f), new Vector3(-8f, 0f, -14f), silverFiligree);
            PartMesh(vandal.transform, "SightWingR", finMesh, new Vector3(0.022f, 0.095f, -0.025f), new Vector3(0.008f, 0.048f, 0.068f), new Vector3(-8f, 0f, 14f), silverFiligree);
            Part(vandal.transform, "SightArchL", PrimitiveType.Cube, new Vector3(-0.013f, 0.116f, -0.025f), new Vector3(0.018f, 0.005f, 0.046f), silverFiligree).transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            Part(vandal.transform, "SightArchR", PrimitiveType.Cube, new Vector3(0.013f, 0.116f, -0.025f), new Vector3(0.018f, 0.005f, 0.046f), silverFiligree).transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            // Floating glowing electric-violet rune diamond reticle (super crisp in ADS!)
            GameObject holoDiamond = Part(vandal.transform, "ReaverHoloReticle", PrimitiveType.Cube, new Vector3(0f, 0.096f, -0.025f), new Vector3(0.0062f, 0.0062f, 0.002f), sightDotMat);
            holoDiamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // 6. SCULPTED HOOKED SILVER CHARGING BOLT (Right side, snaps rapidly on every shot!)
            GameObject boltRoot = new GameObject("VandalBolt");
            boltRoot.transform.SetParent(vandal.transform, false);
            boltRoot.transform.localPosition = new Vector3(0.036f, 0.020f, 0.042f);
            PartMesh(boltRoot.transform, "BoltCarrier", bevelRailMesh, Vector3.zero, new Vector3(0.016f, 0.016f, 0.088f), Vector3.zero, silverFiligree);
            PartMesh(boltRoot.transform, "BoltHook", finMesh, new Vector3(0.015f, 0.002f, -0.018f), new Vector3(0.024f, 0.014f, 0.026f), new Vector3(0f, 0f, -80f), silverFiligree);
            weaponBolts[0] = boltRoot.transform;

            // 7. CURVED SILVER-RIBBED REAVER BANANA MAGAZINE WITH GLOWING VIOLET ARCANE WINDOWS!
            GameObject vMagRoot = new GameObject("VandalBananaMag");
            vMagRoot.transform.SetParent(vandal.transform, false);
            vMagRoot.transform.localPosition = vandalMagBasePos;
            // Upper & lower curved midnight-indigo mag body
            PartMesh(vMagRoot.transform, "MagUpperBody", bevelRailMesh, new Vector3(0f, 0.002f, 0f), new Vector3(0.038f, 0.105f, 0.062f), new Vector3(-11f, 0f, 0f), indigoPlate);
            PartMesh(vMagRoot.transform, "MagLowerBody", bevelRailMesh, new Vector3(0f, -0.088f, 0.032f), new Vector3(0.036f, 0.105f, 0.060f), new Vector3(-28f, 0f, 0f), indigoPlate);
            // Gleaming silver gothic front & rear ribs along the curved banana spine!
            PartMesh(vMagRoot.transform, "MagSilverFrontU", bevelRailMesh, new Vector3(0f, 0.002f, 0.031f), new Vector3(0.022f, 0.108f, 0.012f), new Vector3(-11f, 0f, 0f), silverFiligree);
            PartMesh(vMagRoot.transform, "MagSilverFrontL", bevelRailMesh, new Vector3(0f, -0.088f, 0.062f), new Vector3(0.020f, 0.108f, 0.012f), new Vector3(-28f, 0f, 0f), silverFiligree);
            PartMesh(vMagRoot.transform, "MagBaseSpike", finMesh, new Vector3(0f, -0.145f, 0.066f), new Vector3(0.036f, 0.026f, 0.068f), new Vector3(-118f, 0f, 0f), silverFiligree);
            // Glowing Electric-Violet Mag Windows (Visible on inspect & telekinetic floating reload!)
            Part(vMagRoot.transform, "MagGlowWindowU", PrimitiveType.Cube, new Vector3(0f, -0.008f, 0.002f), new Vector3(0.041f, 0.072f, 0.026f), coreGlow).transform.localRotation = Quaternion.Euler(-11f, 0f, 0f);
            Part(vMagRoot.transform, "MagGlowWindowL", PrimitiveType.Cube, new Vector3(0f, -0.085f, 0.031f), new Vector3(0.039f, 0.072f, 0.024f), coreGlow).transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            weaponMags[0] = vMagRoot.transform;

            // 8. TEXTURED ERGONOMIC PISTOL GRIP & FORGED SILVER TRIGGER GUARD
            PartMesh(vandal.transform, "PistolGrip", bevelRailMesh, new Vector3(0f, -0.086f, -0.118f), new Vector3(0.040f, 0.115f, 0.050f), new Vector3(18f, 0f, 0f), darkChassis);
            PartMesh(vandal.transform, "GripSilverSpine", bevelRailMesh, new Vector3(0f, -0.084f, -0.142f), new Vector3(0.018f, 0.102f, 0.010f), new Vector3(18f, 0f, 0f), silverFiligree);
            Part(vandal.transform, "TriggerGuard", PrimitiveType.Cube, new Vector3(0f, -0.056f, -0.045f), new Vector3(0.012f, 0.008f, 0.076f), silverFiligree);
            Part(vandal.transform, "Trigger", PrimitiveType.Cube, new Vector3(0f, -0.044f, -0.055f), new Vector3(0.006f, 0.024f, 0.008f), silverFiligree).transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            // 9. MUZZLE POINT & 3-PLANE SPECTRAL VIOLET-BLACK GOTHIC STAR MUZZLE FLASH!
            muzzlePoints[0] = MakeEmpty(vandal.transform, "Muzzle0", new Vector3(0f, 0.008f, 0.755f));

            GameObject flashCluster = new GameObject("ReaverSpectralMuzzleFlash");
            flashCluster.transform.SetParent(vandal.transform, false);
            flashCluster.transform.localPosition = muzzlePoints[0].localPosition + new Vector3(0f, 0f, 0.06f);
            reaverMuzzleFlash = flashCluster.transform;

            Shader spriteSh = Shader.Find("Sprites/Default");
            if (spriteSh == null) spriteSh = Shader.Find("Unlit/Transparent");
            reaverMuzzleFlashMat = new Material(spriteSh);
            reaverMuzzleFlashMat.mainTexture = cachedMuzzleStarTex;
            reaverMuzzleFlashMat.color = new Color(1f, 0.92f, 1f, 0.98f);

            // Front facing gothic star burst + 2 crossed longitudinal flame streaks (visible from both hip & ADS!)
            Part(reaverMuzzleFlash, "FlashStarFront", PrimitiveType.Quad, Vector3.zero, new Vector3(0.24f, 0.24f, 1f), reaverMuzzleFlashMat);
            Part(reaverMuzzleFlash, "FlashStreakH", PrimitiveType.Quad, new Vector3(0f, 0f, 0.07f), new Vector3(0.28f, 0.15f, 1f), reaverMuzzleFlashMat).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(reaverMuzzleFlash, "FlashStreakV", PrimitiveType.Quad, new Vector3(0f, 0f, 0.07f), new Vector3(0.28f, 0.15f, 1f), reaverMuzzleFlashMat).transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            reaverMuzzleFlash.gameObject.SetActive(false);

            // 10. SOFT TRANSPARENT DARK-VIOLET SHADOW SMOKE & SPECTRAL WISP PARTICLE SYSTEMS (Zero Squares!)
            reaverAmbientMistPS = CreateWispParticleSystem(
                vandal.transform, "ReaverAmbientShadowMist",
                new Vector3(0f, 0.015f, 0.24f),
                rateOverTime: 8.5f,
                lifeMin: 0.48f, lifeMax: 0.85f,
                speedMin: 0.08f, speedMax: 0.22f,
                sizeMin: 0.055f, sizeMax: 0.135f,
                shapeRadius: 0.14f,
                startColA: new Color(0.16f, 0.04f, 0.30f, 0.42f),
                startColB: new Color(0.56f, 0.20f, 0.96f, 0.34f),
                gravity: -0.22f
            );

            reaverMuzzleSmokePS = CreateWispParticleSystem(
                vandal.transform, "ReaverMuzzleSpectralSmoke",
                muzzlePoints[0].localPosition + new Vector3(0f, 0f, 0.04f),
                rateOverTime: 0f, // Burst emitted on every gunshot!
                lifeMin: 0.28f, lifeMax: 0.56f,
                speedMin: 0.45f, speedMax: 1.35f,
                sizeMin: 0.08f, sizeMax: 0.22f,
                shapeRadius: 0.025f,
                startColA: new Color(0.10f, 0.02f, 0.20f, 0.65f),
                startColB: new Color(0.68f, 0.30f, 1.0f, 0.58f),
                gravity: -0.45f
            );

            reaverReloadAuraPS = CreateWispParticleSystem(
                vandal.transform, "ReaverTelekineticAura",
                vandalMagBasePos + new Vector3(0f, -0.02f, 0f),
                rateOverTime: 0f, // Active during telekinetic reload!
                lifeMin: 0.30f, lifeMax: 0.60f,
                speedMin: 0.15f, speedMax: 0.48f,
                sizeMin: 0.07f, sizeMax: 0.16f,
                shapeRadius: 0.07f,
                startColA: new Color(0.14f, 0.02f, 0.28f, 0.60f),
                startColB: new Color(0.75f, 0.38f, 1.0f, 0.56f),
                gravity: -0.35f
            );
        }

        private ParticleSystem CreateWispParticleSystem(
            Transform parent, string psName, Vector3 localPos,
            float rateOverTime, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax,
            float shapeRadius, Color startColA, Color startColB, float gravity)
        {
            GameObject go = new GameObject(psName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(startColA, startColB);
            main.gravityModifier = gravity;
            main.maxParticles = 48;

            var em = ps.emission;
            em.rateOverTime = rateOverTime;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;

            var colLife = ps.colorOverLifetime;
            colLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.76f, 0.42f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.28f, 0.08f, 0.50f), 0.45f),
                    new GradientColorKey(new Color(0.06f, 0.01f, 0.12f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.16f),
                    new GradientAlphaKey(0.42f, 0.62f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colLife.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 0.45f);
            sc.AddKey(0.35f, 1.0f);
            sc.AddKey(1f, 1.55f);
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

            var rotLife = ps.rotationOverLifetime;
            rotLife.enabled = true;
            rotLife.z = new ParticleSystem.MinMaxCurve(-1.4f, 1.4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            Material wispMat = new Material(sh);
            wispMat.mainTexture = cachedWispTex;
            wispMat.color = Color.white;
            renderer.sharedMaterial = wispMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            if (Application.isPlaying) ps.Play();
            return ps;
        }

        private GameObject Part(Transform parent, string n, PrimitiveType pt, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(pt);
            go.name = n;
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroySafe(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private GameObject PartMesh(Transform parent, string n, Mesh mesh, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        private Transform MakeEmpty(Transform parent, string n, Vector3 localPos)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        // =========================================================================
        // PROCEDURAL SCULPTED MESHES (Gothic Razor Fins & Beveled Forged Armor Rails)
        // =========================================================================
        private static Mesh CreateGothicFinMesh()
        {
            Mesh m = new Mesh { name = "ReaverGothicFin" };
            // Sleek swept-back diamond thorn / claw blade
            Vector3[] verts =
            {
                new Vector3(0f,    0f,   0.50f),  // 0: Front base
                new Vector3(-0.5f, 0f,  -0.12f),  // 1: Left shoulder
                new Vector3(0.5f,  0f,  -0.12f),  // 2: Right shoulder
                new Vector3(0f,    0f,  -0.50f),  // 3: Rear base
                new Vector3(0f,    1.0f,-0.34f),  // 4: Razor swept-back apex!
                new Vector3(0f,    0.42f, 0.16f)  // 5: Curved dorsal spine ridge
            };
            int[] tris =
            {
                0, 5, 1,   0, 2, 5,
                5, 4, 1,   5, 2, 4,
                1, 4, 3,   4, 2, 3,
                0, 1, 3,   0, 3, 2
            };
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh CreateBeveledRailMesh()
        {
            Mesh m = new Mesh { name = "ReaverBeveledRail" };
            // Chamfered 8-sided armor bar (replaces boxy cubes with realistic beveled machined edges!)
            float w = 0.5f, h = 0.5f, l = 0.5f, c = 0.34f;
            Vector3[] ring =
            {
                new Vector3(-c,  h, 0), new Vector3( c,  h, 0),
                new Vector3( w,  c, 0), new Vector3( w, -c, 0),
                new Vector3( c, -h, 0), new Vector3(-c, -h, 0),
                new Vector3(-w, -c, 0), new Vector3(-w,  c, 0)
            };
            Vector3[] verts = new Vector3[18];
            Vector2[] uvs = new Vector2[18];
            for (int i = 0; i < 8; i++)
            {
                verts[i] = new Vector3(ring[i].x, ring[i].y, -l);
                verts[i + 8] = new Vector3(ring[i].x, ring[i].y, l);
                uvs[i] = new Vector2(i / 8f, 0f);
                uvs[i + 8] = new Vector2(i / 8f, 1f);
            }
            verts[16] = new Vector3(0, 0, -l); uvs[16] = new Vector2(0.5f, 0.5f);
            verts[17] = new Vector3(0, 0,  l); uvs[17] = new Vector2(0.5f, 0.5f);

            int[] tris = new int[8 * 12];
            int t = 0;
            for (int i = 0; i < 8; i++)
            {
                int n = (i + 1) % 8;
                // Side quad
                tris[t++] = i;     tris[t++] = i + 8; tris[t++] = n + 8;
                tris[t++] = i;     tris[t++] = n + 8; tris[t++] = n;
                // Back cap (-z)
                tris[t++] = 16;    tris[t++] = i;     tris[t++] = n;
                // Front cap (+z)
                tris[t++] = 17;    tris[t++] = n + 8; tris[t++] = i + 8;
            }
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh CreateTorusRingMesh(float majorR, float minorR, int majorSegs, int minorSegs)
        {
            Mesh m = new Mesh { name = "KarambitFingerRing" };
            Vector3[] verts = new Vector3[majorSegs * minorSegs];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[majorSegs * minorSegs * 6];

            for (int i = 0; i < majorSegs; i++)
            {
                float u = (i / (float)majorSegs) * Mathf.PI * 2f;
                float cu = Mathf.Cos(u), su = Mathf.Sin(u);
                Vector3 center = new Vector3(majorR * cu, majorR * su, 0f);
                Vector3 rad = new Vector3(cu, su, 0f);
                for (int j = 0; j < minorSegs; j++)
                {
                    float v = (j / (float)minorSegs) * Mathf.PI * 2f;
                    int idx = i * minorSegs + j;
                    verts[idx] = center + minorR * (Mathf.Cos(v) * rad + Mathf.Sin(v) * Vector3.forward);
                    uvs[idx] = new Vector2(i / (float)majorSegs, j / (float)minorSegs);
                }
            }

            int t = 0;
            for (int i = 0; i < majorSegs; i++)
            {
                int iN = (i + 1) % majorSegs;
                for (int j = 0; j < minorSegs; j++)
                {
                    int jN = (j + 1) % minorSegs;
                    int a = i * minorSegs + j;
                    int c = iN * minorSegs + j;
                    int d = iN * minorSegs + jN;
                    int b = i * minorSegs + jN;
                    tris[t++] = a; tris[t++] = c; tris[t++] = d;
                    tris[t++] = a; tris[t++] = d; tris[t++] = b;
                }
            }
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh CreateKarambitBladeMesh()
        {
            Mesh m = new Mesh { name = "ReaverKarambitBlade" };
            int segs = 14;
            Vector3[] verts = new Vector3[(segs + 1) * 4 + 1];
            Vector2[] uvs = new Vector2[verts.Length];
            int tipIdx = (segs + 1) * 4;

            // Curved tiger-claw Karambit sweep in YZ plane (sweeps forward +Z and hooks sharply up +Y!)
            for (int i = 0; i <= segs; i++)
            {
                float u = i / (float)segs;
                float ang = Mathf.Lerp(-0.28f, 1.28f, u);
                float radius = 0.145f;
                Vector3 center = new Vector3(0f, (1f - Mathf.Cos(ang)) * radius * 0.92f - 0.015f, Mathf.Sin(ang) * radius * 1.32f);

                // Tangent along curve and outward normal pointing toward the sharp outer spine
                Vector3 tang = new Vector3(0f, Mathf.Sin(ang) * 0.92f, Mathf.Cos(ang) * 1.32f).normalized;
                Vector3 norm = new Vector3(0f, tang.z, -tang.y).normalized;

                // Blade widens smoothly through belly then tapers to a razor claw point
                float widthEnv = Mathf.Sin(Mathf.Clamp01(u * 0.92f + 0.08f) * Mathf.PI * 0.88f) * (1f - u * 0.72f);
                float halfW = Mathf.Max(0.002f, 0.025f * (0.45f + widthEnv * 1.05f) * (1f - u * 0.82f));
                float thick = Mathf.Max(0.0012f, 0.0055f * (1f - u * 0.78f));

                int bIdx = i * 4;
                verts[bIdx + 0] = center + norm * (halfW * 0.85f);          // Outer back spine
                verts[bIdx + 1] = center + new Vector3(thick, 0f, 0f);      // Right bevel ridge
                verts[bIdx + 2] = center - norm * (halfW * 1.25f);          // Inner razor hawk-bill edge!
                verts[bIdx + 3] = center + new Vector3(-thick, 0f, 0f);     // Left bevel ridge

                for (int j = 0; j < 4; j++) uvs[bIdx + j] = new Vector2(j / 4f, u);
            }

            // Needle-sharp hooked talon tip!
            verts[tipIdx] = new Vector3(0f, 0.118f, 0.186f);
            uvs[tipIdx] = new Vector2(0.5f, 1f);

            int[] tris = new int[segs * 4 * 6 + 4 * 3 + 2 * 3];
            int t = 0;
            for (int i = 0; i < segs; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int jN = (j + 1) % 4;
                    int a = i * 4 + j;
                    int c = (i + 1) * 4 + j;
                    int d = (i + 1) * 4 + jN;
                    int b = i * 4 + jN;
                    tris[t++] = a; tris[t++] = c; tris[t++] = d;
                    tris[t++] = a; tris[t++] = d; tris[t++] = b;
                }
            }
            // Tip pyramid cap
            int lastRing = segs * 4;
            for (int j = 0; j < 4; j++)
            {
                int jN = (j + 1) % 4;
                tris[t++] = lastRing + j;
                tris[t++] = tipIdx;
                tris[t++] = lastRing + jN;
            }
            // Base cap
            tris[t++] = 0; tris[t++] = 1; tris[t++] = 2;
            tris[t++] = 0; tris[t++] = 2; tris[t++] = 3;

            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh CreateKatanaBladeMesh()
        {
            Mesh m = new Mesh { name = "OniKatanaBlade" };
            int segs = 12;
            Vector3[] verts = new Vector3[(segs + 1) * 4 + 1];
            Vector2[] uvs = new Vector2[verts.Length];
            int tipIdx = (segs + 1) * 4;

            for (int i = 0; i <= segs; i++)
            {
                float u = i / (float)segs;
                float y = u * 0.56f;
                float soriZ = u * u * 0.048f; // Graceful katana upward curve
                float halfW = Mathf.Lerp(0.016f, 0.010f, u);
                float thick = Mathf.Lerp(0.0048f, 0.0022f, u);

                int bIdx = i * 4;
                verts[bIdx + 0] = new Vector3(0f, y, soriZ + halfW);    // Sharp front cutting edge (+Z)
                verts[bIdx + 1] = new Vector3(thick, y, soriZ);         // Right shinogi ridge
                verts[bIdx + 2] = new Vector3(0f, y, soriZ - halfW);    // Rear mune spine (-Z)
                verts[bIdx + 3] = new Vector3(-thick, y, soriZ);        // Left shinogi ridge
                for (int j = 0; j < 4; j++) uvs[bIdx + j] = new Vector2(j / 4f, u);
            }
            verts[tipIdx] = new Vector3(0f, 0.61f, 0.062f);
            uvs[tipIdx] = new Vector2(0.5f, 1f);

            int[] tris = new int[segs * 4 * 6 + 4 * 3];
            int t = 0;
            for (int i = 0; i < segs; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int jN = (j + 1) % 4;
                    int a = i * 4 + j;
                    int c = (i + 1) * 4 + j;
                    int d = (i + 1) * 4 + jN;
                    int b = i * 4 + jN;
                    tris[t++] = a; tris[t++] = d; tris[t++] = c;
                    tris[t++] = a; tris[t++] = b; tris[t++] = d;
                }
            }
            int lastRing = segs * 4;
            for (int j = 0; j < 4; j++)
            {
                int jN = (j + 1) % 4;
                tris[t++] = lastRing + j;
                tris[t++] = lastRing + jN;
                tris[t++] = tipIdx;
            }
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Texture2D CreateSlashArcTexture()
        {
            int s = 256;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    // Sleek crescent blade slash smear!
                    float r = Mathf.Sqrt(nx * nx + (ny + 0.45f) * (ny + 0.45f));
                    float band = Mathf.Exp(-Mathf.Abs(r - 0.82f) * 14f);
                    float arcFade = Mathf.Clamp01(1f - Mathf.Abs(nx) * 1.08f) * (ny > -0.35f ? 1f : 0f);
                    float core = Mathf.Exp(-Mathf.Abs(r - 0.84f) * 32f) * arcFade;
                    float alpha = Mathf.Clamp01((band * 0.85f + core * 0.9f) * arcFade);
                    if (Mathf.Abs(nx) > 0.96f || Mathf.Abs(ny) > 0.96f) alpha = 0f;
                    Color c = Color.Lerp(new Color(0.55f, 0.18f, 1.0f, alpha), new Color(1f, 0.95f, 1f, alpha), Mathf.Clamp01(core * 1.2f));
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // PROCEDURAL HIGH-RES PBR, RUNE, STAR FLASH & SOFT WISP TEXTURES
        // =========================================================================
        private static void EnsureProceduralTextures()
        {
            if (cachedReaverBodyTex == null) cachedReaverBodyTex = CreateReaverBodyTexture();
            if (cachedReaverSilverTex == null) cachedReaverSilverTex = CreateReaverSilverTexture();
            if (cachedReaverRuneTex == null) cachedReaverRuneTex = CreateReaverRuneTexture();
            if (cachedReaverCrestTex == null) cachedReaverCrestTex = CreateReaverCrestTexture();
            if (cachedGripTex == null) cachedGripTex = CreateGripTexture();
            if (cachedMuzzleStarTex == null) cachedMuzzleStarTex = CreateReaverMuzzleStarTexture();
            if (cachedWispTex == null) cachedWispTex = CreateSoftWispTexture();
        }

        private static Material MakeEmissiveTexturedMat(Texture2D tex, Color emissionTint, float intensity)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Texture");
            Material m = new Material(sh);
            m.color = Color.white;
            m.mainTexture = tex;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.35f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.72f);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emissionTint * intensity);
            }
            if (m.HasProperty("_EmissionMap"))
            {
                m.SetTexture("_EmissionMap", tex);
            }
            return m;
        }

        private static Texture2D CreateReaverBodyTexture()
        {
            int s = 256;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
            System.Random rng = new System.Random(409);
            for (int y = 0; y < s; y++)
            {
                float rowGrain = (float)(rng.NextDouble() * 0.032 - 0.016);
                for (int x = 0; x < s; x++)
                {
                    float fine = (float)(rng.NextDouble() * 0.022 - 0.011);
                    float diag = Mathf.Sin((x * 0.07f) + (y * 0.04f)) * 0.025f;
                    // Deep royal midnight-purple / dark plum obsidian forged armor
                    float r = Mathf.Clamp01(0.165f + rowGrain + fine + diag);
                    float g = Mathf.Clamp01(0.115f + rowGrain * 0.7f + fine + diag * 0.6f);
                    float b = Mathf.Clamp01(0.255f + rowGrain * 1.1f + fine + diag * 1.2f);
                    // Subtle gothic engraved groove lines
                    if (y == 24 || y == s - 24 || (Mathf.Abs(y - s / 2) < 2 && (x % 32) > 4))
                    {
                        r *= 0.45f; g *= 0.45f; b *= 0.55f;
                    }
                    tex.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateReaverSilverTexture()
        {
            int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
            System.Random rng = new System.Random(811);
            for (int y = 0; y < s; y++)
            {
                float streak = (float)(rng.NextDouble() * 0.06 - 0.03);
                float edgeShine = (y < 14 || y > s - 14) ? 0.10f : 0f;
                for (int x = 0; x < s; x++)
                {
                    float n = (float)(rng.NextDouble() * 0.035 - 0.017f);
                    float v = Mathf.Clamp01(0.76f + streak + n + edgeShine);
                    // Cool pewter-silver steel tint matching the sculpted blades in the video
                    tex.SetPixel(x, y, new Color(v * 0.94f, v * 0.96f, Mathf.Clamp01(v * 1.03f), 1f));
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateReaverRuneTexture()
        {
            int w = 256, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;

                    // Swirling gothic flame-rune wave along the Reaver Vandal handguard!
                    float wave1 = Mathf.Sin(nx * 5.2f) * 0.36f + Mathf.Sin(nx * 11.0f) * 0.14f;
                    float wave2 = -Mathf.Sin(nx * 3.8f + 1.2f) * 0.42f;
                    float d1 = Mathf.Abs(ny - wave1);
                    float d2 = Mathf.Abs(ny - wave2);

                    // Spiral flourish near center-front
                    float sx = nx - 0.35f, sy = ny + 0.05f;
                    float sr = Mathf.Sqrt(sx * sx + sy * sy);
                    float sang = Mathf.Atan2(sy, sx);
                    float spiral = Mathf.Abs(sr - (0.14f + 0.09f * Mathf.Sin(sang * 2f + 0.7f)));

                    float d = Mathf.Min(Mathf.Min(d1, d2 * 1.15f), spiral * 1.1f);
                    float edgeMask = Mathf.Clamp01(1f - Mathf.Max(Mathf.Abs(nx) - 0.86f, 0f) * 7f)
                                   * Mathf.Clamp01(1f - Mathf.Max(Mathf.Abs(ny) - 0.78f, 0f) * 5f);

                    float core = Mathf.Exp(-d * 18f) * edgeMask;
                    float halo = Mathf.Exp(-d * 5.8f) * edgeMask;

                    Color baseCol = new Color(0.08f, 0.05f, 0.14f, 1f);
                    Color violetHalo = new Color(0.48f, 0.14f, 0.96f, 1f);
                    Color hotCore = new Color(0.92f, 0.78f, 1.0f, 1f);

                    Color c = Color.Lerp(baseCol, violetHalo, Mathf.Clamp01(halo * 1.15f));
                    c = Color.Lerp(c, hotCore, Mathf.Clamp01(core * 0.95f));
                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateReaverCrestTexture()
        {
            int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
            for (int y = 0; y < s; y++)
            {
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);

                    // Signature glowing curved crescent/flame medallion seen on rear receiver of Reaver Vandal!
                    float ringDist = Mathf.Abs(r - 0.56f);
                    float crescent = (nx > -0.35f || ny < 0.25f) ? 1f : 0.15f;
                    float innerHook = Mathf.Abs(Mathf.Sqrt((nx - 0.10f) * (nx - 0.10f) + (ny + 0.06f) * (ny + 0.06f)) - 0.24f);
                    float flameTail = Mathf.Abs(ny - (0.35f * Mathf.Sin(nx * 3.6f) + 0.18f)) + Mathf.Max(0f, nx - 0.3f);

                    float d = Mathf.Min(ringDist / Mathf.Max(0.2f, crescent), Mathf.Min(innerHook * 1.05f, flameTail * 1.3f));
                    float mask = Mathf.Clamp01(1f - Mathf.Max(r - 0.85f, 0f) * 6f);
                    float glow = Mathf.Exp(-d * 11f) * mask;
                    float hot = Mathf.Exp(-d * 24f) * mask;
                    float innerMist = Mathf.Exp(-r * 2.5f) * (0.35f + 0.2f * Mathf.Sin(ang * 3f)) * mask;

                    Color bg = new Color(0.12f, 0.08f, 0.20f, 1f);
                    Color violet = new Color(0.52f, 0.18f, 0.98f, 1f);
                    Color bright = new Color(0.90f, 0.78f, 1.0f, 1f);
                    Color c = Color.Lerp(bg, violet, Mathf.Clamp01(glow * 1.15f + innerMist));
                    c = Color.Lerp(c, bright, Mathf.Clamp01(hot * 0.88f));
                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGripTexture()
        {
            int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool knurl = ((x + y) % 8 < 2) || (((x - y) & 7) < 2);
                    float v = knurl ? 0.82f : 1.0f;
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateReaverMuzzleStarTexture()
        {
            int s = 256;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);

                    // Jagged 4-point primary + 4-point secondary gothic star rays!
                    float rays4 = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2f)), 5.5f);
                    float raysDiag = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2f + Mathf.PI * 0.5f)), 8.5f) * 0.65f;
                    float starLen = 0.24f + rays4 * 0.72f + raysDiag * 0.52f;

                    float radialFade = Mathf.Clamp01(1f - r / Mathf.Max(0.08f, starLen));
                    float core = Mathf.Exp(-r * 6.2f);
                    float darkSmokeRing = Mathf.Exp(-Mathf.Pow((r - 0.34f) * 3.8f, 2f)) * 0.45f;

                    float alpha = Mathf.Clamp01(Mathf.Pow(radialFade, 1.35f) + core * 0.9f + darkSmokeRing * (1f - r));
                    if (r >= 0.98f) alpha = 0f;

                    // Hot white-lavender center -> electric neon violet -> dark shadow indigo tips!
                    Color shadowCol = new Color(0.16f, 0.02f, 0.34f, alpha);
                    Color violetCol = new Color(0.66f, 0.24f, 1.0f, alpha);
                    Color coreCol = new Color(0.98f, 0.94f, 1.0f, alpha);

                    Color c = Color.Lerp(shadowCol, violetCol, Mathf.Clamp01(radialFade * 1.45f));
                    c = Color.Lerp(c, coreCol, Mathf.Clamp01(core * 1.35f));
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSoftWispTexture()
        {
            int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);
                    // Organic wispy smoke puff (smooth feathered falloff to exact 0 alpha at edges!)
                    float swirl = 1f + 0.14f * Mathf.Sin(ang * 3f + r * 4.5f) + 0.08f * Mathf.Cos(ang * 5f - r * 3f);
                    float falloff = Mathf.Clamp01(1f - (r * swirl));
                    float alpha = Mathf.Pow(falloff, 2.35f);
                    if (r >= 0.96f) alpha = 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // WEAPON STATE TRIGGERS
        // =========================================================================
        public void SetActiveWeaponModel(int weaponId)
        {
            isMeleeEquipped = false;
            quickMeleeActive = false;
            activeWeaponId = Mathf.Clamp(weaponId, 0, 4);
            for (int i = 0; i < weaponModels.Length; i++)
            {
                if (weaponModels[i] != null) weaponModels[i].SetActive(i == activeWeaponId);
            }
            if (muzzleFlashObj != null && muzzlePoints[activeWeaponId] != null)
            {
                muzzleFlashObj.transform.localPosition = muzzlePoints[activeWeaponId].localPosition;
            }
            if (reaverMuzzleFlash != null) reaverMuzzleFlash.gameObject.SetActive(false);
            if (meleeGroup != null) meleeGroup.gameObject.SetActive(false);
            if (gunPivot != null) gunPivot.gameObject.SetActive(true);

            equipTimer = (activeWeaponId == 0) ? 0.36f : 0.28f;
            inspectTimer = 0f;
            boltCycleTimer = 0f;
            sprayHeat = 0f;
            reaverRuneFlare = (activeWeaponId == 0) ? 1.0f : 0f;
            if (activeWeaponId == 0 && reaverAmbientMistPS != null && Application.isPlaying)
            {
                reaverAmbientMistPS.Emit(10);
            }
        }

        public void SetMeleeMode(bool equipped, int meleeId, Color bladeColor)
        {
            activeMeleeId = Mathf.Clamp(meleeId, 0, 3);
            isMeleeEquipped = equipped;
            quickMeleeActive = false;
            UpdateActiveMeleeMesh();

            if (meleeBladeMat != null)
            {
                meleeBladeMat.color = bladeColor;
                if (meleeBladeMat.HasProperty("_EmissionColor"))
                    meleeBladeMat.SetColor("_EmissionColor", bladeColor * 2.2f);
            }
            if (meleeSlashArcMat != null) meleeSlashArcMat.color = new Color(bladeColor.r, bladeColor.g, bladeColor.b, 0f);

            if (equipped)
            {
                meleeEquipDuration = (activeMeleeId == 0) ? 0.74f : 0.56f;
                meleeEquipTimer = meleeEquipDuration;
                meleeInspectTimer = 0f;
                meleeSwingTimer = 0f;
                if (meleeGroup != null) meleeGroup.gameObject.SetActive(true);
                if (gunPivot != null) gunPivot.gameObject.SetActive(false);
                if (activeMeleeId == 0 && karambitMistPS != null && Application.isPlaying) karambitMistPS.Emit(14);
            }
            else
            {
                if (meleeGroup != null) meleeGroup.gameObject.SetActive(false);
                if (gunPivot != null) gunPivot.gameObject.SetActive(true);
            }
        }

        public void SetActiveMeleeVariant(int meleeId, Color bladeColor)
        {
            activeMeleeId = Mathf.Clamp(meleeId, 0, 3);
            UpdateActiveMeleeMesh();
            if (meleeBladeMat != null)
            {
                meleeBladeMat.color = bladeColor;
                if (meleeBladeMat.HasProperty("_EmissionColor"))
                    meleeBladeMat.SetColor("_EmissionColor", bladeColor * 2.2f);
            }
        }

        public void ApplyWeaponSkin(int weaponId, WeaponSkinDef skin)
        {
            if (skin == null || weaponModels == null || weaponId < 0 || weaponId >= weaponModels.Length) return;
            GameObject model = weaponModels[weaponId];
            if (model == null) return;

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material m = r.material;
                if (m == null) continue;
                string n = r.gameObject.name.ToLowerInvariant();

                bool isEmissive = m.IsKeywordEnabled("_EMISSION") || n.Contains("glow") || n.Contains("rune")
                    || n.Contains("core") || n.Contains("reticle") || n.Contains("dot")
                    || n.Contains("vent") || n.Contains("supp") || n.Contains("crest");
                if (isEmissive)
                {
                    if (m.HasProperty("_Color")) m.color = skin.glowColor;
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", skin.glowColor * 2.6f);
                    }
                    continue;
                }

                bool isTrim = n.Contains("silver") || n.Contains("filigree") || n.Contains("barrel")
                    || n.Contains("bolt") || n.Contains("crown") || n.Contains("fin") || n.Contains("spike")
                    || n.Contains("rail") || n.Contains("thorn") || n.Contains("mount") || n.Contains("objective");
                bool isDark = n.Contains("grip") || n.Contains("mag") || n.Contains("polymer")
                    || n.Contains("drum") || n.Contains("box") || n.Contains("stock") || n.Contains("scope");

                if (isTrim)
                {
                    m.color = skin.trimColor;
                }
                else if (isDark)
                {
                    m.color = skin.darkColor;
                }
                else
                {
                    m.color = skin.bodyColor;
                }
            }

            // Update Reaver Vandal dynamic rune / core / muzzle flash materials when Vandal skin changes
            if (weaponId == 0)
            {
                activeVandalGlowColor = skin.glowColor;
                if (reaverRuneMat != null && reaverRuneMat.HasProperty("_EmissionColor"))
                    reaverRuneMat.SetColor("_EmissionColor", skin.glowColor * 2.2f);
                if (reaverCoreGlowMat != null)
                {
                    reaverCoreGlowMat.color = skin.glowColor;
                    if (reaverCoreGlowMat.HasProperty("_EmissionColor"))
                        reaverCoreGlowMat.SetColor("_EmissionColor", skin.glowColor * 2.8f);
                }
                if (reaverSightDotMat != null)
                {
                    reaverSightDotMat.color = skin.glowColor;
                    if (reaverSightDotMat.HasProperty("_EmissionColor"))
                        reaverSightDotMat.SetColor("_EmissionColor", skin.glowColor * 3.6f);
                }
                if (reaverMuzzleFlashMat != null && reaverMuzzleFlashMat.HasProperty("_Color"))
                    reaverMuzzleFlashMat.color = new Color(skin.glowColor.r, skin.glowColor.g, skin.glowColor.b, 0.95f);
            }
        }

        public void ApplyMeleeSkin(int meleeId, WeaponSkinDef skin)
        {
            if (skin == null || meleeModels == null || meleeId < 0 || meleeId >= meleeModels.Length) return;
            GameObject model = meleeModels[meleeId];
            if (model == null) return;

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material m = r.material;
                if (m == null) continue;
                string n = r.gameObject.name.ToLowerInvariant();

                bool isEmissive = m.IsKeywordEnabled("_EMISSION") || n.Contains("glow") || n.Contains("rune")
                    || n.Contains("edge") || n.Contains("core") || n.Contains("ember") || n.Contains("energy");
                if (isEmissive)
                {
                    if (m.HasProperty("_Color")) m.color = skin.glowColor;
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", skin.glowColor * 2.6f);
                    }
                    continue;
                }

                bool isBladeTrim = n.Contains("blade") || n.Contains("ring") || n.Contains("guard")
                    || n.Contains("teeth") || n.Contains("head") || n.Contains("spike") || n.Contains("steel");
                m.color = isBladeTrim ? skin.trimColor : skin.bodyColor;
            }

            if (meleeId == 0 && karambitRuneMat != null && karambitRuneMat.HasProperty("_EmissionColor"))
            {
                karambitRuneMat.color = skin.glowColor;
                karambitRuneMat.SetColor("_EmissionColor", skin.glowColor * 2.6f);
            }
            if (meleeId == activeMeleeId)
            {
                if (meleeBladeMat != null)
                {
                    meleeBladeMat.color = skin.glowColor;
                    if (meleeBladeMat.HasProperty("_EmissionColor"))
                        meleeBladeMat.SetColor("_EmissionColor", skin.glowColor * 2.2f);
                }
                if (meleeSlashArcMat != null)
                    meleeSlashArcMat.color = new Color(skin.glowColor.r, skin.glowColor.g, skin.glowColor.b, 0f);
            }
        }

        public void TriggerInspect()
        {
            if (isMeleeEquipped)
            {
                meleeInspectDuration = (activeMeleeId == 0) ? 2.65f : 2.2f;
                meleeInspectTimer = meleeInspectDuration;
                if (activeMeleeId == 0 && karambitMistPS != null && Application.isPlaying) karambitMistPS.Emit(10);
                return;
            }
            inspectDuration = (activeWeaponId == 0) ? 3.2f : 2.1f;
            inspectTimer = inspectDuration;
            if (activeWeaponId == 0 && reaverAmbientMistPS != null && Application.isPlaying)
            {
                reaverAmbientMistPS.Emit(8);
            }
        }

        public Vector3 GetMuzzleWorldPosition()
        {
            if (muzzlePoints != null && activeWeaponId < muzzlePoints.Length && muzzlePoints[activeWeaponId] != null)
                return muzzlePoints[activeWeaponId].position;
            return transform.position + transform.forward * 0.7f;
        }

        public void TriggerRecoil(WeaponDef w, bool isADS = false)
        {
            shotCounter++;
            inspectTimer = 0f;
            float adsScale = isADS ? 0.58f : 1.0f;

            if (activeWeaponId == 0)
            {
                // Authentic VALORANT Reaver Vandal punchy kick + snappy upward climb + micro spray twist!
                sprayHeat = Mathf.Min(1f, sprayHeat + 0.22f);
                float sideSign = (shotCounter % 2 == 0 ? 1f : -1f) * (0.45f + sprayHeat * 0.95f);

                recoilPosZ = Mathf.Min(0.105f, recoilPosZ + w.recoilKick * 0.82f * adsScale);
                recoilVelZ += w.recoilKick * 6.8f * adsScale;

                recoilPitch = Mathf.Min(11.5f, recoilPitch + (2.35f + sprayHeat * 1.45f) * adsScale);
                recoilPitchVel += (26f + sprayHeat * 14f) * adsScale;

                recoilYawVel += sideSign * Random.Range(10f, 18f) * adsScale;
                recoilRollVel += -sideSign * Random.Range(24f, 44f) * adsScale;

                vandalBoltKick = 1.0f;
                reaverRuneFlare = 1.25f;
                muzzleDuration = 0.062f;
                muzzleTimer = muzzleDuration;

                if (reaverMuzzleFlash != null)
                {
                    float flashScale = isADS ? Random.Range(0.72f, 0.92f) : Random.Range(0.95f, 1.28f);
                    reaverMuzzleFlash.localScale = Vector3.one * flashScale;
                    reaverMuzzleFlash.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                }
                if (reaverMuzzleSmokePS != null && Application.isPlaying) reaverMuzzleSmokePS.Emit(5);
                if (reaverAmbientMistPS != null && Application.isPlaying) reaverAmbientMistPS.Emit(3);
            }
            else
            {
                recoilPosZ = w.recoilKick * adsScale;
                recoilPitch = w.recoilKick * 52f * adsScale;
                muzzleDuration = w.isSuppressed ? 0.03f : 0.06f;
                muzzleTimer = muzzleDuration;
                if (w.isSniper) boltCycleTimer = 0.75f;
            }

            SpawnBrassCasing();
        }

        public void TriggerMeleeSwing(Color bladeColor, int meleeId = -1, bool isHeavy = false)
        {
            if (meleeId >= 0)
            {
                activeMeleeId = Mathf.Clamp(meleeId, 0, 3);
                UpdateActiveMeleeMesh();
            }
            meleeLastHeavy = isHeavy;
            meleeSwingDuration = isHeavy ? 0.48f : 0.34f;
            meleeSwingTimer = meleeSwingDuration;
            meleeInspectTimer = 0f;
            meleeEquipTimer = 0f;
            meleeComboIndex = (meleeComboIndex + 1) % 2;
            quickMeleeActive = !isMeleeEquipped;

            if (meleeBladeMat != null)
            {
                meleeBladeMat.color = bladeColor;
                if (meleeBladeMat.HasProperty("_EmissionColor"))
                    meleeBladeMat.SetColor("_EmissionColor", bladeColor * (isHeavy ? 3.2f : 2.2f));
            }
            if (meleeSlashArcMat != null)
            {
                meleeSlashArcMat.color = new Color(bladeColor.r, bladeColor.g, bladeColor.b, 0.92f);
            }
            if (activeMeleeId == 0 && karambitMistPS != null && Application.isPlaying)
            {
                karambitMistPS.Emit(isHeavy ? 14 : 7);
            }
        }

        public void SpawnTracerBeam(Vector3 targetPoint, Color tracerColor)
        {
            Vector3 startPos = GetMuzzleWorldPosition();
            if (activeWeaponId == 0)
            {
                // Dual-layer Reaver Vandal spectral tracer dynamically tinted by active weapon skin!
                Color coreTint = Color.Lerp(tracerColor, Color.white, 0.35f);
                Color auraTint = tracerColor * 0.55f;
                if (cachedSpectralTracerCoreMat == null)
                    cachedSpectralTracerCoreMat = CitadelWorldBuilder.MakeEmissiveMat(coreTint, 4.0f);
                if (cachedSpectralTracerAuraMat == null)
                    cachedSpectralTracerAuraMat = CitadelWorldBuilder.MakeEmissiveMat(auraTint, 2.4f);

                cachedSpectralTracerCoreMat.color = coreTint;
                if (cachedSpectralTracerCoreMat.HasProperty("_EmissionColor"))
                    cachedSpectralTracerCoreMat.SetColor("_EmissionColor", coreTint * 4.0f);
                cachedSpectralTracerAuraMat.color = auraTint;
                if (cachedSpectralTracerAuraMat.HasProperty("_EmissionColor"))
                    cachedSpectralTracerAuraMat.SetColor("_EmissionColor", auraTint * 2.4f);

                GameObject coreGo = new GameObject("ReaverTracerCore");
                LineRenderer coreLr = coreGo.AddComponent<LineRenderer>();
                coreLr.positionCount = 2;
                coreLr.startWidth = 0.016f;
                coreLr.endWidth = 0.005f;
                coreLr.sharedMaterial = cachedSpectralTracerCoreMat;
                coreLr.SetPosition(0, startPos);
                coreLr.SetPosition(1, targetPoint);
                Destroy(coreGo, 0.062f);

                GameObject auraGo = new GameObject("ReaverTracerShadow");
                LineRenderer auraLr = auraGo.AddComponent<LineRenderer>();
                auraLr.positionCount = 2;
                auraLr.startWidth = 0.044f;
                auraLr.endWidth = 0.016f;
                auraLr.sharedMaterial = cachedSpectralTracerAuraMat;
                auraLr.SetPosition(0, startPos);
                auraLr.SetPosition(1, targetPoint);
                Destroy(auraGo, 0.075f);
                return;
            }

            GameObject go = new GameObject("ValorantTracer");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.022f;
            lr.endWidth = 0.008f;
            lr.material = CitadelWorldBuilder.MakeEmissiveMat(tracerColor, 3.5f);
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, targetPoint);
            Destroy(go, 0.055f);
        }

        private void SpawnBrassCasing()
        {
            Transform b = (weaponBolts != null && activeWeaponId < weaponBolts.Length) ? weaponBolts[activeWeaponId] : null;
            if (b == null) return;
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "BrassCasing";
            c.transform.position = b.position;
            c.transform.localScale = new Vector3(0.012f, 0.022f, 0.012f);
            c.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeStdMat(
                activeWeaponId == 0 ? new Color(0.58f, 0.44f, 0.72f) : new Color(0.95f, 0.74f, 0.25f),
                0.9f, 0.78f
            );
            Rigidbody rb = c.AddComponent<Rigidbody>();
            Vector3 ejectDir = transform.right * Random.Range(2.4f, 3.6f) + transform.up * Random.Range(1.4f, 2.3f);
            rb.AddForce(ejectDir, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 14f, ForceMode.Impulse);
            c.AddComponent<ShellCasing3D>();
        }

        // =========================================================================
        // FRAME-BY-FRAME ANIMATION UPDATE (Recoil, Muzzle, Inspect, Telekinetic Reload!)
        // =========================================================================
        public void UpdateViewmodel(float dt, bool isADS, bool isSniperScoped, bool reloading, float reloadProgress, float bobPhase)
        {
            if (weaponRig == null || gunPivot == null) return;
            float clampedDt = Mathf.Min(dt, 0.045f);

            // Hide 3D gun mesh cleanly when Sniper Scoped OR when Melee Weapon ([0]) is equipped in hand!
            bool showGun = !isSniperScoped && !isMeleeEquipped && !quickMeleeActive;
            gunPivot.gameObject.SetActive(showGun);

            // Update Articulated Melee Hands, Realistic Unsheathe Draw, Karambit Spin, & Slash Attacks!
            UpdateMeleeAnimation(dt, bobPhase);
            if (!showGun)
            {
                if (muzzleFlashObj != null) muzzleFlashObj.SetActive(false);
                if (reaverMuzzleFlash != null) reaverMuzzleFlash.gameObject.SetActive(false);
                return;
            }

            // 1. MUZZLE FLASH (Reaver Spectral Star vs Standard Flash)
            if (muzzleTimer > 0f && !isSniperScoped)
            {
                muzzleTimer = Mathf.Max(0f, muzzleTimer - dt);
                float tNorm = muzzleTimer / Mathf.Max(0.01f, muzzleDuration);
                if (activeWeaponId == 0)
                {
                    if (muzzleFlashObj != null) muzzleFlashObj.SetActive(false);
                    if (reaverMuzzleFlash != null)
                    {
                        reaverMuzzleFlash.gameObject.SetActive(true);
                        if (reaverMuzzleFlashMat != null)
                        {
                            Color mf = Color.Lerp(activeVandalGlowColor, Color.white, 0.45f);
                            reaverMuzzleFlashMat.color = new Color(mf.r, mf.g, mf.b, Mathf.Clamp01(tNorm * 1.45f));
                        }
                    }
                }
                else
                {
                    if (reaverMuzzleFlash != null) reaverMuzzleFlash.gameObject.SetActive(false);
                    if (muzzleFlashObj != null)
                    {
                        muzzleFlashObj.SetActive(true);
                        muzzleFlashObj.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    }
                }
            }
            else
            {
                if (muzzleFlashObj != null) muzzleFlashObj.SetActive(false);
                if (reaverMuzzleFlash != null) reaverMuzzleFlash.gameObject.SetActive(false);
            }

            // 2. DAMPED SPRING RECOIL RECOVERY (Crisp & weighty!)
            if (activeWeaponId == 0)
            {
                StepSpring(ref recoilPosZ, ref recoilVelZ, 260f, 22f, clampedDt);
                StepSpring(ref recoilPitch, ref recoilPitchVel, 220f, 20f, clampedDt);
                StepSpring(ref recoilYaw, ref recoilYawVel, 180f, 18f, clampedDt);
                StepSpring(ref recoilRoll, ref recoilRollVel, 190f, 17f, clampedDt);
                sprayHeat = Mathf.Max(0f, sprayHeat - dt * 2.4f);
                vandalBoltKick = Mathf.Lerp(vandalBoltKick, 0f, dt * 32f);
            }
            else
            {
                recoilPosZ = Mathf.Lerp(recoilPosZ, 0f, dt * 15f);
                recoilPitch = Mathf.Lerp(recoilPitch, 0f, dt * 15f);
                recoilYaw = Mathf.Lerp(recoilYaw, 0f, dt * 15f);
                recoilRoll = Mathf.Lerp(recoilRoll, 0f, dt * 15f);
            }

            // 3. LIVE PULSING REAVER RUNES (Breathing aura + gunshot/reload surge in active skin color!)
            if (activeWeaponId == 0)
            {
                reaverRuneFlare = Mathf.Lerp(reaverRuneFlare, 0f, dt * 7.5f);
                float breath = 1.65f + 0.35f * Mathf.Sin(Time.time * 3.2f) + reaverRuneFlare * 2.4f;
                if (reaverRuneMat != null && reaverRuneMat.HasProperty("_EmissionColor"))
                    reaverRuneMat.SetColor("_EmissionColor", activeVandalGlowColor * breath);
                if (reaverCoreGlowMat != null && reaverCoreGlowMat.HasProperty("_EmissionColor"))
                    reaverCoreGlowMat.SetColor("_EmissionColor", activeVandalGlowColor * (breath * 1.15f));
                if (reaverSightDotMat != null && reaverSightDotMat.HasProperty("_EmissionColor"))
                    reaverSightDotMat.SetColor("_EmissionColor", Color.Lerp(activeVandalGlowColor, Color.white, 0.35f) * (isADS ? 4.4f : 2.8f));
            }

            if (equipTimer > 0f) equipTimer = Mathf.Max(0f, equipTimer - dt);
            if (inspectTimer > 0f) inspectTimer = Mathf.Max(0f, inspectTimer - dt);
            if (boltCycleTimer > 0f) boltCycleTimer = Mathf.Max(0f, boltCycleTimer - dt);
            if (!reloading)
            {
                reloadLockFlared = false;
                reloadBoltFlared = false;
            }

            // 4. EQUIP, TWO-STAGE REAVER INSPECT (0:52 & 0:56), AND TELEKINETIC RELOAD (1:04-1:06)
            float equipMax = (activeWeaponId == 0) ? 0.36f : 0.28f;
            float equipNorm = equipTimer / equipMax;
            float equipDipY = -Mathf.Sin(equipNorm * Mathf.PI * 0.5f) * 0.24f;
            float equipPitch = equipNorm * 30f;
            float equipRoll = (activeWeaponId == 0) ? -equipNorm * 26f : 0f;

            float inspectPitch = 0f, inspectYaw = 0f, inspectRoll = 0f;
            Vector3 inspectOffset = Vector3.zero;
            if (inspectTimer > 0f && !reloading && !isADS)
            {
                float ip = 1f - (inspectTimer / Mathf.Max(0.1f, inspectDuration));
                if (activeWeaponId == 0)
                {
                    // Exact 2-Stage VALORANT Reaver Inspect from 0:52 -> 0:56!
                    if (ip < 0.40f)
                    {
                        // Stage 1 (0:52): Steep upward-diagonal tilt showcasing silver banana mag & underside fangs
                        float t1 = Mathf.Sin((ip / 0.40f) * Mathf.PI);
                        inspectPitch = -36f * t1;
                        inspectYaw = -26f * t1;
                        inspectRoll = 34f * t1;
                        inspectOffset = new Vector3(-0.045f * t1, 0.032f * t1, 0.03f * t1);
                    }
                    else if (ip < 0.84f)
                    {
                        // Stage 2 (0:56): Full horizontal side profile sweep displaying dorsal spikes & glowing runes!
                        float t2 = Mathf.Sin(((ip - 0.40f) / 0.44f) * Mathf.PI);
                        inspectPitch = -8f * t2;
                        inspectYaw = -64f * t2;
                        inspectRoll = -16f * t2;
                        inspectOffset = new Vector3(-0.095f * t2, 0.015f * t2, 0.055f * t2);
                    }
                }
                else
                {
                    inspectYaw = Mathf.Sin(ip * Mathf.PI) * -42f;
                    inspectRoll = Mathf.Sin(ip * Mathf.PI * 2f) * 24f;
                }
            }

            float reloadRoll = 0f, reloadPitchAdd = 0f, reloadYawAdd = 0f, reloadDropY = 0f, boltPullZ = 0f;
            Vector3 leftHandTargetPos = leftHandBasePos;
            Vector3 leftHandTargetEuler = leftHandBaseEuler;

            if (reloading)
            {
                if (activeWeaponId == 0)
                {
                    // AUTHENTIC REAVER VANDAL TELEKINETIC FLOATING-MAG RELOAD (Matches 1:04-1:06!)
                    if (reaverReloadAuraPS != null && Application.isPlaying && Random.value < dt * 26f)
                        reaverReloadAuraPS.Emit(1);

                    if (reloadProgress < 0.30f)
                    {
                        // Phase 1 (1:04): Left hand pulls off forend & gestures downward-left; mag floats out telekinetically!
                        float p1 = Mathf.SmoothStep(0f, 1f, reloadProgress / 0.30f);
                        reloadRoll = Mathf.Lerp(0f, 20f, p1);
                        reloadPitchAdd = Mathf.Lerp(0f, -12f, p1);
                        reloadYawAdd = Mathf.Lerp(0f, -14f, p1);
                        leftHandTargetPos = leftHandBasePos + new Vector3(-0.14f * p1, -0.10f * p1, -0.06f * p1);
                        leftHandTargetEuler = new Vector3(-24f * p1, 32f * p1, 48f * p1);
                    }
                    else if (reloadProgress < 0.68f)
                    {
                        // Phase 2 (1:06): Rifle tilts dramatically up-left showcasing runes while fresh mag levitates in!
                        float p2 = (reloadProgress - 0.30f) / 0.38f;
                        float liftArc = Mathf.Sin(Mathf.Clamp01(p2) * Mathf.PI * 0.85f);
                        reloadPitchAdd = -12f - 24f * liftArc;
                        reloadYawAdd = -14f - 14f * liftArc;
                        reloadRoll = Mathf.Lerp(20f, -24f, Mathf.SmoothStep(0f, 1f, p2));

                        // Crisp upward telekinetic lock snap at 0.60!
                        bool lockSnap = reloadProgress > 0.58f && reloadProgress < 0.66f;
                        if (lockSnap)
                        {
                            reloadDropY = 0.032f;
                            reloadPitchAdd -= 8f;
                            if (!reloadLockFlared)
                            {
                                reloadLockFlared = true;
                                reaverRuneFlare = 1.6f;
                                if (reaverReloadAuraPS != null && Application.isPlaying) reaverReloadAuraPS.Emit(12);
                            }
                        }

                        // Left hand sweeps upward invoking spectral summon energy
                        leftHandTargetPos = leftHandBasePos + new Vector3(-0.12f, -0.06f + p2 * 0.07f, -0.04f);
                        leftHandTargetEuler = new Vector3(-16f + p2 * 22f, 24f, 36f - p2 * 20f);
                    }
                    else
                    {
                        // Phase 3 (0.68-1.00): Telekinetic bolt yank & dark rune shimmer back to ready!
                        float p3 = (reloadProgress - 0.68f) / 0.32f;
                        float settle = 1f - Mathf.SmoothStep(0f, 1f, p3);
                        reloadPitchAdd = -14f * settle;
                        reloadYawAdd = -14f * settle;
                        reloadRoll = -16f * settle;

                        // Telekinetic bolt pulls back smoothly & snaps shut at 0.86
                        if (reloadProgress < 0.86f)
                        {
                            float bp = (reloadProgress - 0.68f) / 0.18f;
                            boltPullZ = -Mathf.Sin(bp * Mathf.PI * 0.5f) * 0.090f;
                            leftHandTargetPos = leftHandBasePos + new Vector3(-0.08f, -0.01f, -0.04f - bp * 0.11f);
                            leftHandTargetEuler = new Vector3(10f, 18f, 18f);
                        }
                        else
                        {
                            if (!reloadBoltFlared)
                            {
                                reloadBoltFlared = true;
                                reaverRuneFlare = 1.35f;
                                if (reaverAmbientMistPS != null && Application.isPlaying) reaverAmbientMistPS.Emit(8);
                            }
                            float ret = (reloadProgress - 0.86f) / 0.14f;
                            leftHandTargetPos = Vector3.Lerp(leftHandBasePos + new Vector3(-0.06f, -0.02f, -0.10f), leftHandBasePos, ret);
                            leftHandTargetEuler = Vector3.Lerp(new Vector3(10f, 12f, 14f), leftHandBaseEuler, ret);
                        }
                    }
                }
                else
                {
                    // Classic tactical reloads for Weapons 1-4
                    if (reloadProgress < 0.35f) { reloadRoll = 30f; reloadPitchAdd = 14f; }
                    else if (reloadProgress < 0.68f)
                    {
                        reloadRoll = 26f;
                        bool slapWindow = reloadProgress > 0.42f && reloadProgress < 0.54f;
                        reloadPitchAdd = slapWindow ? -10f : 10f;
                        reloadDropY = slapWindow ? 0.036f : -0.015f;
                    }
                    else if (reloadProgress < 0.90f)
                    {
                        reloadRoll = activeWeaponId == 1 ? 14f : -18f;
                        reloadPitchAdd = 6f;
                        boltPullZ = -0.085f;
                    }
                }
            }

            // Apply left hand animation smoothly
            if (leftHandRig != null)
            {
                leftHandRig.localPosition = Vector3.Lerp(leftHandRig.localPosition, leftHandTargetPos, dt * 16f);
                leftHandRig.localRotation = Quaternion.Slerp(leftHandRig.localRotation, Quaternion.Euler(leftHandTargetEuler), dt * 16f);
            }

            // Apply weapon rig position & rotation
            Vector3 targetPos = (isADS ? adsPos : hipPos) + inspectOffset;
            Vector3 targetEuler = isADS ? adsEuler : hipEuler;
            weaponRig.localPosition = Vector3.Lerp(weaponRig.localPosition, targetPos, dt * 16f);

            Quaternion desiredRot = Quaternion.Euler(
                targetEuler.x - recoilPitch + reloadPitchAdd + equipPitch + inspectPitch,
                targetEuler.y + recoilYaw + reloadYawAdd + inspectYaw,
                targetEuler.z + recoilRoll + reloadRoll + equipRoll + inspectRoll
            );
            weaponRig.localRotation = Quaternion.Slerp(weaponRig.localRotation, desiredRot, dt * (activeWeaponId == 0 ? 22f : 14f));

            float swayAmp = isADS ? 0.0014f : 0.0095f;
            float recoilLiftY = (activeWeaponId == 0) ? recoilPosZ * 0.22f : 0f;
            gunPivot.localPosition = new Vector3(
                Mathf.Cos(bobPhase * 0.5f) * swayAmp,
                Mathf.Abs(Mathf.Sin(bobPhase)) * swayAmp + reloadDropY + equipDipY + recoilLiftY,
                -recoilPosZ
            );

            // 5. ANIMATE ACTIVE WEAPON'S MAGAZINE (Telekinetic Floating Banana Mag for Reaver Vandal!)
            Transform mag = weaponMags[activeWeaponId];
            if (mag != null)
            {
                if (activeWeaponId == 0)
                {
                    Vector3 magPos = vandalMagBasePos;
                    Quaternion magRot = Quaternion.identity;
                    if (reloading)
                    {
                        if (reloadProgress < 0.34f)
                        {
                            // Ejected mag levitates smoothly down & drifts away while rotating eerily
                            float ep = Mathf.SmoothStep(0f, 1f, reloadProgress / 0.34f);
                            magPos = vandalMagBasePos + new Vector3(-0.06f * ep, -0.34f * ep, -0.05f * ep);
                            magRot = Quaternion.Euler(32f * ep, -40f * ep, 22f * ep);
                        }
                        else if (reloadProgress < 0.62f)
                        {
                            // Fresh spectral mag levitates straight up out of shadow mist and locks into magwell!
                            float ip = Mathf.SmoothStep(0f, 1f, (reloadProgress - 0.34f) / 0.28f);
                            float rem = 1f - ip;
                            magPos = vandalMagBasePos + new Vector3(0.03f * rem, -0.32f * rem, 0.02f * rem);
                            magRot = Quaternion.Euler(-24f * rem, 30f * rem, -12f * rem);
                        }
                    }
                    mag.localPosition = magPos;
                    mag.localRotation = magRot;
                }
                else
                {
                    float baseY = (activeWeaponId == 3) ? -0.075f : -0.10f;
                    float curY = baseY;
                    if (reloading)
                    {
                        if (reloadProgress < 0.36f) curY = baseY - (reloadProgress / 0.36f) * 0.25f;
                        else if (reloadProgress < 0.50f) curY = (baseY - 0.25f) + ((reloadProgress - 0.36f) / 0.14f) * 0.25f;
                    }
                    mag.localPosition = new Vector3(mag.localPosition.x, curY, mag.localPosition.z);
                }
            }

            // 6. ANIMATE CHARGING BOLT & ODIN TOP COVER
            Transform bolt = weaponBolts[activeWeaponId];
            if (bolt != null)
            {
                if (activeWeaponId == 0)
                {
                    bolt.localPosition = new Vector3(0.036f, 0.020f, 0.042f - vandalBoltKick * 0.088f + boltPullZ);
                }
                else if (activeWeaponId == 4)
                {
                    float lift = 0f, back = 0f;
                    if (boltCycleTimer > 0f)
                    {
                        float bp = 1f - (boltCycleTimer / 0.75f);
                        lift = Mathf.Sin(bp * Mathf.PI) * 58f;
                        back = Mathf.Sin(bp * Mathf.PI) * -0.07f;
                    }
                    else if (reloading && reloadProgress > 0.72f)
                    {
                        float bp = (reloadProgress - 0.72f) / 0.28f;
                        lift = Mathf.Sin(bp * Mathf.PI) * 58f;
                        back = Mathf.Sin(bp * Mathf.PI) * -0.07f;
                    }
                    bolt.localPosition = new Vector3(0.034f, 0.02f, -0.06f + back);
                    bolt.localRotation = Quaternion.Euler(0f, 0f, lift);
                }
                else
                {
                    bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, 0.03f - recoilPosZ * 1.4f + boltPullZ);
                }
            }

            if (odinTopCover != null)
            {
                float coverAngle = (activeWeaponId == 3 && reloading && reloadProgress > 0.08f && reloadProgress < 0.75f) ? -68f : 0f;
                odinTopCover.localRotation = Quaternion.Slerp(odinTopCover.localRotation, Quaternion.Euler(coverAngle, 0f, 0f), dt * 12f);
            }
        }

        private void UpdateMeleeAnimation(float dt, float bobPhase)
        {
            if (meleeGroup == null || meleeRightArmPivot == null) return;

            if (meleeSwingTimer > 0f)
            {
                meleeSwingTimer = Mathf.Max(0f, meleeSwingTimer - dt);
                if (meleeSwingTimer <= 0f) quickMeleeActive = false;
            }
            if (meleeEquipTimer > 0f) meleeEquipTimer = Mathf.Max(0f, meleeEquipTimer - dt);
            if (meleeInspectTimer > 0f) meleeInspectTimer = Mathf.Max(0f, meleeInspectTimer - dt);

            bool showMelee = isMeleeEquipped || quickMeleeActive;
            meleeGroup.gameObject.SetActive(showMelee);
            if (!showMelee)
            {
                if (meleeSlashArcObj != null) meleeSlashArcObj.SetActive(false);
                return;
            }

            // Animate pulsing Karambit runes & motorized Chainsword saw teeth!
            if (activeMeleeId == 0 && karambitRuneMat != null && karambitRuneMat.HasProperty("_EmissionColor"))
            {
                float kPulse = 1.95f + 0.45f * Mathf.Sin(Time.time * 4.0f) + (meleeSwingTimer > 0f ? 1.8f : 0f) + (meleeEquipTimer > 0f ? 1.2f : 0f);
                karambitRuneMat.SetColor("_EmissionColor", new Color(0.68f, 0.26f, 1.0f) * kPulse);
            }
            if (activeMeleeId == 2 && chainswordTeethRoot != null)
            {
                float revSpeed = meleeSwingTimer > 0f ? 32f : 11f;
                float sawJitter = Mathf.Repeat(Time.time * revSpeed, 1f) * 0.044f;
                chainswordTeethRoot.localPosition = new Vector3(0f, 0.34f + sawJitter - 0.022f, 0.052f);
            }

            // Base combat-ready stance with smooth breathing + sprint sway
            float swayX = Mathf.Cos(bobPhase * 0.5f) * 0.015f;
            float swayY = Mathf.Abs(Mathf.Sin(bobPhase)) * 0.015f + Mathf.Sin(Time.time * 2.2f) * 0.004f;
            float revVib = (activeMeleeId == 2) ? Mathf.Sin(Time.time * 58f) * 0.0022f : 0f;

            Vector3 rPos = meleeRightHandBasePos + new Vector3(swayX + revVib, swayY + revVib, 0f);
            Vector3 rEuler = meleeRightHandBaseEuler + new Vector3(0f, 0f, -swayX * 180f);
            Vector3 lPos = meleeLeftHandBasePos + new Vector3(-swayX * 0.7f, swayY * 0.8f, 0f);
            Vector3 lEuler = meleeLeftHandBaseEuler;
            float karambitSpinX = 0f;

            // Adjust natural grip stance for Katana/Chainsword/Warhammer vs Karambit reverse grip
            if (activeMeleeId == 1)
            {
                rPos += new Vector3(0.02f, -0.05f, 0.02f);
                rEuler += new Vector3(8f, -12f, 24f);
                lPos += new Vector3(0.11f, -0.02f, 0.04f);
            }
            else if (activeMeleeId == 2 || activeMeleeId == 3)
            {
                rPos += new Vector3(0.03f, -0.07f, 0.03f);
                rEuler += new Vector3(14f, -10f, 18f);
            }

            // 1. REALISTIC UNSHEATHING / EQUIP DRAW ANIMATION (Press [0]!)
            if (meleeEquipTimer > 0f && meleeSwingTimer <= 0f)
            {
                float ep = 1f - (meleeEquipTimer / Mathf.Max(0.1f, meleeEquipDuration));
                if (activeMeleeId == 0)
                {
                    // Authentic VALORANT Reaver Karambit Draw: Pull from lower-right hip -> 360-deg Index-Finger Twirl -> Reverse-Grip Snap!
                    if (ep < 0.28f)
                    {
                        float p1 = Mathf.SmoothStep(0f, 1f, ep / 0.28f);
                        rPos = Vector3.Lerp(meleeRightHandBasePos + new Vector3(0.14f, -0.34f, -0.18f), meleeRightHandBasePos + new Vector3(-0.02f, 0.02f, 0.04f), p1);
                        rEuler = Vector3.Lerp(new Vector3(52f, -35f, -28f), new Vector3(-6f, -28f, 24f), p1);
                        lPos = Vector3.Lerp(meleeLeftHandBasePos + new Vector3(-0.12f, -0.26f, -0.12f), meleeLeftHandBasePos, p1);
                    }
                    else if (ep < 0.78f)
                    {
                        // High-speed 360-degree Finger-Ring Spin around the gloved index finger!
                        float p2 = (ep - 0.28f) / 0.50f;
                        float spinEase = 1f - Mathf.Pow(1f - p2, 3f);
                        karambitSpinX = -395f * spinEase;
                        float liftArc = Mathf.Sin(p2 * Mathf.PI);
                        rPos += new Vector3(-0.03f * liftArc, 0.035f * liftArc, 0.045f * liftArc);
                        rEuler += new Vector3(-12f * liftArc, -10f * liftArc, 18f * liftArc);
                    }
                    else
                    {
                        // Crisp Reverse-Grip Lock Snap!
                        float p3 = (ep - 0.78f) / 0.22f;
                        float settle = 1f - Mathf.SmoothStep(0f, 1f, p3);
                        karambitSpinX = -35f * settle;
                        rPos += new Vector3(0f, -0.015f * Mathf.Sin(p3 * Mathf.PI), -0.022f * Mathf.Sin(p3 * Mathf.PI));
                        rEuler += new Vector3(9f * Mathf.Sin(p3 * Mathf.PI), 0f, -8f * settle);
                    }
                }
                else if (activeMeleeId == 1)
                {
                    // Oni Katana: Diagonal sweep out of lower-left scabbard!
                    float drawEase = Mathf.SmoothStep(0f, 1f, ep);
                    float arc = Mathf.Sin(drawEase * Mathf.PI);
                    rPos = Vector3.Lerp(new Vector3(-0.32f, -0.28f, -0.10f), rPos, drawEase) + new Vector3(0f, 0.05f * arc, 0.06f * arc);
                    rEuler = Vector3.Lerp(new Vector3(40f, 55f, -72f), rEuler, drawEase);
                    lPos = Vector3.Lerp(meleeLeftHandBasePos + new Vector3(0f, -0.22f, 0f), lPos, drawEase);
                }
                else
                {
                    // Chainsword / Warhammer: Heavy upward heave + wrist lock
                    float drawEase = Mathf.SmoothStep(0f, 1f, ep);
                    rPos = Vector3.Lerp(rPos + new Vector3(0.10f, -0.36f, -0.14f), rPos, drawEase);
                    rEuler = Vector3.Lerp(rEuler + new Vector3(48f, -20f, -35f), rEuler, drawEase);
                    lPos = Vector3.Lerp(lPos + new Vector3(-0.10f, -0.26f, -0.10f), lPos, drawEase);
                }
            }

            // 2. KARAMBIT CONTINUOUS FINGER-RING SPIN INSPECT ([Y] while Melee Equipped!)
            if (meleeInspectTimer > 0f && meleeSwingTimer <= 0f)
            {
                float ip = 1f - (meleeInspectTimer / Mathf.Max(0.1f, meleeInspectDuration));
                float env = Mathf.Sin(Mathf.Clamp01(ip) * Mathf.PI);
                if (activeMeleeId == 0)
                {
                    // 3 Continuous Smooth Finger-Ring Spins (1080 deg!) around the index finger!
                    float spinCurve = Mathf.SmoothStep(0f, 1f, ip);
                    karambitSpinX = -1080f * spinCurve;
                    rPos += new Vector3(-0.085f * env, 0.042f * env, 0.065f * env);
                    rEuler += new Vector3(-14f * env, -22f * env, 26f * env);
                }
                else
                {
                    rPos += new Vector3(-0.09f * env, 0.03f * env, 0.05f * env);
                    rEuler += new Vector3(-10f * env, -42f * env, 38f * Mathf.Sin(ip * Mathf.PI * 2f));
                }
            }

            // 3. REALISTIC SLASH & HEAVY STAB ANIMATIONS (LMB Light Combo / RMB Heavy Rip!)
            bool showArc = false;
            if (meleeSwingTimer > 0f)
            {
                float sp = 1f - (meleeSwingTimer / Mathf.Max(0.05f, meleeSwingDuration));
                if (meleeLastHeavy)
                {
                    // RMB HEAVY STAB / OVERHEAD RIP: Wind-up high-right -> Explosive deep forward thrust -> Recoil pull!
                    if (sp < 0.26f)
                    {
                        float w = Mathf.SmoothStep(0f, 1f, sp / 0.26f);
                        rPos += new Vector3(0.10f * w, 0.14f * w, -0.09f * w);
                        rEuler += new Vector3(-42f * w, -24f * w, -28f * w);
                        lPos += new Vector3(-0.06f * w, -0.04f * w, -0.06f * w);
                    }
                    else if (sp < 0.62f)
                    {
                        float st = Mathf.SmoothStep(0f, 1f, (sp - 0.26f) / 0.36f);
                        rPos += Vector3.Lerp(new Vector3(0.10f, 0.14f, -0.09f), new Vector3(-0.24f, -0.09f, 0.22f), st);
                        rEuler += Vector3.Lerp(new Vector3(-42f, -24f, -28f), new Vector3(62f, 38f, 68f), st);
                        lPos += new Vector3(-0.08f, -0.08f, -0.10f);
                        showArc = true;
                        if (meleeSlashArcObj != null)
                        {
                            meleeSlashArcObj.transform.localPosition = new Vector3(-0.02f, 0.03f, 0.16f);
                            meleeSlashArcObj.transform.localEulerAngles = new Vector3(0f, 0f, -38f);
                            meleeSlashArcObj.transform.localScale = new Vector3(0.86f, 0.50f, 1f);
                        }
                    }
                    else
                    {
                        float rec = Mathf.SmoothStep(0f, 1f, (sp - 0.62f) / 0.38f);
                        rPos += Vector3.Lerp(new Vector3(-0.24f, -0.09f, 0.22f), Vector3.zero, rec);
                        rEuler += Vector3.Lerp(new Vector3(62f, 38f, 68f), Vector3.zero, rec);
                    }
                }
                else
                {
                    // LMB LIGHT COMBO: Alternating crisp Right->Left forehand & Left->Right backhand slashes!
                    float dir = (meleeComboIndex == 0) ? 1f : -1f;
                    if (sp < 0.22f)
                    {
                        float w = Mathf.SmoothStep(0f, 1f, sp / 0.22f);
                        rPos += new Vector3(0.12f * dir * w, 0.05f * w, -0.05f * w);
                        rEuler += new Vector3(-16f * w, -28f * dir * w, -34f * dir * w);
                    }
                    else if (sp < 0.62f)
                    {
                        float sw = Mathf.SmoothStep(0f, 1f, (sp - 0.22f) / 0.40f);
                        rPos += Vector3.Lerp(new Vector3(0.12f * dir, 0.05f, -0.05f), new Vector3(-0.34f * dir, -0.06f, 0.16f), sw);
                        rEuler += Vector3.Lerp(new Vector3(-16f, -28f * dir, -34f * dir), new Vector3(36f, 54f * dir, 72f * dir), sw);
                        lPos += new Vector3(-0.05f * sw, -0.05f * sw, -0.06f * sw);
                        showArc = true;
                        if (meleeSlashArcObj != null)
                        {
                            meleeSlashArcObj.transform.localPosition = new Vector3(0f, 0.02f, 0.14f);
                            meleeSlashArcObj.transform.localEulerAngles = new Vector3(0f, 0f, dir > 0f ? -16f : 164f);
                            meleeSlashArcObj.transform.localScale = new Vector3(0.72f, 0.38f, 1f);
                        }
                    }
                    else
                    {
                        float rec = Mathf.SmoothStep(0f, 1f, (sp - 0.62f) / 0.38f);
                        rPos += Vector3.Lerp(new Vector3(-0.34f * dir, -0.06f, 0.16f), Vector3.zero, rec);
                        rEuler += Vector3.Lerp(new Vector3(36f, 54f * dir, 72f * dir), Vector3.zero, rec);
                    }
                }
            }

            // Apply transforms smoothly
            meleeRightArmPivot.localPosition = Vector3.Lerp(meleeRightArmPivot.localPosition, rPos, dt * 28f);
            meleeRightArmPivot.localRotation = Quaternion.Slerp(meleeRightArmPivot.localRotation, Quaternion.Euler(rEuler), dt * 28f);

            if (meleeLeftArmPivot != null)
            {
                meleeLeftArmPivot.localPosition = Vector3.Lerp(meleeLeftArmPivot.localPosition, lPos, dt * 20f);
                meleeLeftArmPivot.localRotation = Quaternion.Slerp(meleeLeftArmPivot.localRotation, Quaternion.Euler(lEuler), dt * 20f);
            }
            if (karambitRingPivot != null)
            {
                karambitRingPivot.localRotation = Quaternion.Euler(karambitSpinX, 0f, 0f);
            }
            if (meleeSlashArcObj != null)
            {
                meleeSlashArcObj.SetActive(showArc);
            }
        }

        private static void StepSpring(ref float val, ref float vel, float stiffness, float damping, float dt)
        {
            float force = -stiffness * val - damping * vel;
            vel += force * dt;
            val += vel * dt;
            if (Mathf.Abs(val) < 0.0001f && Mathf.Abs(vel) < 0.0001f)
            {
                val = 0f;
                vel = 0f;
            }
        }
    }
}
