using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    public enum GameModeType { Campaign, EndlessHorde, PurgatoryBlitz }
    public enum EnemyType { AshWretch, CinderImp, IronGorger, VoidCantor, ArchonMalakor }
    public enum PickupKind { Health, Armor, Shells, Cells }

    public enum BossAttackPattern
    {
        Fan3,
        Fan5Mortar,
        IceBlinkBurst,
        Radial8Nova,
        VoidTwinSpiral,
        GatlingStream,
        BloodSeraph7,
        StormHyperVolley,
        SolarRing10,
        MalakorApocalypse
    }

    [Serializable]
    public class WeaponDef
    {
        public int id;
        public string name;
        public string shortName;
        public string desc;
        public string ammoType; // "infinite", "shells", "cells"
        public string ammoLabel;
        public int magSize;
        public int costPerShot;
        public float baseDamage;
        public float damage;
        public float headshotMult = 4.0f;
        public float cooldown;
        public float reloadTime = 1.55f;
        public int pellets;
        public float spread;
        public float adsSpread;
        public float adsFov = 54f;
        public Color accentColor;
        public Color tracerColor;
        public bool isAuto;
        public bool isOdinSpinup;
        public bool isSniper;
        public bool isSuppressed;
        public float resonanceGain;
        public float recoilKick;
        public bool owned;
        public int buyCost;
        public int level = 1;
        public int upgradeCost;
    }

    [Serializable]
    public class MeleeDef
    {
        public int id;
        public string name;
        public string desc;
        public float damage;
        public float range;
        public float cooldown;
        public float lifesteal;
        public bool isShockwave;
        public Color bladeColor;
        public bool owned;
        public int cost;
    }

    [Serializable]
    public class SpecialAbilityDef
    {
        public string id;
        public string name;
        public string desc;
        public string tag;
        public int cost;
        public int chargesPerBuy;
        public bool isInstant;
    }

    [Serializable]
    public class BossProfile
    {
        public int bossLevel;          // 10, 20, 30, ..., 100
        public string bossName;
        public string bossTitle;
        public float baseHp;
        public float moveSpeed;
        public float attackDamage;
        public float attackCooldown;
        public float projSpeed;
        public float scale;
        public bool canBlink;
        public bool hasWings;
        public bool hasHalo;
        public bool hasShoulderCannons;
        public bool healsOnEnrage;
        public Color armorColor;
        public Color skinColor;
        public Color glowColor;
        public Color projColor;
        public BossAttackPattern pattern;
        public EnemyType summonTypeA;
        public EnemyType summonTypeB;
        public int creditBounty;
    }

    [Serializable]
    public class StratumDef
    {
        public int realmIndex;         // 0..9 (Realm I .. Realm X)
        public string name;            // e.g. "REALM I: CITADEL OF ASH"
        public Color skyColor;
        public Color fogColor;
        public float fogDensity;
        public Color sunColor;
        public Color ambientColor;
        public BossProfile boss;
    }

    public static class GameCatalog
    {
        public const int MaxCampaignLevel = 100;

        public static List<WeaponDef> CreateDefaultWeapons()
        {
            return new List<WeaponDef>
            {
                new WeaponDef
                {
                    id = 0, name = "REAVER VANDAL", shortName = "VANDAL",
                    desc = "Iconic 7.62 heavy rifle with curved banana mag, Reaver crimson-obsidian runes, and lethal 1-tap headshots (4x HS).",
                    ammoType = "infinite", ammoLabel = "[R] RELOAD // 7.62 REAVER",
                    magSize = 25, costPerShot = 1, baseDamage = 40f, damage = 40f, headshotMult = 4.0f,
                    cooldown = 0.105f, reloadTime = 1.60f, pellets = 1, spread = 0.012f, adsSpread = 0.002f, adsFov = 54f,
                    accentColor = new Color(0.95f, 0.20f, 0.25f), tracerColor = new Color(1f, 0.45f, 0.22f),
                    isAuto = true, resonanceGain = 6f, recoilKick = 0.052f, owned = true, level = 1, upgradeCost = 220
                },
                new WeaponDef
                {
                    id = 1, name = "INFERNO PHANTOM", shortName = "PHANTOM",
                    desc = "Suppressed high-RPM tactical rifle with integrated silencer, pinpoint ADS holo-sight, and rapid reload.",
                    ammoType = "infinite", ammoLabel = "[R] RELOAD // 5.56 SILENCED",
                    magSize = 30, costPerShot = 1, baseDamage = 35f, damage = 35f, headshotMult = 3.5f,
                    cooldown = 0.088f, reloadTime = 1.45f, pellets = 1, spread = 0.009f, adsSpread = 0.0015f, adsFov = 52f,
                    accentColor = new Color(0.16f, 0.88f, 1f), tracerColor = new Color(0.35f, 0.92f, 1f),
                    isAuto = true, isSuppressed = true, resonanceGain = 5f, recoilKick = 0.036f, owned = true, level = 1, upgradeCost = 240
                },
                new WeaponDef
                {
                    id = 2, name = "HELL-JUDGE SHOTGUN", shortName = "JUDGE",
                    desc = "Rapid-fire drum-fed tactical shotgun unleashing 9 brimstone pellets per blast for close-quarters devastation.",
                    ammoType = "shells", ammoLabel = "[R] RELOAD // 12G DRUM",
                    magSize = 7, costPerShot = 1, baseDamage = 20f, damage = 20f, headshotMult = 2.0f,
                    cooldown = 0.34f, reloadTime = 1.75f, pellets = 9, spread = 0.072f, adsSpread = 0.042f, adsFov = 58f,
                    accentColor = new Color(1f, 0.48f, 0.12f), tracerColor = new Color(1f, 0.62f, 0.18f),
                    isAuto = true, resonanceGain = 11f, recoilKick = 0.095f, owned = true, level = 1, upgradeCost = 260
                },
                new WeaponDef
                {
                    id = 3, name = "SOUL-SHREDDER ODIN", shortName = "ODIN",
                    desc = "100-round belt-fed heavy LMG with Valorant spin-up RPM ramp, visible brass ammo belt, and overwhelming wall-of-fire.",
                    ammoType = "cells", ammoLabel = "[R] RELOAD // 100-RD BELT",
                    magSize = 100, costPerShot = 1, baseDamage = 32f, damage = 32f, headshotMult = 2.5f,
                    cooldown = 0.095f, reloadTime = 2.25f, pellets = 1, spread = 0.024f, adsSpread = 0.008f, adsFov = 56f,
                    accentColor = new Color(1f, 0.78f, 0.20f), tracerColor = new Color(1f, 0.85f, 0.30f),
                    isAuto = true, isOdinSpinup = true, resonanceGain = 4f, recoilKick = 0.048f, owned = true, level = 1, upgradeCost = 300
                },
                new WeaponDef
                {
                    id = 4, name = "REAVER OPERATOR", shortName = "OPERATOR",
                    desc = "Bolt-action .50 anti-materiel rail sniper with full-screen optic scope (20 deg FOV) and earth-shattering 1-shot eliminations.",
                    ammoType = "shells", ammoLabel = "[R] RELOAD // .50 RAIL AP",
                    magSize = 5, costPerShot = 1, baseDamage = 180f, damage = 180f, headshotMult = 3.0f,
                    cooldown = 1.12f, reloadTime = 1.95f, pellets = 1, spread = 0.025f, adsSpread = 0.0001f, adsFov = 20f,
                    accentColor = new Color(0.72f, 0.26f, 1f), tracerColor = new Color(0.82f, 0.42f, 1f),
                    isAuto = false, isSniper = true, resonanceGain = 22f, recoilKick = 0.145f, owned = true, buyCost = 350, level = 1, upgradeCost = 340
                }
            };
        }

        public static List<MeleeDef> CreateDefaultMelees()
        {
            return new List<MeleeDef>
            {
                new MeleeDef
                {
                    id = 0, name = "REAVER KARAMBIT",
                    desc = "Curved obsidian claw-blade with demonic mist edge. Ultra-fast Valorant karambit ring-slash ([V]).",
                    damage = 75f, range = 3.4f, cooldown = 0.38f, bladeColor = new Color(0.72f, 0.25f, 1f),
                    owned = true, cost = 0
                },
                new MeleeDef
                {
                    id = 1, name = "ONI BLOOD-KATANA",
                    desc = "Superheated demonic katana that cleaves multiple hostiles in a wide 4.6m crimson energy arc ([V]).",
                    damage = 145f, range = 4.6f, cooldown = 0.46f, bladeColor = new Color(1f, 0.16f, 0.12f),
                    owned = false, cost = 250
                },
                new MeleeDef
                {
                    id = 2, name = "RIPPER CHAINSWORD",
                    desc = "Motorized demonic chainsaw blade. Shreds armor and leeches +20 Vitality HP per hit ([V]).",
                    damage = 195f, range = 4.0f, cooldown = 0.52f, lifesteal = 20f, bladeColor = new Color(1f, 0.67f, 0.13f),
                    owned = false, cost = 400
                },
                new MeleeDef
                {
                    id = 3, name = "HELL-BREAKER WARHAMMER",
                    desc = "Colossal obsidian maul. Slams the ground to unleash a 7.5m 360-degree seismic shockwave ([V]).",
                    damage = 270f, range = 7.5f, cooldown = 0.72f, isShockwave = true, bladeColor = new Color(1f, 0.82f, 0.30f),
                    owned = false, cost = 550
                }
            };
        }

        public static List<SpecialAbilityDef> CreateDefaultAbilities()
        {
            return new List<SpecialAbilityDef>
            {
                new SpecialAbilityDef
                {
                    id = "nova", name = "HELLFIRE ORBITAL NOVA", tag = "OFFENSIVE BARRAGE",
                    desc = "Press [X] to launch a 360-degree ring of 12 explosive brimstone missiles around you.",
                    cost = 200, chargesPerBuy = 2
                },
                new SpecialAbilityDef
                {
                    id = "aegis", name = "VAMPIRIC BLOOD-AEGIS", tag = "DEFENSIVE / LIFESTEAL",
                    desc = "Press [X] for 6 seconds of 100% Invulnerability Shield + all bullets leech +10 HP per hit.",
                    cost = 300, chargesPerBuy = 2
                },
                new SpecialAbilityDef
                {
                    id = "meteor", name = "BRIMSTONE METEOR STRIKE", tag = "APOCALYPTIC AIRSTRIKE",
                    desc = "Press [X] to call down apocalyptic molten meteors from the Citadel sky onto active demons.",
                    cost = 420, chargesPerBuy = 2
                },
                new SpecialAbilityDef
                {
                    id = "resupply", name = "FULL TACTICAL RESUPPLY CRATE", tag = "INSTANT SUPPLY DROP",
                    desc = "Instantly restores 100 HP, 100 Aegis Armor, max Slag Shells, max Plasma Cells, and full mags.",
                    cost = 150, isInstant = true
                }
            };
        }

        /// <summary>
        /// 10 Distinct Infernal Realms spanning Levels 1-100, each culminating at Level 10, 20, 30, ..., 100
        /// with a completely unique 3D Final Boss (unique silhouette, wings/halos/cannons, colors, attack pattern, and summons).
        /// </summary>
        public static List<StratumDef> CreateTenRealms()
        {
            return new List<StratumDef>
            {
                // REALM I: LEVELS 1 - 10
                new StratumDef
                {
                    realmIndex = 0, name = "REALM I: CITADEL OF ASH (LVL 1-10)",
                    skyColor = new Color(0.68f, 0.58f, 0.42f), fogColor = new Color(0.44f, 0.36f, 0.27f), fogDensity = 0.015f,
                    sunColor = new Color(1.0f, 0.88f, 0.68f), ambientColor = new Color(0.42f, 0.35f, 0.26f),
                    boss = new BossProfile
                    {
                        bossLevel = 10, bossName = "PYROCLAST", bossTitle = "WARDEN OF ASH",
                        baseHp = 1100f, moveSpeed = 4.6f, attackDamage = 18f, attackCooldown = 1.55f, projSpeed = 22f,
                        scale = 2.0f, hasShoulderCannons = true,
                        armorColor = new Color(0.22f, 0.14f, 0.10f), skinColor = new Color(0.75f, 0.25f, 0.12f),
                        glowColor = new Color(1f, 0.58f, 0.12f), projColor = new Color(1f, 0.48f, 0.10f),
                        pattern = BossAttackPattern.Fan3, summonTypeA = EnemyType.AshWretch, summonTypeB = EnemyType.CinderImp,
                        creditBounty = 600
                    }
                },
                // REALM II: LEVELS 11 - 20
                new StratumDef
                {
                    realmIndex = 1, name = "REALM II: MOLTEN BASILICA (LVL 11-20)",
                    skyColor = new Color(0.58f, 0.20f, 0.11f), fogColor = new Color(0.34f, 0.12f, 0.07f), fogDensity = 0.016f,
                    sunColor = new Color(1.0f, 0.46f, 0.20f), ambientColor = new Color(0.38f, 0.16f, 0.10f),
                    boss = new BossProfile
                    {
                        bossLevel = 20, bossName = "IGNIS REX", bossTitle = "THE FORGE TYRANT",
                        baseHp = 1800f, moveSpeed = 4.2f, attackDamage = 21f, attackCooldown = 1.50f, projSpeed = 23f,
                        scale = 2.2f, hasShoulderCannons = true, hasHalo = false,
                        armorColor = new Color(0.35f, 0.24f, 0.12f), skinColor = new Color(0.88f, 0.18f, 0.08f),
                        glowColor = new Color(1f, 0.82f, 0.22f), projColor = new Color(1f, 0.32f, 0.08f),
                        pattern = BossAttackPattern.Fan5Mortar, summonTypeA = EnemyType.IronGorger, summonTypeB = EnemyType.CinderImp,
                        creditBounty = 850
                    }
                },
                // REALM III: LEVELS 21 - 30
                new StratumDef
                {
                    realmIndex = 2, name = "REALM III: FROZEN ABYSS (LVL 21-30)",
                    skyColor = new Color(0.14f, 0.34f, 0.46f), fogColor = new Color(0.08f, 0.20f, 0.30f), fogDensity = 0.016f,
                    sunColor = new Color(0.65f, 0.92f, 1.0f), ambientColor = new Color(0.18f, 0.34f, 0.44f),
                    boss = new BossProfile
                    {
                        bossLevel = 30, bossName = "GLACIERON", bossTitle = "FROST-WRAITH SOVEREIGN",
                        baseHp = 2500f, moveSpeed = 5.6f, attackDamage = 22f, attackCooldown = 1.35f, projSpeed = 28f,
                        scale = 2.15f, canBlink = true, hasHalo = true,
                        armorColor = new Color(0.12f, 0.24f, 0.34f), skinColor = new Color(0.28f, 0.68f, 0.88f),
                        glowColor = new Color(0.45f, 0.96f, 1f), projColor = new Color(0.30f, 0.92f, 1f),
                        pattern = BossAttackPattern.IceBlinkBurst, summonTypeA = EnemyType.VoidCantor, summonTypeB = EnemyType.AshWretch,
                        creditBounty = 1100
                    }
                },
                // REALM IV: LEVELS 31 - 40
                new StratumDef
                {
                    realmIndex = 3, name = "REALM IV: PLAGUE CATACOMBS (LVL 31-40)",
                    skyColor = new Color(0.16f, 0.38f, 0.18f), fogColor = new Color(0.09f, 0.22f, 0.10f), fogDensity = 0.017f,
                    sunColor = new Color(0.68f, 1.0f, 0.42f), ambientColor = new Color(0.18f, 0.34f, 0.16f),
                    boss = new BossProfile
                    {
                        bossLevel = 40, bossName = "PESTILOR", bossTitle = "ARCH-DEFILER OF BILE",
                        baseHp = 3300f, moveSpeed = 4.7f, attackDamage = 24f, attackCooldown = 1.40f, projSpeed = 24f,
                        scale = 2.3f, hasShoulderCannons = true,
                        armorColor = new Color(0.14f, 0.22f, 0.10f), skinColor = new Color(0.24f, 0.62f, 0.18f),
                        glowColor = new Color(0.55f, 1f, 0.18f), projColor = new Color(0.45f, 1f, 0.14f),
                        pattern = BossAttackPattern.Radial8Nova, summonTypeA = EnemyType.CinderImp, summonTypeB = EnemyType.IronGorger,
                        creditBounty = 1350
                    }
                },
                // REALM V: LEVELS 41 - 50
                new StratumDef
                {
                    realmIndex = 4, name = "REALM V: OBSIDIAN SANCTUM (LVL 41-50)",
                    skyColor = new Color(0.26f, 0.09f, 0.38f), fogColor = new Color(0.14f, 0.05f, 0.24f), fogDensity = 0.015f,
                    sunColor = new Color(0.78f, 0.45f, 1.0f), ambientColor = new Color(0.26f, 0.12f, 0.36f),
                    boss = new BossProfile
                    {
                        bossLevel = 50, bossName = "UMBRATHOR", bossTitle = "EMPEROR OF THE VOID",
                        baseHp = 4400f, moveSpeed = 5.4f, attackDamage = 26f, attackCooldown = 1.25f, projSpeed = 27f,
                        scale = 2.4f, canBlink = true, hasWings = true, hasHalo = true,
                        armorColor = new Color(0.08f, 0.04f, 0.14f), skinColor = new Color(0.42f, 0.12f, 0.72f),
                        glowColor = new Color(0.84f, 0.36f, 1f), projColor = new Color(0.76f, 0.28f, 1f),
                        pattern = BossAttackPattern.VoidTwinSpiral, summonTypeA = EnemyType.VoidCantor, summonTypeB = EnemyType.VoidCantor,
                        creditBounty = 1800
                    }
                },
                // REALM VI: LEVELS 51 - 60
                new StratumDef
                {
                    realmIndex = 5, name = "REALM VI: IRON SLAUGHTERHOUSE (LVL 51-60)",
                    skyColor = new Color(0.46f, 0.26f, 0.14f), fogColor = new Color(0.26f, 0.15f, 0.09f), fogDensity = 0.015f,
                    sunColor = new Color(1.0f, 0.72f, 0.36f), ambientColor = new Color(0.34f, 0.22f, 0.14f),
                    boss = new BossProfile
                    {
                        bossLevel = 60, bossName = "FERRUM TITAN", bossTitle = "COLOSSUS OF CHAINS",
                        baseHp = 5600f, moveSpeed = 3.9f, attackDamage = 25f, attackCooldown = 1.05f, projSpeed = 28f,
                        scale = 2.55f, hasShoulderCannons = true,
                        armorColor = new Color(0.32f, 0.30f, 0.28f), skinColor = new Color(0.55f, 0.24f, 0.14f),
                        glowColor = new Color(1f, 0.42f, 0.10f), projColor = new Color(1f, 0.65f, 0.15f),
                        pattern = BossAttackPattern.GatlingStream, summonTypeA = EnemyType.IronGorger, summonTypeB = EnemyType.IronGorger,
                        creditBounty = 2200
                    }
                },
                // REALM VII: LEVELS 61 - 70
                new StratumDef
                {
                    realmIndex = 6, name = "REALM VII: CRIMSON CATHEDRAL (LVL 61-70)",
                    skyColor = new Color(0.52f, 0.06f, 0.12f), fogColor = new Color(0.30f, 0.04f, 0.07f), fogDensity = 0.016f,
                    sunColor = new Color(1.0f, 0.28f, 0.32f), ambientColor = new Color(0.38f, 0.08f, 0.12f),
                    boss = new BossProfile
                    {
                        bossLevel = 70, bossName = "SANGUINUS", bossTitle = "THE BLOOD SERAPH",
                        baseHp = 6800f, moveSpeed = 5.8f, attackDamage = 28f, attackCooldown = 1.20f, projSpeed = 27f,
                        scale = 2.38f, canBlink = true, hasWings = true, hasHalo = true, healsOnEnrage = true,
                        armorColor = new Color(0.32f, 0.04f, 0.08f), skinColor = new Color(0.86f, 0.10f, 0.16f),
                        glowColor = new Color(1f, 0.85f, 0.32f), projColor = new Color(1f, 0.12f, 0.20f),
                        pattern = BossAttackPattern.BloodSeraph7, summonTypeA = EnemyType.VoidCantor, summonTypeB = EnemyType.CinderImp,
                        creditBounty = 2600
                    }
                },
                // REALM VIII: LEVELS 71 - 80
                new StratumDef
                {
                    realmIndex = 7, name = "REALM VIII: STORM OF SULFUR (LVL 71-80)",
                    skyColor = new Color(0.16f, 0.22f, 0.44f), fogColor = new Color(0.10f, 0.14f, 0.28f), fogDensity = 0.015f,
                    sunColor = new Color(0.95f, 0.92f, 0.45f), ambientColor = new Color(0.22f, 0.26f, 0.42f),
                    boss = new BossProfile
                    {
                        bossLevel = 80, bossName = "FULGURON", bossTitle = "TEMPEST ARCH-FIEND",
                        baseHp = 8200f, moveSpeed = 6.2f, attackDamage = 29f, attackCooldown = 1.05f, projSpeed = 34f,
                        scale = 2.45f, canBlink = true, hasShoulderCannons = true, hasHalo = true,
                        armorColor = new Color(0.12f, 0.18f, 0.36f), skinColor = new Color(0.24f, 0.42f, 0.86f),
                        glowColor = new Color(1f, 0.95f, 0.32f), projColor = new Color(1f, 0.94f, 0.25f),
                        pattern = BossAttackPattern.StormHyperVolley, summonTypeA = EnemyType.VoidCantor, summonTypeB = EnemyType.IronGorger,
                        creditBounty = 3200
                    }
                },
                // REALM IX: LEVELS 81 - 90
                new StratumDef
                {
                    realmIndex = 8, name = "REALM IX: ASTRAL PURGATORY (LVL 81-90)",
                    skyColor = new Color(0.44f, 0.18f, 0.36f), fogColor = new Color(0.24f, 0.10f, 0.22f), fogDensity = 0.014f,
                    sunColor = new Color(1.0f, 0.86f, 0.96f), ambientColor = new Color(0.36f, 0.22f, 0.34f),
                    boss = new BossProfile
                    {
                        bossLevel = 90, bossName = "ASTRALIS", bossTitle = "HARBINGER OF ECLIPSE",
                        baseHp = 9800f, moveSpeed = 5.5f, attackDamage = 31f, attackCooldown = 1.15f, projSpeed = 29f,
                        scale = 2.55f, canBlink = true, hasWings = true, hasHalo = true,
                        armorColor = new Color(0.84f, 0.78f, 0.64f), skinColor = new Color(0.75f, 0.22f, 0.56f),
                        glowColor = new Color(1f, 0.92f, 0.65f), projColor = new Color(1f, 0.55f, 0.92f),
                        pattern = BossAttackPattern.SolarRing10, summonTypeA = EnemyType.IronGorger, summonTypeB = EnemyType.VoidCantor,
                        creditBounty = 4000
                    }
                },
                // REALM X: LEVELS 91 - 100 (FINAL ASCENSION!)
                new StratumDef
                {
                    realmIndex = 9, name = "REALM X: THE EMPYREAN THRONE (LVL 91-100)",
                    skyColor = new Color(0.18f, 0.03f, 0.08f), fogColor = new Color(0.10f, 0.02f, 0.05f), fogDensity = 0.014f,
                    sunColor = new Color(1.0f, 0.22f, 0.18f), ambientColor = new Color(0.28f, 0.07f, 0.10f),
                    boss = new BossProfile
                    {
                        bossLevel = 100, bossName = "ARCHON MALAKOR", bossTitle = "SUPREME SOVEREIGN OF HELL",
                        baseHp = 13500f, moveSpeed = 5.6f, attackDamage = 34f, attackCooldown = 1.00f, projSpeed = 30f,
                        scale = 2.85f, canBlink = true, hasWings = true, hasHalo = true, hasShoulderCannons = true, healsOnEnrage = true,
                        armorColor = new Color(0.12f, 0.04f, 0.08f), skinColor = new Color(0.92f, 0.10f, 0.08f),
                        glowColor = new Color(1f, 0.84f, 0.28f), projColor = new Color(1f, 0.78f, 0.22f),
                        pattern = BossAttackPattern.MalakorApocalypse, summonTypeA = EnemyType.IronGorger, summonTypeB = EnemyType.VoidCantor,
                        creditBounty = 10000
                    }
                }
            };
        }

        public static readonly string[] RubricTitles =
        {
            "1. GAMEPLAY (10 PTS)",
            "2. RULES (10 PTS)",
            "3. VICTORY & LOSS CONDITIONS (10 PTS)",
            "4. INTERACTIVITY MODES (10 PTS)",
            "5. CHALLENGES (10 PTS)",
            "6. STRUCTURE: OBJECTIVES, FLOW, DURATION, AVAILABILITY, RELATIONSHIP, PROGRESSION (10 PTS)"
        };

        public static readonly string[] RubricBodies =
        {
            "Original Inverted Thermodynamic Ascension with Valorant-inspired crisp tactical gunplay across 100 escalating levels: 5 distinct 3D firearms (Reaver Vandal, Inferno Phantom, Hell-Judge, Soul-Shredder Odin, Reaver Operator), 1-tap headshots, Right-Click ADS/Scope, per-weapon mechanical reloads [R], inspect [Y], phase-dash [SHIFT], 3D melee [V], Special Abilities [X], and Chrono-Thermal Overdrive [Q/E].",
            "1) Aegis Shield Partitioning: Aegis Armor absorbs 65% of incoming damage while active. 2) Magazine & Reserve Discipline: Detachable magazines require realistic [R] reloads. 3) Kill-Chain Decay: 4.5s window builds multiplier 1.0x to 5.0x with escalating kill-streak chimes. 4) Level Gate Lock: Citadel Portal unlocks at 100% purge. 5) Boss Tier Milestones: Every 10th level (10, 20, ..., 100) seals the Realm behind a unique Final Boss.",
            "VICTORY: Purge every level from Level 1 to Level 100, defeat all 10 unique Realm Final Bosses (Pyroclast, Ignis Rex, Glacieron, Pestilor, Umbrathor, Ferrum Titan, Sanguinus, Fulguron, Astralis, and Level 100 Supreme Sovereign Archon Malakor) to achieve Total Ascension.\nLOSS: Vitality HP dropping to 0 shatters the Warden's vessel; in Purgatory Blitz, letting the clock reach 00:00.0 triggers Chrono-Collapse.",
            "System Interactivity Layers: 3D Hip-Fire, Holo ADS & Operator Scope Modes, Weapon Inspect [Y], Live 2D Top-Down Radar, Interactive Black-Market Armory Shop [B] (Upgrades MK-1..MK-5, Melees, Special Abilities), Level/Boss Tier Selector, and Built-In 6-Criteria Rubric Inspector.\nPlayable Modes: 01 100-Level Campaign Ascension, 02 Endless Abyssal Horde, 03 Purgatory Blitz.",
            "1) Multi-Archetype 3D Target Prioritization scaling from Level 1 to 100. 2) 10 Distinct Multi-Phase Final Bosses every 10 levels with unique attack patterns (Radial Novas, Blinking Frost Lances, Gatling Suppression, Blood Seraph Heal-Phase, Solar Rings, and Malakor's 3-Phase Apocalypse). 3) Crisp Recoil Control, Odin Spin-Up & Headshot Precision. 4) Soul Credit Economy & Weapon Upgrades.",
            "OBJECTIVES: Climb Levels 1 to 100 across 10 Infernal Realms and vanquish all 10 unique Final Bosses.\nFLOW: 9 escalating skirmish levels per Realm -> Realm Final Boss at Level 10/20/.../100 -> Realm Transition & Boss Bounty -> Next Realm.\nDURATION: 1-3m per level, full 100-level campaign with persistent checkpoint saves.\nAVAILABILITY: Standalone Unity 6 build + WebGL deployment.\nRELATIONSHIP: Closed-loop economy connecting kills & Boss bounties -> Armory Shop upgrades & Abilities -> Higher Realms.\nPROGRESSION: 10 unique Realm atmospheres (Ash, Molten, Frozen, Plague, Void, Iron, Crimson, Storm, Astral, Empyrean Throne) and 10 unique Final Bosses."
        };
    }
}
