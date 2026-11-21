using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// 5 Distinct VALORANT-Inspired Demonic 3D Weapon Models (Reaver Vandal, Inferno Phantom w/ Silencer,
    /// Hell-Judge Drum Shotgun, Soul-Shredder Odin w/ Brass Ammo Belt & Top Cover, Reaver Operator Sniper
    /// w/ Scope & Cycling Bolt Handle), plus Per-Weapon Authentic Valorant Reload Animations, Weapon Equip
    /// Pull-Out, Weapon Inspect ([Y]), 3D Karambit/Katana Melee Swings ([V]), and 3D Bullet Tracer Beams!
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

        private readonly Vector3 hipPos = new Vector3(0.25f, -0.23f, 0.46f);
        private readonly Vector3 hipEuler = new Vector3(1.2f, -3.5f, 0.8f);
        private readonly Vector3 adsPos = new Vector3(0.0f, -0.145f, 0.34f);
        private readonly Vector3 adsEuler = Vector3.zero;

        private int activeWeaponId = 0;
        private float recoilOffset;
        private float recoilPitch;
        private float muzzleTimer;
        private float equipTimer;
        private float inspectTimer;
        private float boltCycleTimer;
        private float meleeSwingTimer;

        private void Start()
        {
            if (weaponRig == null || gunPivot == null || weaponModels == null || weaponModels.Length < 5 || weaponModels[0] == null)
            {
                BuildViewmodel();
            }
            // Strip any legacy Point Light gizmo object from older saved scenes
            if (muzzleLight != null)
            {
                Destroy(muzzleLight.gameObject);
                muzzleLight = null;
            }
            if (gunPivot != null)
            {
                Transform legacyLight = gunPivot.Find("MuzzleLight");
                if (legacyLight != null) Destroy(legacyLight.gameObject);
            }
        }

        public void BuildViewmodel()
        {
            if (weaponRig != null)
            {
                if (Application.isPlaying) Destroy(weaponRig.gameObject);
                else DestroyImmediate(weaponRig.gameObject);
            }

            GameObject rigGo = new GameObject("WeaponRig");
            rigGo.transform.SetParent(transform, false);
            rigGo.transform.localPosition = hipPos;
            rigGo.transform.localEulerAngles = hipEuler;
            weaponRig = rigGo.transform;

            GameObject pivotGo = new GameObject("GunPivot");
            pivotGo.transform.SetParent(weaponRig, false);
            gunPivot = pivotGo.transform;

            Material obsidianMetal = CitadelWorldBuilder.MakeStdMat(new Color(0.10f, 0.10f, 0.12f), 0.88f, 0.72f);
            Material darkPolymer = CitadelWorldBuilder.MakeStdMat(new Color(0.06f, 0.06f, 0.08f), 0.25f, 0.35f);
            Material silverChrome = CitadelWorldBuilder.MakeStdMat(new Color(0.68f, 0.72f, 0.78f), 0.94f, 0.82f);
            Material brassGold = CitadelWorldBuilder.MakeStdMat(new Color(0.92f, 0.72f, 0.25f), 0.90f, 0.76f);
            Material gloveMat = CitadelWorldBuilder.MakeStdMat(new Color(0.09f, 0.10f, 0.11f), 0.12f, 0.25f);
            Material sleeveMat = CitadelWorldBuilder.MakeStdMat(new Color(0.16f, 0.14f, 0.18f), 0.08f, 0.18f);

            Material vandalGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.18f, 0.22f), 2.8f);
            Material phantomGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.14f, 0.88f, 1f), 2.8f);
            Material judgeGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.48f, 0.12f), 2.8f);
            Material odinGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.80f, 0.20f), 2.8f);
            Material opGlow = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.72f, 0.24f, 1f), 3.2f);

            weaponModels = new GameObject[5];
            weaponMags = new Transform[5];
            weaponBolts = new Transform[5];
            muzzlePoints = new Transform[5];

            // Shared Gloved Hands & Sleeves
            GameObject hands = new GameObject("HandsRig");
            hands.transform.SetParent(gunPivot, false);
            Part(hands.transform, "LeftGlove", PrimitiveType.Cube, new Vector3(-0.032f, -0.048f, 0.24f), new Vector3(0.072f, 0.065f, 0.10f), gloveMat).transform.localRotation = Quaternion.Euler(0, 0, -14f);
            Part(hands.transform, "LeftArm", PrimitiveType.Cylinder, new Vector3(-0.11f, -0.16f, 0.06f), new Vector3(0.105f, 0.17f, 0.105f), sleeveMat).transform.localRotation = Quaternion.Euler(60f, 0f, 22f);
            Part(hands.transform, "RightGlove", PrimitiveType.Cube, new Vector3(0.01f, -0.078f, -0.12f), new Vector3(0.066f, 0.066f, 0.088f), gloveMat);

            // =====================================================================
            // MODEL 0: REAVER VANDAL (AK-Style Curved Banana Mag, Reaver Spikes, Holo Dot)
            // =====================================================================
            GameObject vandal = new GameObject("Model_ReaverVandal");
            vandal.transform.SetParent(gunPivot, false);
            weaponModels[0] = vandal;
            Part(vandal.transform, "Receiver", PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.064f, 0.088f, 0.42f), obsidianMetal);
            Part(vandal.transform, "TopCover", PrimitiveType.Cube, new Vector3(0, 0.048f, -0.02f), new Vector3(0.056f, 0.026f, 0.36f), darkPolymer);
            // Glowing Reaver Rune Core along the side
            Part(vandal.transform, "ReaverCore", PrimitiveType.Cube, new Vector3(0, 0.008f, 0.06f), new Vector3(0.068f, 0.022f, 0.24f), vandalGlow);
            // Valorant Vandal Angled Handguard & Exposed Gas Tube + Hooded Front Sight
            Part(vandal.transform, "Handguard", PrimitiveType.Cube, new Vector3(0, -0.004f, 0.29f), new Vector3(0.056f, 0.072f, 0.22f), darkPolymer);
            Part(vandal.transform, "GasTube", PrimitiveType.Cylinder, new Vector3(0, 0.036f, 0.30f), new Vector3(0.020f, 0.11f, 0.020f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(vandal.transform, "Barrel", PrimitiveType.Cylinder, new Vector3(0, 0.008f, 0.52f), new Vector3(0.024f, 0.16f, 0.024f), obsidianMetal).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(vandal.transform, "MuzzleBrake", PrimitiveType.Cube, new Vector3(0, 0.008f, 0.67f), new Vector3(0.036f, 0.036f, 0.07f), silverChrome);
            // Holo Red-Dot Reflex Sight
            Part(vandal.transform, "HoloFrameL", PrimitiveType.Cube, new Vector3(-0.022f, 0.095f, -0.02f), new Vector3(0.005f, 0.045f, 0.06f), obsidianMetal);
            Part(vandal.transform, "HoloFrameR", PrimitiveType.Cube, new Vector3(0.022f, 0.095f, -0.02f), new Vector3(0.005f, 0.045f, 0.06f), obsidianMetal);
            Part(vandal.transform, "HoloTop", PrimitiveType.Cube, new Vector3(0, 0.118f, -0.02f), new Vector3(0.048f, 0.005f, 0.06f), obsidianMetal);
            Part(vandal.transform, "HoloDot", PrimitiveType.Sphere, new Vector3(0, 0.098f, -0.02f), Vector3.one * 0.006f, vandalGlow);
            // Right-Side AK Charging Handle Bolt
            weaponBolts[0] = Part(vandal.transform, "VandalBolt", PrimitiveType.Cube, new Vector3(0.042f, 0.018f, 0.04f), new Vector3(0.028f, 0.016f, 0.08f), silverChrome).transform;
            // Curved Vandal Banana Magazine
            GameObject vMagRoot = new GameObject("VandalBananaMag");
            vMagRoot.transform.SetParent(vandal.transform, false);
            vMagRoot.transform.localPosition = new Vector3(0, -0.095f, 0.09f);
            Part(vMagRoot.transform, "MagUpper", PrimitiveType.Cube, Vector3.zero, new Vector3(0.040f, 0.10f, 0.064f), darkPolymer).transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            Part(vMagRoot.transform, "MagLower", PrimitiveType.Cube, new Vector3(0, -0.085f, 0.032f), new Vector3(0.038f, 0.10f, 0.062f), obsidianMetal).transform.localRotation = Quaternion.Euler(-28f, 0, 0);
            weaponMags[0] = vMagRoot.transform;
            Part(vandal.transform, "Grip", PrimitiveType.Cube, new Vector3(0, -0.085f, -0.12f), new Vector3(0.042f, 0.11f, 0.052f), darkPolymer).transform.localRotation = Quaternion.Euler(18f, 0, 0);
            muzzlePoints[0] = MakeEmpty(vandal.transform, "Muzzle0", new Vector3(0, 0.008f, 0.72f));

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
            // Signature Long Cylindrical Phantom Suppressor + Heat Rings
            Part(phantom.transform, "Suppressor", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.58f), new Vector3(0.048f, 0.19f, 0.048f), darkPolymer).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(phantom.transform, "SuppRing1", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.50f), new Vector3(0.052f, 0.008f, 0.052f), phantomGlow).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(phantom.transform, "SuppRing2", PrimitiveType.Cylinder, new Vector3(0, 0.006f, 0.66f), new Vector3(0.052f, 0.008f, 0.052f), phantomGlow).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // EOTech Cyan Holo Sight
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
            // Iconic Wide Drum Magazine underneath!
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
            // Flip-Up Top Feed Cover (Animates open during Odin reload!)
            GameObject coverPivot = new GameObject("OdinTopCoverPivot");
            coverPivot.transform.SetParent(odin.transform, false);
            coverPivot.transform.localPosition = new Vector3(0, 0.052f, 0.14f);
            Part(coverPivot.transform, "TopCoverPlate", PrimitiveType.Cube, new Vector3(0, 0.01f, -0.13f), new Vector3(0.095f, 0.025f, 0.28f), darkPolymer);
            Part(coverPivot.transform, "OdinReflex", PrimitiveType.Cube, new Vector3(0, 0.048f, -0.14f), new Vector3(0.068f, 0.042f, 0.05f), obsidianMetal);
            Part(coverPivot.transform, "OdinReflexDot", PrimitiveType.Sphere, new Vector3(0, 0.050f, -0.14f), Vector3.one * 0.008f, odinGlow);
            odinTopCover = coverPivot.transform;
            // Dual Heavy Rotary Barrels
            Part(odin.transform, "BarrelL", PrimitiveType.Cylinder, new Vector3(-0.024f, 0.01f, 0.48f), new Vector3(0.032f, 0.24f, 0.032f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(odin.transform, "BarrelR", PrimitiveType.Cylinder, new Vector3(0.024f, 0.01f, 0.48f), new Vector3(0.032f, 0.24f, 0.032f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(odin.transform, "OdinHeatCore", PrimitiveType.Cube, new Vector3(0, 0.01f, 0.38f), new Vector3(0.088f, 0.024f, 0.18f), odinGlow);
            weaponBolts[3] = Part(odin.transform, "OdinBolt", PrimitiveType.Cube, new Vector3(0.062f, 0.01f, 0.04f), new Vector3(0.024f, 0.024f, 0.09f), silverChrome).transform;
            // Side-Mounted 100-Round Box Mag + Visible Brass Ammo Belt!
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
            Part(op.transform, "SniperChassis", PrimitiveType.Cube, new Vector3(0, -0.005f, 0.02f), new Vector3(0.068f, 0.092f, 0.50f), obsidianMetal);
            Part(op.transform, "OpEnergyCore", PrimitiveType.Cube, new Vector3(0, 0.012f, 0.10f), new Vector3(0.072f, 0.018f, 0.30f), opGlow);
            // Ultra-Long Heavy Sniper Barrel + Rectangular Slotted Muzzle Brake
            Part(op.transform, "OpBarrel", PrimitiveType.Cylinder, new Vector3(0, 0.016f, 0.58f), new Vector3(0.028f, 0.34f, 0.028f), silverChrome).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "OpMuzzleBrake", PrimitiveType.Cube, new Vector3(0, 0.016f, 0.92f), new Vector3(0.056f, 0.042f, 0.095f), obsidianMetal);
            // Oversized Dual-Ring Telescopic Sniper Scope
            Part(op.transform, "ScopeMountF", PrimitiveType.Cube, new Vector3(0, 0.065f, 0.08f), new Vector3(0.026f, 0.045f, 0.024f), obsidianMetal);
            Part(op.transform, "ScopeMountR", PrimitiveType.Cube, new Vector3(0, 0.065f, -0.08f), new Vector3(0.026f, 0.045f, 0.024f), obsidianMetal);
            Part(op.transform, "ScopeBody", PrimitiveType.Cylinder, new Vector3(0, 0.105f, 0.02f), new Vector3(0.048f, 0.18f, 0.048f), darkPolymer).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "ScopeObjective", PrimitiveType.Cylinder, new Vector3(0, 0.105f, 0.18f), new Vector3(0.064f, 0.055f, 0.064f), obsidianMetal).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Part(op.transform, "ScopeGlowLens", PrimitiveType.Sphere, new Vector3(0, 0.105f, -0.15f), new Vector3(0.042f, 0.042f, 0.01f), opGlow);
            // Bolt-Action Handle (Lifts up and cycles back after every Operator shot!)
            GameObject opBoltPivot = new GameObject("OperatorBoltHandle");
            opBoltPivot.transform.SetParent(op.transform, false);
            opBoltPivot.transform.localPosition = new Vector3(0.034f, 0.02f, -0.06f);
            Part(opBoltPivot.transform, "BoltStem", PrimitiveType.Cube, new Vector3(0.026f, 0, 0), new Vector3(0.055f, 0.014f, 0.014f), silverChrome);
            Part(opBoltPivot.transform, "BoltKnob", PrimitiveType.Sphere, new Vector3(0.055f, -0.012f, 0), Vector3.one * 0.024f, silverChrome);
            weaponBolts[4] = opBoltPivot.transform;
            weaponMags[4] = Part(op.transform, "OpMag", PrimitiveType.Cube, new Vector3(0, -0.095f, 0.06f), new Vector3(0.042f, 0.11f, 0.085f), darkPolymer).transform;
            Part(op.transform, "Grip", PrimitiveType.Cube, new Vector3(0, -0.088f, -0.14f), new Vector3(0.042f, 0.11f, 0.052f), darkPolymer).transform.localRotation = Quaternion.Euler(16f, 0, 0);
            muzzlePoints[4] = MakeEmpty(op.transform, "Muzzle4", new Vector3(0, 0.016f, 0.98f));

            // Shared Muzzle Flash (No Point Light component so Unity Gizmos never show a lightbulb over the gun!)
            muzzleFlashObj = Part(gunPivot, "MuzzleFlash", PrimitiveType.Sphere, new Vector3(0f, 0.01f, 0.76f), new Vector3(0.11f, 0.11f, 0.24f), CitadelWorldBuilder.MakeEmissiveMat(new Color(1f, 0.80f, 0.25f), 4.5f));
            muzzleFlashObj.SetActive(false);
            muzzleLight = null;

            // 3D Melee Group (Reaver Karambit / Oni Katana / Ripper Chainsword / Warhammer)
            GameObject meleeGo = new GameObject("MeleeViewGroup");
            meleeGo.transform.SetParent(weaponRig, false);
            meleeGroup = meleeGo.transform;
            meleeBladeMat = CitadelWorldBuilder.MakeEmissiveMat(new Color(0.72f, 0.25f, 1f), 1.6f);
            Part(meleeGroup, "MeleeRing", PrimitiveType.Cylinder, new Vector3(0, -0.07f, 0), new Vector3(0.042f, 0.012f, 0.042f), obsidianMetal).transform.localRotation = Quaternion.Euler(0, 0, 90f);
            Part(meleeGroup, "MeleeHilt", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.030f, 0.075f, 0.030f), darkPolymer);
            Part(meleeGroup, "MeleeBlade", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.03f), new Vector3(0.014f, 0.50f, 0.058f), meleeBladeMat).transform.localRotation = Quaternion.Euler(14f, 0, 0);
            meleeGroup.gameObject.SetActive(false);

            SetActiveWeaponModel(0);
        }

        private GameObject Part(Transform parent, string n, PrimitiveType pt, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(pt);
            go.name = n;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private Transform MakeEmpty(Transform parent, string n, Vector3 localPos)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        public void SetActiveWeaponModel(int weaponId)
        {
            activeWeaponId = Mathf.Clamp(weaponId, 0, 4);
            for (int i = 0; i < weaponModels.Length; i++)
            {
                if (weaponModels[i] != null) weaponModels[i].SetActive(i == activeWeaponId);
            }
            if (muzzleFlashObj != null && muzzlePoints[activeWeaponId] != null)
            {
                muzzleFlashObj.transform.localPosition = muzzlePoints[activeWeaponId].localPosition;
            }
            equipTimer = 0.28f;
            inspectTimer = 0f;
            boltCycleTimer = 0f;
        }

        public void TriggerInspect()
        {
            inspectTimer = 2.1f;
        }

        public Vector3 GetMuzzleWorldPosition()
        {
            if (muzzlePoints != null && activeWeaponId < muzzlePoints.Length && muzzlePoints[activeWeaponId] != null)
                return muzzlePoints[activeWeaponId].position;
            return transform.position + transform.forward * 0.7f;
        }

        public void TriggerRecoil(WeaponDef w)
        {
            recoilOffset = w.recoilKick;
            recoilPitch = w.recoilKick * 52f;
            muzzleTimer = w.isSuppressed ? 0.03f : 0.06f;
            inspectTimer = 0f;
            if (w.isSniper) boltCycleTimer = 0.75f;
            SpawnBrassCasing();
        }

        public void TriggerMeleeSwing(Color bladeColor)
        {
            meleeSwingTimer = 0.34f;
            if (meleeBladeMat != null)
            {
                meleeBladeMat.color = bladeColor;
                if (meleeBladeMat.HasProperty("_EmissionColor"))
                    meleeBladeMat.SetColor("_EmissionColor", bladeColor * 1.8f);
            }
        }

        public void SpawnTracerBeam(Vector3 targetPoint, Color tracerColor)
        {
            GameObject go = new GameObject("ValorantTracer");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.022f;
            lr.endWidth = 0.008f;
            lr.material = CitadelWorldBuilder.MakeEmissiveMat(tracerColor, 3.5f);
            lr.SetPosition(0, GetMuzzleWorldPosition());
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
            c.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeStdMat(new Color(0.95f, 0.74f, 0.25f), 0.9f, 0.75f);
            Rigidbody rb = c.AddComponent<Rigidbody>();
            Vector3 ejectDir = transform.right * Random.Range(2.4f, 3.6f) + transform.up * Random.Range(1.4f, 2.3f);
            rb.AddForce(ejectDir, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 14f, ForceMode.Impulse);
            c.AddComponent<ShellCasing3D>();
        }

        public void UpdateViewmodel(float dt, bool isADS, bool isSniperScoped, bool reloading, float reloadProgress, float bobPhase)
        {
            if (weaponRig == null || gunPivot == null) return;

            // Hide 3D gun mesh cleanly when looking through the Operator's full-screen sniper scope!
            gunPivot.gameObject.SetActive(!isSniperScoped);

            if (muzzleTimer > 0f && !isSniperScoped)
            {
                muzzleTimer -= dt;
                if (muzzleFlashObj != null)
                {
                    muzzleFlashObj.SetActive(true);
                    muzzleFlashObj.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                }
                if (muzzleLight != null) muzzleLight.intensity = 3.2f;
            }
            else
            {
                if (muzzleFlashObj != null) muzzleFlashObj.SetActive(false);
                if (muzzleLight != null) muzzleLight.intensity = 0f;
            }

            recoilOffset = Mathf.Lerp(recoilOffset, 0f, dt * 15f);
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, dt * 15f);
            if (equipTimer > 0f) equipTimer = Mathf.Max(0f, equipTimer - dt);
            if (inspectTimer > 0f) inspectTimer = Mathf.Max(0f, inspectTimer - dt);
            if (boltCycleTimer > 0f) boltCycleTimer = Mathf.Max(0f, boltCycleTimer - dt);

            Vector3 targetPos = isADS ? adsPos : hipPos;
            Vector3 targetEuler = isADS ? adsEuler : hipEuler;
            weaponRig.localPosition = Vector3.Lerp(weaponRig.localPosition, targetPos, dt * 16f);

            // Equip Pull-Out Dip + Inspect Side-Tilt + Per-Weapon Valorant Reload Animation!
            float equipDipY = -Mathf.Sin((equipTimer / 0.28f) * Mathf.PI * 0.5f) * 0.26f;
            float equipPitch = (equipTimer / 0.28f) * 32f;

            float inspectYaw = 0f, inspectRoll = 0f;
            if (inspectTimer > 0f && !reloading && !isADS)
            {
                float ip = 1f - (inspectTimer / 2.1f);
                inspectYaw = Mathf.Sin(ip * Mathf.PI) * -42f;
                inspectRoll = Mathf.Sin(ip * Mathf.PI * 2f) * 24f;
            }

            float reloadRoll = 0f, reloadPitchAdd = 0f, reloadDropY = 0f, boltPullZ = 0f;
            if (reloading)
            {
                if (reloadProgress < 0.35f)
                {
                    reloadRoll = 30f;
                    reloadPitchAdd = 14f;
                }
                else if (reloadProgress < 0.68f)
                {
                    reloadRoll = 26f;
                    bool slapWindow = reloadProgress > 0.42f && reloadProgress < 0.54f;
                    reloadPitchAdd = slapWindow ? -10f : 10f;
                    reloadDropY = slapWindow ? 0.036f : -0.015f;
                }
                else if (reloadProgress < 0.90f)
                {
                    reloadRoll = activeWeaponId == 1 ? 14f : -18f; // Phantom paddle slap vs Vandal/Op bolt pull
                    reloadPitchAdd = 6f;
                    boltPullZ = -0.085f;
                }
            }

            Quaternion desiredRot = Quaternion.Euler(
                targetEuler.x - recoilPitch + reloadPitchAdd + equipPitch,
                targetEuler.y + inspectYaw,
                targetEuler.z + reloadRoll + inspectRoll
            );
            weaponRig.localRotation = Quaternion.Slerp(weaponRig.localRotation, desiredRot, dt * 14f);

            float swayAmp = isADS ? 0.0018f : 0.010f;
            gunPivot.localPosition = new Vector3(
                Mathf.Cos(bobPhase * 0.5f) * swayAmp,
                Mathf.Abs(Mathf.Sin(bobPhase)) * swayAmp + reloadDropY + equipDipY,
                -recoilOffset
            );

            // Animate active weapon's Magazine & Bolt (and Odin's Flip-Up Top Cover!)
            Transform mag = weaponMags[activeWeaponId];
            if (mag != null)
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

            Transform bolt = weaponBolts[activeWeaponId];
            if (bolt != null)
            {
                if (activeWeaponId == 4)
                {
                    // Operator Bolt Handle lifts up & slides back!
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
                    bolt.localPosition = new Vector3(bolt.localPosition.x, bolt.localPosition.y, 0.03f - recoilOffset * 1.4f + boltPullZ);
                }
            }

            if (odinTopCover != null)
            {
                float coverAngle = (activeWeaponId == 3 && reloading && reloadProgress > 0.08f && reloadProgress < 0.75f) ? -68f : 0f;
                odinTopCover.localRotation = Quaternion.Slerp(odinTopCover.localRotation, Quaternion.Euler(coverAngle, 0f, 0f), dt * 12f);
            }

            // 3D Melee Blade Swing ([V])
            if (meleeGroup != null)
            {
                if (meleeSwingTimer > 0f)
                {
                    meleeSwingTimer -= dt;
                    meleeGroup.gameObject.SetActive(true);
                    float p = 1f - (meleeSwingTimer / 0.34f);
                    meleeGroup.localPosition = new Vector3(-0.36f + p * 0.68f, -0.08f, 0.44f);
                    meleeGroup.localEulerAngles = new Vector3(32f, -20f, 68f - p * 136f);
                }
                else
                {
                    meleeGroup.gameObject.SetActive(false);
                }
            }
        }
    }
}
