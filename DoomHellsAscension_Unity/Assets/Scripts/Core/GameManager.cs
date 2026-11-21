using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Central Game State Manager, BLACK-Inspired Animated Full-Rifle Title Screen,
    /// Tactical HUD + Top-Down Radar, and Black-Market Armory Shop ([B]).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public CitadelWorldBuilder worldBuilder;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public bool ShopOpen { get; private set; }
        public bool BeaconUnlocked { get; private set; }
        public bool OverdriveActive { get; private set; }
        public float DifficultyMult { get; private set; } = 1.0f;
        public List<DemonEnemyAI> ActiveEnemies { get; } = new List<DemonEnemyAI>();

        private GameModeType currentMode = GameModeType.Campaign;
        private List<StratumDef> realms;
        private int currentLevel = 1;           // 1 .. 100
        private int selectedStartLevel = 1;     // 1 .. highestLevelUnlocked (strictly sequential!)
        private int highestLevelUnlocked = 1;   // Saved via PlayerPrefs; unlocks next level ONLY after clearing current level
        private int endlessWave = 1;
        private int totalStratumEnemies;
        private const string PrefHighestLevel = "DOOM_UnlockedCampaignLevel";
        private int score;
        private int kills;
        private float multiplier = 1.0f;
        private float peakMultiplier = 1.0f;
        private float comboTimer;
        private float elapsedTime;
        private float blitzTimer = 150f;
        private float ascensionGauge;
        private float overdriveTimer;
        private int killStreakCount;
        private float killBannerTimer;
        private string killBannerText = "";

        public int NextKillStreakCount()
        {
            return (comboTimer > 0f) ? Mathf.Min(5, killStreakCount + 1) : 1;
        }

        private string modalMode = ""; // "", "pause", "stratum", "victory", "defeat"
        private string modalTitle = "";
        private string modalSub = "";
        private int shopCategory = 0; // 0=Guns, 1=Melee, 2=Abilities
        private Vector2 shopScroll;

        private readonly List<string> killfeed = new List<string>();
        private readonly Queue<GameObject> decalQueue = new Queue<GameObject>();

        private struct MenuParticle
        {
            public float x, y, vx, vy, size, angle, spin, phase, alpha;
            public Color color;
        }

        // Procedural UI Textures & Cached GUIStyles (Zero per-frame allocations)
        private Texture2D whiteTex;
        private Texture2D blackHeroBgTex;
        private Texture2D fullRifleTex;
        private Texture2D softGlowTex;
        private Texture2D glassShardTex;
        private Texture2D bulletCraterTex;
        private MenuParticle[] menuShards;
        private MenuParticle[] menuEmbers;
        private MenuParticle[] menuTracers;
        private GUIStyle titleBrassStyle;
        private GUIStyle titleShadowStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle headerStyle;
        private GUIStyle centerHeadStyle;
        private GUIStyle cardTitleSelStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardDescStyle;
        private GUIStyle controlsCenterStyle;
        private GUIStyle bannerStyle;
        private GUIStyle panelBoxStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallMonoStyle;
        private GUIStyle hudCenterSmallStyle;
        private GUIStyle hudValueStyle;
        private GUIStyle hudAmmoRightStyle;
        private GUIStyle hudCenterBodyStyle;
        private GUIStyle hudSlotStyle;
        private GUIStyle buttonBrassStyle;
        private GUIStyle buttonDarkStyle;
        private bool stylesReady;

        private void Awake()
        {
            Instance = this;
            realms = GameCatalog.CreateTenRealms();
            highestLevelUnlocked = Mathf.Clamp(PlayerPrefs.GetInt(PrefHighestLevel, 1), 1, GameCatalog.MaxCampaignLevel);
            selectedStartLevel = highestLevelUnlocked;
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            blackHeroBgTex = GenerateBlackStyleHeroTexture();
            fullRifleTex = GenerateFullTacticalRifleTexture();
            softGlowTex = GenerateSoftGlowTexture();
            glassShardTex = GenerateGlassShardTexture();
            bulletCraterTex = GenerateBulletCraterTexture();
            InitMenuParticles();
        }

        private void Start()
        {
            if (worldBuilder == null) worldBuilder = FindFirstObjectByType<CitadelWorldBuilder>();
            if (worldBuilder != null)
            {
                worldBuilder.ApplyStratumAtmosphere(GetRealmForLevel(selectedStartLevel));
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private StratumDef GetRealmForLevel(int level)
        {
            int idx = Mathf.Clamp((level - 1) / 10, 0, realms.Count - 1);
            return realms[idx];
        }

        private void Update()
        {
            // Global Shortcuts
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleShop();
                return;
            }
            if (Input.GetKeyDown(KeyCode.M) && RealisticGunAudio.Instance != null)
            {
                RealisticGunAudio.Instance.ToggleAudio();
            }
            if (IsRunning && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
            {
                if (ShopOpen) ToggleShop();
                else if (modalMode == "pause") ResumeGame();
                else if (string.IsNullOrEmpty(modalMode)) PauseGame();
            }

            if (!IsRunning)
            {
                // Showcase camera angle overlooking the Citadel Plaza behind the BLACK landing screen
                if (Camera.main != null && FPSPlayerController.Instance != null)
                {
                    Camera.main.transform.position = new Vector3(1.5f + Mathf.Sin(Time.time * 0.35f) * 0.6f, 2.2f, 11.5f);
                    Camera.main.transform.rotation = Quaternion.Euler(4f, 188f + Mathf.Cos(Time.time * 0.25f) * 3f, 0f);
                }
                return;
            }

            if (IsPaused) return;

            // WebGL browser Pointer Lock safeguard: clicking canvas cleanly re-locks mouse aim
            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                LockCursor(true);
            }

            float dt = Time.deltaTime;
            elapsedTime += dt;

            if (currentMode == GameModeType.PurgatoryBlitz)
            {
                blitzTimer -= dt;
                if (blitzTimer <= 0f)
                {
                    blitzTimer = 0f;
                    TriggerGameOver("CHRONO-COLLAPSE: DOOMSDAY CLOCK EXPIRED");
                    return;
                }
            }

            if (comboTimer > 0f)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f)
                {
                    multiplier = 1.0f;
                    killStreakCount = 0;
                }
            }
            if (killBannerTimer > 0f) killBannerTimer -= dt;

            if (OverdriveActive)
            {
                overdriveTimer -= dt;
                if (overdriveTimer <= 0f)
                {
                    OverdriveActive = false;
                    AddKillfeed("ASCENSION OVERDRIVE DEPLETED");
                }
            }
        }

        public void StartNewGame()
        {
            // Strict sequential progression: never allow deploying beyond highest unlocked level
            selectedStartLevel = Mathf.Clamp(selectedStartLevel, 1, highestLevelUnlocked);
            currentLevel = selectedStartLevel;
            endlessWave = 1;
            score = 0;
            kills = 0;
            multiplier = 1.0f;
            peakMultiplier = 1.0f;
            comboTimer = 0f;
            elapsedTime = 0f;
            blitzTimer = 150f;
            ascensionGauge = 0f;
            OverdriveActive = false;
            overdriveTimer = 0f;
            modalMode = "";
            ShopOpen = false;

            if (FPSPlayerController.Instance != null)
            {
                FPSPlayerController.Instance.ResetForNewRun();
                // Grant earned scaling credits only when continuing from a legitimately unlocked level
                if (currentLevel > 1)
                {
                    FPSPlayerController.Instance.credits = Mathf.Max(FPSPlayerController.Instance.credits, 500 + (currentLevel - 1) * 45);
                }
            }
            LoadCampaignLevel(currentLevel);

            IsRunning = true;
            IsPaused = false;
            LockCursor(true);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.StartMusic();
        }

        private void LoadCampaignLevel(int levelNum)
        {
            ClearDynamicObjects();
            // Enforce strict lock: cannot load any level higher than highestLevelUnlocked
            currentLevel = Mathf.Clamp(levelNum, 1, highestLevelUnlocked);

            StratumDef realm = GetRealmForLevel(currentLevel);
            BeaconUnlocked = false;

            if (worldBuilder != null)
            {
                worldBuilder.ApplyStratumAtmosphere(realm);
                worldBuilder.SetPortalUnlocked(false);
            }
            if (FPSPlayerController.Instance != null) FPSPlayerController.Instance.TeleportToSpawn();

            if (currentMode == GameModeType.EndlessHorde)
            {
                SpawnEndlessWave(endlessWave);
            }
            else
            {
                SpawnLevelEncounter(currentLevel, realm);
                SpawnSupplyPickups();
            }

            totalStratumEnemies = ActiveEnemies.Count;
        }

        private void SpawnLevelEncounter(int lvl, StratumDef realm)
        {
            Vector3[] spawnRing =
            {
                new Vector3(-13, 0, -9),  new Vector3(13, 0, -9),  new Vector3(-17, 0, -20), new Vector3(17, 0, -20),
                new Vector3(0, 0, -16),   new Vector3(-9, 0, -26), new Vector3(9, 0, -26),   new Vector3(-18, 0, -7),
                new Vector3(18, 0, -7),   new Vector3(-6, 0, -14), new Vector3(6, 0, -14),   new Vector3(0, 0, -28)
            };

            bool isBossLevel = (lvl % 10 == 0);
            int stageInRealm = ((lvl - 1) % 10) + 1; // 1..10
            int realmIdx = (lvl - 1) / 10;           // 0..9

            if (isBossLevel)
            {
                // Spawn the Realm's UNIQUE Final Boss at the Citadel Colonnade!
                SpawnUniqueBoss(realm.boss, new Vector3(0f, 0f, -25f));
                // Plus escalating elite vanguard escorts
                int escortCount = Mathf.Min(4, 2 + realmIdx / 3);
                for (int i = 0; i < escortCount; i++)
                {
                    EnemyType et = (i % 2 == 0) ? realm.boss.summonTypeA : realm.boss.summonTypeB;
                    SpawnSingleEnemy(et, spawnRing[i]);
                }
                AddKillfeed($"LEVEL {lvl} FINAL BOSS: {realm.boss.bossName}, {realm.boss.bossTitle}!");
            }
            else
            {
                // Escalating 9-stage climb inside each 10-level Realm!
                int enemyCount = Mathf.Clamp(4 + stageInRealm + realmIdx / 2, 5, spawnRing.Length);
                for (int i = 0; i < enemyCount; i++)
                {
                    EnemyType et = PickEnemyForLevel(lvl, i);
                    Vector3 basePt = spawnRing[i % spawnRing.Length];
                    Vector3 jitter = new Vector3(((i * 37) % 5 - 2) * 0.6f, 0f, ((i * 53) % 5 - 2) * 0.6f);
                    SpawnSingleEnemy(et, basePt + jitter);
                }
                AddKillfeed($"LEVEL {lvl} / 100 - {realm.name} (BOSS AT LVL {((realmIdx + 1) * 10)})");
            }
        }

        private static EnemyType PickEnemyForLevel(int lvl, int slot)
        {
            if (lvl <= 3) return (slot % 3 == 2) ? EnemyType.CinderImp : EnemyType.AshWretch;
            if (lvl <= 9)
            {
                int r = slot % 4;
                if (r == 0) return EnemyType.AshWretch;
                if (r == 1 || r == 2) return EnemyType.CinderImp;
                return EnemyType.IronGorger;
            }
            // Levels 11+: Full 4-archetype mix with higher Void-Cantor & Iron-Gorger ratio in deeper realms
            int mod = (slot + lvl) % 4;
            if (mod == 0) return lvl > 40 ? EnemyType.VoidCantor : EnemyType.AshWretch;
            if (mod == 1) return EnemyType.CinderImp;
            if (mod == 2) return EnemyType.IronGorger;
            return EnemyType.VoidCantor;
        }

        public void SpawnSingleEnemy(EnemyType type, Vector3 pos)
        {
            GameObject go = new GameObject($"Demon_{type}");
            go.transform.position = pos;
            var ai = go.AddComponent<DemonEnemyAI>();
            int scaleLevel = currentMode == GameModeType.EndlessHorde ? (endlessWave * 5) : currentLevel;
            ai.Initialize(type, DifficultyMult, scaleLevel);
            ActiveEnemies.Add(ai);
            totalStratumEnemies = Mathf.Max(totalStratumEnemies, ActiveEnemies.Count);
        }

        public void SpawnUniqueBoss(BossProfile profile, Vector3 pos)
        {
            GameObject go = new GameObject($"BOSS_Lvl{profile.bossLevel}_{profile.bossName}");
            go.transform.position = pos;
            var ai = go.AddComponent<DemonEnemyAI>();
            ai.InitializeUniqueBoss(profile, DifficultyMult);
            ActiveEnemies.Add(ai);
            totalStratumEnemies = Mathf.Max(totalStratumEnemies, ActiveEnemies.Count);
        }

        private void SpawnEndlessWave(int wave)
        {
            Vector3[] spots =
            {
                new Vector3(-14, 0, -8), new Vector3(14, 0, -8), new Vector3(-16, 0, -20), new Vector3(16, 0, -20),
                new Vector3(0, 0, -16), new Vector3(-9, 0, -26), new Vector3(9, 0, -26), new Vector3(-18, 0, -14),
                new Vector3(18, 0, -14), new Vector3(0, 0, -28)
            };
            int count = Mathf.Min(spots.Length, 5 + wave * 2);
            if (wave % 3 == 0)
            {
                BossProfile bp = realms[((wave / 3) - 1) % realms.Count].boss;
                SpawnUniqueBoss(bp, new Vector3(0f, 0f, -25f));
                AddKillfeed($"HORDE BOSS WAVE {wave}: {bp.bossName}, {bp.bossTitle}!");
            }
            for (int i = 0; i < count; i++)
            {
                EnemyType t = (EnemyType)(i % (wave >= 2 ? 4 : 2));
                SpawnSingleEnemy(t, spots[i % spots.Length]);
            }
            SpawnSupplyPickups();
            totalStratumEnemies = ActiveEnemies.Count;
            AddKillfeed($"ABYSSAL HORDE WAVE {wave} MANIFESTED!");
        }

        private void SpawnSupplyPickups()
        {
            (PickupKind, Vector3, Color)[] items =
            {
                (PickupKind.Shells, new Vector3(-7, 0.8f, 2), new Color(1f, 0.55f, 0.14f)),
                (PickupKind.Health, new Vector3(7, 0.8f, -2), new Color(0.16f, 0.94f, 0.53f)),
                (PickupKind.Cells,  new Vector3(-13, 0.8f, -16), new Color(0.72f, 0.36f, 1f)),
                (PickupKind.Armor,  new Vector3(13, 0.8f, -16), new Color(0.14f, 0.85f, 1f)),
                (PickupKind.Health, new Vector3(0, 0.8f, -20), new Color(0.16f, 0.94f, 0.53f))
            };
            foreach (var it in items)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Pickup_{it.Item1}";
                Destroy(go.GetComponent<Collider>());
                go.transform.position = it.Item2;
                go.transform.localScale = Vector3.one * 0.65f;
                go.GetComponent<Renderer>().material = CitadelWorldBuilder.MakeEmissiveMat(it.Item3, 2.2f);
                var p = go.AddComponent<PickupItem3D>();
                p.kind = it.Item1;
            }
        }

        private void ClearDynamicObjects()
        {
            foreach (var e in ActiveEnemies) if (e != null) Destroy(e.gameObject);
            ActiveEnemies.Clear();
            foreach (var p in FindObjectsByType<Projectile3D>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            foreach (var pk in FindObjectsByType<PickupItem3D>(FindObjectsSortMode.None)) Destroy(pk.gameObject);
        }

        public void AddResonance(float amt)
        {
            if (OverdriveActive) return;
            ascensionGauge = Mathf.Clamp(ascensionGauge + amt, 0f, 100f);
        }

        public void TriggerAscensionOverdrive()
        {
            if (ascensionGauge < 100f || OverdriveActive) return;
            ascensionGauge = 0f;
            OverdriveActive = true;
            overdriveTimer = 8.0f;
            if (FPSPlayerController.Instance != null)
                FPSPlayerController.Instance.armor = Mathf.Min(FPSPlayerController.Instance.maxArmor, FPSPlayerController.Instance.armor + 25);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayOverdrive();
            AddKillfeed("ASCENSION OVERDRIVE ENGAGED (2x DMG // CHRONO-RIFT)!");
        }

        public void OnEnemyKilled(DemonEnemyAI enemy, bool isHeadshot)
        {
            ActiveEnemies.Remove(enemy);
            kills++;
            killStreakCount = (comboTimer > 0f) ? Mathf.Min(5, killStreakCount + 1) : 1;
            killBannerTimer = 2.4f;
            string streakTitle = killStreakCount >= 5 ? "ACE // SOVEREIGN!" :
                                 killStreakCount == 4 ? "QUADRA KILL" :
                                 killStreakCount == 3 ? "TRIPLE KILL" :
                                 killStreakCount == 2 ? "DOUBLE KILL" : "ELIMINATED";
            killBannerText = $"{(isHeadshot ? "[HEADSHOT] " : "")}{streakTitle}  (x{killStreakCount})";
            comboTimer = 4.5f;
            multiplier = Mathf.Min(5.0f, multiplier + 0.5f);
            if (multiplier > peakMultiplier) peakMultiplier = multiplier;

            int pts = Mathf.RoundToInt(enemy.scoreValue * multiplier * (isHeadshot ? 1.25f : 1f));
            score += pts;

            int bounty = enemy.customCreditBounty > 0
                ? enemy.customCreditBounty
                : Mathf.RoundToInt(enemy.scoreValue * 0.38f * (isHeadshot ? 1.35f : 1f));
            if (FPSPlayerController.Instance != null) FPSPlayerController.Instance.credits += bounty;

            if (enemy.isBoss)
            {
                killBannerText = $"BOSS SLAIN: {enemy.displayName}! (+${bounty})";
                killBannerTimer = 3.5f;
            }

            AddResonance(14f);

            if (currentMode == GameModeType.PurgatoryBlitz)
            {
                blitzTimer += 2.5f;
                AddKillfeed($"{(isHeadshot ? "HEADSHOT! " : "")}{enemy.displayName} (+${bounty} // +2.5s)");
            }
            else
            {
                AddKillfeed($"{(isHeadshot ? "HEADSHOT! " : "")}{enemy.displayName} (+{pts} PTS // +${bounty})");
            }

            if (ActiveEnemies.Count == 0)
            {
                if (currentMode == GameModeType.EndlessHorde)
                {
                    endlessWave++;
                    Invoke(nameof(SpawnNextEndlessWaveDelayed), 1.3f);
                }
                else
                {
                    BeaconUnlocked = true;
                    if (worldBuilder != null) worldBuilder.SetPortalUnlocked(true);
                    AddKillfeed("PORTAL UNSEALED! ENTER THE GOLDEN RIFT!");
                }
            }
        }

        private void SpawnNextEndlessWaveDelayed()
        {
            if (IsRunning) SpawnEndlessWave(endlessWave);
        }

        public void OnPortalEntered()
        {
            // Strict guard: player must eliminate 100% of hostiles and unseal the portal before advancing
            if (!IsRunning || IsPaused || !BeaconUnlocked || ActiveEnemies.Count > 0) return;

            IsPaused = true;
            LockCursor(false);

            int clearBonus = 100 + currentLevel * 15;
            if (FPSPlayerController.Instance != null) FPSPlayerController.Instance.credits += clearBonus;

            if (currentLevel < GameCatalog.MaxCampaignLevel)
            {
                int nextLvl = currentLevel + 1;
                // Unlock the next level ONLY after finishing the current level!
                if (nextLvl > highestLevelUnlocked)
                {
                    highestLevelUnlocked = nextLvl;
                    PlayerPrefs.SetInt(PrefHighestLevel, highestLevelUnlocked);
                    PlayerPrefs.Save();
                }
                selectedStartLevel = highestLevelUnlocked;

                StratumDef nextRealm = GetRealmForLevel(nextLvl);
                bool beatBoss = (currentLevel % 10 == 0);
                bool nextIsBoss = (nextLvl % 10 == 0);

                modalMode = "stratum";
                modalTitle = beatBoss
                    ? $"REALM {(currentLevel / 10)} CONQUERED! LEVEL {nextLvl} UNLOCKED!"
                    : $"LEVEL {currentLevel} COMPLETE! LEVEL {nextLvl} UNLOCKED (+${clearBonus})";
                modalSub = nextIsBoss
                    ? $"WARNING! LEVEL {nextLvl} FINAL BOSS AWAITS: {nextRealm.boss.bossName}, {nextRealm.boss.bossTitle}!"
                    : (beatBoss
                        ? $"Advancing into {nextRealm.name}! Prepare for Level {nextLvl}."
                        : $"Level {nextLvl} is now unlocked ({nextRealm.name}). Next Boss at Level {((nextLvl - 1) / 10 + 1) * 10}.");
            }
            else
            {
                score += 25000;
                modalMode = "victory";
                modalTitle = "LEVEL 100 COMPLETE // TOTAL ASCENSION!";
                modalSub = "You defeated all 10 Realm Final Bosses including Archon Malakor and conquered all 100 Levels!";
            }
        }

        public void TriggerGameOver(string reason)
        {
            IsPaused = true;
            LockCursor(false);
            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.StopMusic();
            modalMode = "defeat";
            modalTitle = "ASCENSION FAILED";
            modalSub = reason;
        }

        public void PauseGame()
        {
            IsPaused = true;
            modalMode = "pause";
            modalTitle = "TACTICAL COMMAND STANDBY";
            modalSub = "Press [B] to open the Armory Shop or Resume your Ascension.";
            LockCursor(false);
        }

        public void ResumeGame()
        {
            IsPaused = false;
            modalMode = "";
            ShopOpen = false;
            LockCursor(true);
        }

        public void ToggleShop()
        {
            ShopOpen = !ShopOpen;
            if (ShopOpen)
            {
                if (IsRunning) IsPaused = true;
                LockCursor(false);
            }
            else if (IsRunning && string.IsNullOrEmpty(modalMode))
            {
                IsPaused = false;
                LockCursor(true);
            }
        }

        public void AddKillfeed(string msg)
        {
            killfeed.Insert(0, msg);
            if (killfeed.Count > 4) killfeed.RemoveAt(killfeed.Count - 1);
        }

        public void SpawnBurstParticles(Vector3 pos, Color col, int count)
        {
            GameObject go = new GameObject("ImpactBurst");
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = col;
            main.gravityModifier = 1.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = CitadelWorldBuilder.MakeEmissiveMat(col, 2.5f);
            ps.Play();
        }

        public void SpawnBulletDecal(Vector3 point, Vector3 normal)
        {
            GameObject d = GameObject.CreatePrimitive(PrimitiveType.Quad);
            d.name = "BulletHole";
            Destroy(d.GetComponent<Collider>());
            d.transform.position = point + normal * 0.02f;
            d.transform.rotation = Quaternion.LookRotation(-normal);
            d.transform.localScale = Vector3.one * 0.16f;
            d.GetComponent<Renderer>().sharedMaterial = CitadelWorldBuilder.MakeUnlitMat(new Color(0.06f, 0.04f, 0.03f));
            decalQueue.Enqueue(d);
            if (decalQueue.Count > 45)
            {
                GameObject old = decalQueue.Dequeue();
                if (old != null) Destroy(old);
            }
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // =========================================================================
        // PROCEDURAL ANIMATED HERO BACKDROP & COMPLETE FULL TACTICAL RIFLE SPRITE
        // =========================================================================
        private Texture2D GenerateBlackStyleHeroTexture()
        {
            int w = 768, h = 432;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            float ox = w * 0.50f, oy = h * 0.60f;

            // 1. Deep obsidian studio backdrop + fiery hellfire-amber radial aura + atmospheric smoke texture
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - ox) * 0.85f, dy = (y - oy) * 1.15f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float glow = Mathf.Clamp01(1f - d / 290f);
                    float g2 = glow * glow;
                    float haze = Mathf.PerlinNoise(x * 0.014f, y * 0.014f) * 0.14f
                               + Mathf.PerlinNoise(x * 0.032f + 7f, y * 0.032f + 3f) * 0.06f;
                    float vig = Mathf.Clamp01(1f - d / 560f);
                    px[y * w + x] = new Color(
                        Mathf.Clamp01((0.025f + g2 * 0.42f + haze * 0.45f) * (0.45f + 0.55f * vig)),
                        Mathf.Clamp01((0.020f + g2 * 0.21f + haze * 0.32f) * (0.45f + 0.55f * vig)),
                        Mathf.Clamp01((0.024f + g2 * 0.08f + haze * 0.28f) * (0.45f + 0.55f * vig)),
                        0.96f
                    );
                }
            }

            // 2. Subtle high-velocity radial ballistic lines behind the full rifle
            System.Random rng = new System.Random(2026);
            for (int l = 0; l < 28; l++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float r0 = 36f + (float)rng.NextDouble() * 55f;
                float r1 = 220f + (float)rng.NextDouble() * 340f;
                bool gold = rng.NextDouble() > 0.52;
                Color lc = gold ? new Color(0.92f, 0.64f, 0.22f, 0.22f) : new Color(0.75f, 0.84f, 0.94f, 0.16f);
                int steps = Mathf.RoundToInt(r1 - r0);
                for (int k = 0; k < steps; k++)
                {
                    float r = r0 + k;
                    int lx = Mathf.RoundToInt(ox + Mathf.Cos(ang) * r);
                    int ly = Mathf.RoundToInt(oy + Mathf.Sin(ang) * r);
                    if (lx >= 0 && lx < w && ly >= 0 && ly < h)
                        px[ly * w + lx] = Color.Lerp(px[ly * w + lx], lc, lc.a * (1f - k / (float)steps));
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a complete, full-length high-resolution tactical demonic assault rifle sprite
        /// (Buttstock -> Buffer Tube -> Upper/Lower Receiver -> Holographic Optic -> Pistol Grip ->
        ///  Trigger Guard -> Curved 30rd Banana Magazine -> Vented M-LOK Handguard & Foregrip ->
        ///  Heavy Barrel & A-Frame Front Sight -> Ported Muzzle Brake).
        /// </summary>
        private Texture2D GenerateFullTacticalRifleTexture()
        {
            int w = 1024, h = 340;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[w * h];

            Color gunDark = new Color(0.10f, 0.11f, 0.13f, 1f);
            Color gunPolymer = new Color(0.16f, 0.17f, 0.19f, 1f);
            Color gunSteel = new Color(0.42f, 0.46f, 0.52f, 1f);
            Color gunChrome = new Color(0.78f, 0.84f, 0.92f, 1f);
            Color gunBrass = new Color(0.92f, 0.72f, 0.28f, 1f);
            Color demonicCore = new Color(1.0f, 0.34f, 0.10f, 1f);
            Color opticGlass = new Color(0.22f, 0.72f, 0.88f, 0.55f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color col = Color.clear;
                    bool hit = false;

                    // 1. RUBBER BUTTPAD & COLLAPSIBLE TACTICAL BUTTSTOCK (x = 24..216)
                    if (x >= 24 && x <= 42 && y >= 106 && y <= 218)
                    {
                        float rib = ((y % 10) < 3) ? 0.72f : 1.0f;
                        col = gunDark * rib; col.a = 1f; hit = true;
                    }
                    else if (x > 42 && x <= 184)
                    {
                        float t = (x - 42f) / 142f;
                        float topY = Mathf.Lerp(218f, 214f, t);
                        float botY = Mathf.Lerp(106f, 166f, t);
                        if (y >= botY && y <= topY)
                        {
                            // Skeletonized ergonomic stock cutout
                            bool inCutout = (x >= 68 && x <= 144 && y >= botY + 18f && y <= 178f && y <= botY + 58f);
                            if (!inCutout)
                            {
                                float shade = 0.68f + 0.45f * Mathf.Clamp01((y - botY) / Mathf.Max(1f, topY - botY));
                                bool cheekHighlight = (y > topY - 12f);
                                col = (cheekHighlight ? gunSteel * 0.72f : gunPolymer) * shade;
                                col.a = 1f; hit = true;
                            }
                        }
                    }

                    // 2. CYLINDRICAL METALLIC BUFFER TUBE (x = 162..258, y = 184..214)
                    if (!hit && x >= 162 && x <= 258 && y >= 184 && y <= 214)
                    {
                        float cy = 199f;
                        float cyl = 0.38f + 0.75f * Mathf.Clamp01(1f - Mathf.Abs(y - (cy + 5f)) / 16f);
                        col = gunSteel * cyl; col.a = 1f; hit = true;
                    }

                    // 3. UPPER & LOWER RECEIVER + PICATINNY TOP RAIL + MAGWELL (x = 252..508)
                    if (!hit && x >= 252 && x <= 508 && y >= 146 && y <= 236)
                    {
                        // Top Picatinny rail teeth
                        if (y >= 226 && y <= 236)
                        {
                            bool notch = ((x - 252) % 14) < 5 && y > 231;
                            if (!notch)
                            {
                                col = gunSteel * (y > 232 ? 1.15f : 0.72f); col.a = 1f; hit = true;
                            }
                        }
                        // Upper receiver body (y = 182..226)
                        else if (y >= 182 && y < 226)
                        {
                            float spec = 0.55f + 0.55f * Mathf.Clamp01(1f - Mathf.Abs(y - 214f) / 26f);
                            col = gunSteel * (spec * 0.78f);
                            // Chrome bolt carrier inside recessed ejection port
                            if (x >= 352 && x <= 426 && y >= 194 && y <= 214)
                            {
                                float bSpec = 0.65f + 0.45f * Mathf.Clamp01(1f - Mathf.Abs(y - 207f) / 10f);
                                col = (x == 352 || x == 426 || y == 194 || y == 214) ? gunDark : gunChrome * bSpec;
                            }
                            // Glowing Demonic Reaver energy channel along upper receiver
                            else if (x >= 274 && x <= 490 && y >= 185 && y <= 190)
                            {
                                col = Color.Lerp(demonicCore, gunBrass, (x - 274f) / 216f);
                            }
                            // Brass shell deflector & forward assist detail
                            else if (x >= 324 && x <= 344 && y >= 196 && y <= 212)
                            {
                                col = gunBrass * 0.85f;
                            }
                            col.a = 1f; hit = true;
                        }
                        // Lower receiver & beveled magwell (y = 146..182)
                        else if (x >= 262 && x <= 496 && y >= 146 && y < 182)
                        {
                            // Cut space behind magwell for trigger guard area
                            bool lowerCut = (x >= 326 && x <= 422 && y < 156);
                            if (!lowerCut)
                            {
                                float bevel = 0.78f + 0.30f * Mathf.Clamp01((y - 146f) / 36f);
                                col = gunPolymer * bevel;
                                // Takedown pins & gold fire selector
                                float dPin1 = Vector2.Distance(new Vector2(x, y), new Vector2(282, 168));
                                float dPin2 = Vector2.Distance(new Vector2(x, y), new Vector2(476, 170));
                                if (dPin1 < 5.5f || dPin2 < 5.0f) col = gunBrass;
                                // Flared magwell lip
                                if (x >= 426 && x <= 496 && y <= 152) col = gunSteel * 0.85f;
                                col.a = 1f; hit = true;
                            }
                        }
                    }

                    // 4. ERGONOMIC ANGLED PISTOL GRIP (x = 244..332, y = 58..156)
                    if (!hit && y >= 58 && y <= 156)
                    {
                        float t = (156f - y) / 98f; // 0 at top, 1 at bottom
                        float gripLeft = Mathf.Lerp(282f, 244f, t);
                        float gripRight = Mathf.Lerp(330f, 294f, t);
                        if (x >= gripLeft && x <= gripRight)
                        {
                            float u = (x - gripLeft) / Mathf.Max(1f, gripRight - gripLeft);
                            float texRib = (((x + y) % 8) < 2) ? 0.78f : 1.0f;
                            float round = 0.65f + 0.45f * Mathf.Sin(u * Mathf.PI);
                            col = (y < 66 ? gunDark : gunPolymer) * (round * texRib);
                            col.a = 1f; hit = true;
                        }
                    }

                    // 5. TRIGGER GUARD & CURVED METALLIC TRIGGER (x = 324..428, y = 114..156)
                    if (!hit && x >= 324 && x <= 428 && y >= 114 && y <= 156)
                    {
                        bool botBar = (y >= 114 && y <= 122 && x >= 324 && x <= 424);
                        bool frontStrut = (x >= 416 && x <= 426 && y >= 116 && y <= 152);
                        // Curved trigger blade
                        float trigCx = 362f + Mathf.Pow((152f - y) / 26f, 2f) * 14f;
                        bool onTrig = (y >= 126 && y <= 152 && Mathf.Abs(x - trigCx) < 4.5f);
                        if (botBar || frontStrut) { col = gunDark * 1.4f; col.a = 1f; hit = true; }
                        else if (onTrig) { col = gunBrass; col.a = 1f; hit = true; }
                    }

                    // 6. CURVED 30-ROUND TACTICAL BANANA MAGAZINE (x = 424..552, y = 26..148)
                    if (!hit && y >= 26 && y <= 148)
                    {
                        float t = (148f - y) / 122f; // 0 at magwell top, 1 at baseplate
                        float magCx = 460f + 46f * t * t;
                        float halfW = Mathf.Lerp(29f, 26f, t);
                        if (Mathf.Abs(x - magCx) <= halfW)
                        {
                            float u = (x - (magCx - halfW)) / (halfW * 2f);
                            float curveShade = 0.58f + 0.55f * Mathf.Sin(u * Mathf.PI);
                            col = gunPolymer * curveShade;
                            // Heavy floorplate at bottom
                            if (y <= 36) col = gunSteel * 0.72f;
                            // Diagonal waffle grip ribs + glowing golden ammo indicator slots
                            else if ((y % 18) < 3) col *= 0.72f;
                            else if (u > 0.66f && u < 0.80f && (y > 54 && y < 126) && ((y % 16) < 8))
                                col = Color.Lerp(gunBrass, demonicCore, 0.35f);
                            col.a = 1f; hit = true;
                        }
                    }

                    // 7. HOODED HOLOGRAPHIC / REFLEX OPTIC SIGHT + REAR BACKUP SIGHT (x = 256..436, y = 236..296)
                    if (!hit && y >= 236 && y <= 296)
                    {
                        // Rear flip-up sight
                        if (x >= 258 && x <= 286 && y <= 266)
                        {
                            col = gunSteel * 0.78f; col.a = 1f; hit = true;
                        }
                        // Tactical Reflex Optic
                        else if (x >= 308 && x <= 434)
                        {
                            bool mountBase = (y <= 252 && x >= 312 && x <= 428);
                            bool hoodTop = (y >= 282 && y <= 294 && x >= 322 && x <= 420);
                            bool hoodLeft = (x >= 316 && x <= 332 && y >= 250 && y <= 290);
                            bool hoodRight = (x >= 412 && x <= 428 && y >= 250 && y <= 290);
                            bool inWindow = (x > 332 && x < 412 && y > 252 && y < 282);
                            if (mountBase || hoodTop || hoodLeft || hoodRight)
                            {
                                float hSpec = 0.62f + 0.48f * Mathf.Clamp01((y - 236f) / 58f);
                                col = (y > 288 ? gunChrome * 0.8f : gunSteel * 0.82f) * hSpec;
                                // Gold optic accent stripe
                                if (y >= 244 && y <= 248 && x >= 324 && x <= 416) col = gunBrass;
                                col.a = 1f; hit = true;
                            }
                            else if (inWindow)
                            {
                                col = opticGlass;
                                // Glowing crimson holographic reticle dot + ring inside optic glass!
                                float rd = Vector2.Distance(new Vector2(x, y), new Vector2(372, 267));
                                if (rd < 3.2f || (rd > 7.5f && rd < 9.2f))
                                    col = new Color(1f, 0.20f, 0.14f, 0.96f);
                                hit = true;
                            }
                        }
                    }

                    // 8. VENTED TACTICAL M-LOK HANDGUARD + ANGLED FOREGRIP (x = 508..748)
                    if (!hit && x >= 508 && x <= 748)
                    {
                        // Top rail continuation
                        if (y >= 226 && y <= 236)
                        {
                            bool notch = ((x - 508) % 14) < 5 && y > 231;
                            if (!notch) { col = gunSteel * (y > 232 ? 1.12f : 0.74f); col.a = 1f; hit = true; }
                        }
                        // Main octagonal metallic handguard shroud (y = 172..226)
                        else if (y >= 172 && y < 226)
                        {
                            float spec = 0.42f + 0.68f * Mathf.Clamp01(1f - Mathf.Abs(y - 212f) / 28f);
                            // 4 M-LOK cooling vent slots showing glowing hot steel barrel inside!
                            int slotPeriod = (x - 524) % 52;
                            bool inVent = (x >= 524 && x <= 728 && slotPeriod >= 6 && slotPeriod <= 38 && y >= 193 && y <= 207);
                            if (inVent)
                            {
                                float heat = 0.55f + 0.45f * Mathf.Clamp01(1f - Mathf.Abs(y - 200f) / 7f);
                                col = Color.Lerp(gunDark, demonicCore, heat * 0.72f);
                            }
                            else
                            {
                                col = gunSteel * (spec * 0.85f);
                                // Gold-brass trim band along lower edge
                                if (y >= 176 && y <= 180) col = gunBrass * 0.92f;
                            }
                            col.a = 1f; hit = true;
                        }
                        // Sleek Tactical Angled Foregrip under handguard (x = 588..672, y = 148..172)
                        else if (x >= 588 && x <= 672 && y >= 148 && y < 172)
                        {
                            float slopeMinY = (x < 614) ? Mathf.Lerp(172f, 148f, (x - 588f) / 26f)
                                            : (x > 646) ? Mathf.Lerp(148f, 172f, (x - 646f) / 26f) : 148f;
                            if (y >= slopeMinY)
                            {
                                col = gunPolymer * (0.75f + 0.30f * ((y - 148f) / 24f));
                                col.a = 1f; hit = true;
                            }
                        }
                    }

                    // 9. HEAVY FLUTED STEEL BARREL + GAS TUBE + A-FRAME FRONT SIGHT (x = 748..922)
                    if (!hit && x >= 748 && x <= 922)
                    {
                        bool onBarrel = (y >= 189 && y <= 211);
                        bool onGasTube = (x <= 804 && y >= 214 && y <= 220);
                        // Crisp triangular A-Frame front sight tower
                        bool onAFrame = (x >= 780 && x <= 840 && y >= 209 && y <= 278
                                         && y <= 209 + (x - 774) * 2.5f
                                         && y <= 209 + (846 - x) * 1.65f);
                        bool onAFrameCut = (x >= 796 && x <= 824 && y >= 219 && y <= 252
                                            && y <= 219 + (826 - x) * 1.5f);
                        bool onBayoLug = (x >= 792 && x <= 824 && y >= 176 && y < 189);

                        if (onBarrel)
                        {
                            float cyl = 0.32f + 0.72f * Mathf.Clamp01(1f - Mathf.Abs(y - 204f) / 13f);
                            float sheen = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y - 205f) / 4.5f), 2f) * 0.30f;
                            col = new Color(
                                Mathf.Clamp01(gunChrome.r * cyl + sheen),
                                Mathf.Clamp01(gunChrome.g * cyl + sheen),
                                Mathf.Clamp01(gunChrome.b * cyl + sheen), 1f);
                            hit = true;
                        }
                        else if (onGasTube || (onAFrame && !onAFrameCut) || onBayoLug)
                        {
                            float s = 0.52f + 0.48f * Mathf.Clamp01((y - 180f) / 96f);
                            col = gunSteel * (s * 1.05f); col.a = 1f; hit = true;
                        }
                    }

                    // 10. HEAVY PORTED TACTICAL MUZZLE BRAKE / COMPENSATOR (x = 922..1002, y = 183..217)
                    if (!hit && x >= 922 && x <= 1002 && y >= 183 && y <= 217)
                    {
                        bool port1 = (x >= 936 && x <= 956 && y >= 193 && y <= 207);
                        bool port2 = (x >= 966 && x <= 986 && y >= 193 && y <= 207);
                        bool ringGroove = (x >= 926 && x <= 929);
                        if (port1 || port2 || ringGroove)
                        {
                            col = new Color(0.03f, 0.03f, 0.04f, 1f);
                        }
                        else
                        {
                            float cyl = 0.34f + 0.72f * Mathf.Clamp01(1f - Mathf.Abs(y - 205f) / 18f);
                            float sheen = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y - 206f) / 5f), 2f) * 0.28f;
                            col = new Color(
                                Mathf.Clamp01(gunChrome.r * cyl + sheen),
                                Mathf.Clamp01(gunChrome.g * cyl + sheen),
                                Mathf.Clamp01(gunChrome.b * cyl + sheen), 1f);
                            // Subtle superheated amber glint at tip
                            if (x >= 996) col = Color.Lerp(col, gunBrass, 0.45f);
                        }
                        hit = true;
                    }

                    px[y * w + x] = col;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateSoftGlowTexture()
        {
            int sz = 128;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[sz * sz];
            float half = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.3f);
                    px[y * sz + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateGlassShardTexture()
        {
            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[sz * sz];
            Vector2 v0 = new Vector2(6f, 12f);
            Vector2 v1 = new Vector2(58f, 26f);
            Vector2 v2 = new Vector2(24f, 58f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    if (PointInTriangle(new Vector2(x, y), v0, v1, v2, out float edgeDist))
                    {
                        float rim = edgeDist < 2.4f ? 0.92f : 0.24f;
                        float glint = Mathf.Clamp01(1f - (x + y) / 115f) * 0.22f;
                        px[y * sz + x] = new Color(0.88f + glint, 0.94f + glint, 1f, rim);
                    }
                    else
                    {
                        px[y * sz + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private void InitMenuParticles()
        {
            System.Random rng = new System.Random(777);

            // 32 live rotating shattered glass shards drifting slowly across the hero screen
            menuShards = new MenuParticle[32];
            for (int i = 0; i < menuShards.Length; i++)
            {
                bool goldTint = rng.NextDouble() < 0.30;
                menuShards[i] = new MenuParticle
                {
                    x = (float)rng.NextDouble() * 1280f,
                    y = (float)rng.NextDouble() * 520f,
                    vx = (float)(rng.NextDouble() * 28.0 - 14.0),
                    vy = -12f - (float)rng.NextDouble() * 24f,
                    size = 14f + (float)rng.NextDouble() * 26f,
                    angle = (float)rng.NextDouble() * 360f,
                    spin = (float)(rng.NextDouble() * 55.0 - 27.5),
                    phase = (float)rng.NextDouble() * 6.28f,
                    alpha = 0.35f + (float)rng.NextDouble() * 0.50f,
                    color = goldTint ? new Color(1f, 0.76f, 0.32f, 1f) : new Color(0.86f, 0.93f, 1f, 1f)
                };
            }

            // 52 rising fiery embers drifting diagonally across the background
            menuEmbers = new MenuParticle[52];
            for (int i = 0; i < menuEmbers.Length; i++)
            {
                bool crimson = rng.NextDouble() < 0.35;
                menuEmbers[i] = new MenuParticle
                {
                    x = (float)rng.NextDouble() * 1280f,
                    y = (float)rng.NextDouble() * 720f,
                    vx = 18f + (float)rng.NextDouble() * 34f,
                    vy = -28f - (float)rng.NextDouble() * 62f,
                    size = 3f + (float)rng.NextDouble() * 7f,
                    phase = (float)rng.NextDouble() * 6.28f,
                    alpha = 0.45f + (float)rng.NextDouble() * 0.55f,
                    color = crimson ? new Color(1f, 0.28f, 0.12f, 1f) : new Color(1f, 0.78f, 0.26f, 1f)
                };
            }

            // 8 periodic high-velocity ballistic tracer streaks
            menuTracers = new MenuParticle[8];
            for (int i = 0; i < menuTracers.Length; i++)
            {
                menuTracers[i] = new MenuParticle
                {
                    x = (float)rng.NextDouble() * 1280f,
                    y = 60f + (float)rng.NextDouble() * 360f,
                    vx = 540f + (float)rng.NextDouble() * 420f,
                    size = 110f + (float)rng.NextDouble() * 130f,
                    angle = -16f + (float)rng.NextDouble() * 20f,
                    phase = i * 1.15f,
                    alpha = 0.22f + (float)rng.NextDouble() * 0.24f,
                    color = (i % 2 == 0) ? new Color(1f, 0.80f, 0.36f, 1f) : new Color(0.84f, 0.92f, 1f, 1f)
                };
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out float minEdgeDist)
        {
            float d1 = Cross2D(p - a, b - a);
            float d2 = Cross2D(p - b, c - b);
            float d3 = Cross2D(p - c, a - c);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            if (hasNeg && hasPos) { minEdgeDist = 99f; return false; }

            float e1 = Mathf.Abs(d1) / Mathf.Max(0.001f, (b - a).magnitude);
            float e2 = Mathf.Abs(d2) / Mathf.Max(0.001f, (c - b).magnitude);
            float e3 = Mathf.Abs(d3) / Mathf.Max(0.001f, (a - c).magnitude);
            minEdgeDist = Mathf.Min(e1, Mathf.Min(e2, e3));
            return true;
        }

        private static float Cross2D(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        private Texture2D GenerateBulletCraterTexture()
        {
            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(sz * 0.5f, sz * 0.5f);
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 d = new Vector2(x, y) - c;
                    float ang = Mathf.Atan2(d.y, d.x);
                    float jag = 1f + 0.18f * Mathf.Sin(ang * 7f) + 0.10f * Mathf.Cos(ang * 13f);
                    float r = d.magnitude / (sz * 0.34f * jag);

                    if (r < 0.72f)
                        tex.SetPixel(x, y, new Color(0.02f, 0.02f, 0.02f, 0.98f));
                    else if (r < 1.05f)
                        tex.SetPixel(x, y, new Color(0.95f, 0.84f, 0.52f, 0.96f));
                    else if (r < 1.25f)
                        tex.SetPixel(x, y, new Color(0.28f, 0.20f, 0.08f, Mathf.Clamp01((1.25f - r) / 0.20f) * 0.85f));
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // COMPLETE RESOLUTION-INDEPENDENT IMGUI RENDERER (1280x720 VIRTUAL CANVAS)
        // =========================================================================
        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            titleBrassStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 84,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleBrassStyle.normal.textColor = new Color(0.91f, 0.78f, 0.45f);

            titleShadowStyle = new GUIStyle(titleBrassStyle);
            titleShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.92f);

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            subtitleStyle.normal.textColor = new Color(0.96f, 0.86f, 0.64f);

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(1f, 0.84f, 0.42f);

            centerHeadStyle = new GUIStyle(headerStyle)
            {
                fontSize = 20,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            cardTitleSelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };
            cardTitleSelStyle.normal.textColor = new Color(1f, 0.86f, 0.44f);

            cardTitleStyle = new GUIStyle(cardTitleSelStyle);
            cardTitleStyle.normal.textColor = new Color(0.86f, 0.82f, 0.74f);

            cardDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            cardDescStyle.normal.textColor = new Color(0.82f, 0.79f, 0.75f);

            controlsCenterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            controlsCenterStyle.normal.textColor = new Color(0.84f, 0.77f, 0.62f);

            bannerStyle = new GUIStyle(headerStyle)
            {
                fontSize = 16,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter
            };
            bannerStyle.normal.textColor = Color.white;

            panelBoxStyle = new GUIStyle(GUI.skin.box);
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.94f, 0.91f, 0.86f);

            smallMonoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };
            smallMonoStyle.normal.textColor = new Color(0.88f, 0.80f, 0.64f);

            hudCenterSmallStyle = new GUIStyle(smallMonoStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            hudValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };
            hudValueStyle.normal.textColor = new Color(1f, 0.84f, 0.42f);

            hudAmmoRightStyle = new GUIStyle(hudValueStyle)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleRight
            };

            hudCenterBodyStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            hudSlotStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter
            };
            hudSlotStyle.normal.textColor = new Color(0.95f, 0.92f, 0.86f);

            buttonBrassStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonBrassStyle.normal.textColor = new Color(1f, 0.90f, 0.56f);

            buttonDarkStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonDarkStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            InitStyles();

            // Resolution-independent scaling so UI fits cleanly in any Unity Game View or fullscreen window!
            float uiScale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            uiScale = Mathf.Max(0.45f, uiScale);
            float vw = Screen.width / uiScale;
            float vh = Screen.height / uiScale;

            Matrix4x4 prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(uiScale, uiScale, 1f));

            if (!IsRunning)
            {
                if (ShopOpen) DrawShopModal(vw, vh);
                else DrawBlackLandingMenu(vw, vh);
            }
            else
            {
                DrawGameplayHUD(vw, vh);
                if (ShopOpen) DrawShopModal(vw, vh);
                else if (!string.IsNullOrEmpty(modalMode)) DrawStateModal(vw, vh);
            }

            GUI.matrix = prevMatrix;
        }

        private void DrawBlackLandingMenu(float sw, float sh)
        {
            float t = Time.unscaledTime;
            Color savedCol = GUI.color;

            // 0. Animated Breathing Dark Studio Backdrop
            float bgZoom = 1.04f + Mathf.Sin(t * 0.45f) * 0.025f;
            float bgW = sw * bgZoom;
            float bgH = sh * bgZoom;
            float bgPanX = Mathf.Sin(t * 0.32f) * 18f;
            float bgPanY = Mathf.Cos(t * 0.27f) * 10f;
            GUI.DrawTexture(new Rect((sw - bgW) * 0.5f + bgPanX, (sh - bgH) * 0.5f + bgPanY, bgW, bgH), blackHeroBgTex, ScaleMode.ScaleAndCrop);

            // 2. Bottom Tactical Command Deck Dimensions (computed early so animated hero centers cleanly above it)
            float deckW = Mathf.Min(1140f, sw - 64f);
            float deckH = 224f;
            float deckX = (sw - deckW) * 0.5f;
            float deckY = sh - deckH - 20f;
            float subY = deckY - 38f;
            float titleY = subY - 82f;

            // 0B. LIVE ANIMATED VOLUMETRIC SWEEPING LIGHT BEAMS & PULSING HELLFIRE AURA
            Matrix4x4 baseMat = GUI.matrix;
            float heroMidY = (40f + titleY) * 0.5f + 6f;
            if (softGlowTex != null)
            {
                // Sweeping diagonal spotlight / smoke beams
                for (int b = 0; b < 3; b++)
                {
                    float sweepX = sw * (0.20f + b * 0.30f) + Mathf.Sin(t * (0.42f + b * 0.16f) + b * 2.1f) * 150f;
                    float beamAng = -24f + Mathf.Sin(t * (0.35f + b * 0.12f) + b) * 14f;
                    ApplyVirtualRotation(baseMat, beamAng, new Vector2(sweepX, sh * 0.32f));
                    GUI.color = (b == 1)
                        ? new Color(1.0f, 0.56f, 0.16f, 0.16f + 0.06f * Mathf.Sin(t * 1.4f))
                        : new Color(0.85f, 0.65f, 0.32f, 0.11f + 0.04f * Mathf.Cos(t * 1.1f + b));
                    GUI.DrawTexture(new Rect(sweepX - 95f, -120f, 190f, sh * 1.25f), softGlowTex);
                    GUI.matrix = baseMat;
                }

                // Pulsing fiery radial aura directly behind the complete rifle
                float auraPulse = 1.0f + Mathf.Sin(t * 2.1f) * 0.10f;
                float auraW = 780f * auraPulse;
                float auraH = 380f * auraPulse;
                float auraCx = sw * 0.50f + Mathf.Sin(t * 0.7f) * 24f;
                float auraCy = heroMidY;
                GUI.color = new Color(1.0f, 0.44f, 0.10f, 0.45f + 0.12f * Mathf.Sin(t * 2.1f));
                GUI.DrawTexture(new Rect(auraCx - auraW * 0.5f, auraCy - auraH * 0.5f, auraW, auraH), softGlowTex);
                GUI.color = new Color(1.0f, 0.80f, 0.32f, 0.26f + 0.08f * Mathf.Cos(t * 2.7f));
                GUI.DrawTexture(new Rect(auraCx - auraW * 0.32f, auraCy - auraH * 0.32f, auraW * 0.64f, auraH * 0.64f), softGlowTex);
            }

            // 0C. LIVE SUPERSONIC BALLISTIC TRACER STREAKS ZIPPING ACROSS BACKGROUND
            if (menuTracers != null)
            {
                for (int i = 0; i < menuTracers.Length; i++)
                {
                    MenuParticle tr = menuTracers[i];
                    float tx = ((tr.x + t * tr.vx) % (sw + 520f)) - 260f;
                    float ty = tr.y + Mathf.Sin(t * 0.8f + tr.phase) * 18f;
                    ApplyVirtualRotation(baseMat, tr.angle, new Vector2(tx, ty));
                    GUI.color = new Color(tr.color.r, tr.color.g, tr.color.b, tr.alpha * (0.6f + 0.4f * Mathf.Sin(t * 4f + tr.phase)));
                    GUI.DrawTexture(new Rect(tx - tr.size * 0.5f, ty - 1.2f, tr.size, 2.4f), whiteTex);
                    GUI.matrix = baseMat;
                }
            }

            // 0D. LIVE RISING & SWIRLING HELLFIRE EMBERS
            if (menuEmbers != null && softGlowTex != null)
            {
                for (int i = 0; i < menuEmbers.Length; i++)
                {
                    MenuParticle em = menuEmbers[i];
                    float ex = (em.x + t * em.vx + Mathf.Sin(t * 1.9f + em.phase) * 26f) % (sw + 40f);
                    if (ex < -20f) ex += sw + 40f;
                    float ey = (em.y + t * em.vy) % (sh + 40f);
                    if (ey < -20f) ey += sh + 40f;
                    float flicker = 0.55f + 0.45f * Mathf.Sin(t * 5.2f + em.phase);
                    float esz = em.size * (0.85f + 0.30f * flicker);
                    GUI.color = new Color(em.color.r, em.color.g, em.color.b, em.alpha * flicker);
                    GUI.DrawTexture(new Rect(ex - esz, ey - esz, esz * 2f, esz * 2f), softGlowTex);
                }
            }

            // 0E. LIVE FLOATING & ROTATING SHATTERED GLASS SHARDS (BULLET-TIME DEBRIS)
            if (menuShards != null && glassShardTex != null)
            {
                for (int i = 0; i < menuShards.Length; i++)
                {
                    MenuParticle s = menuShards[i];
                    float sx = (s.x + t * s.vx + Mathf.Sin(t * 0.9f + s.phase) * 22f) % (sw + 80f);
                    if (sx < -40f) sx += sw + 80f;
                    float sy = (s.y + t * s.vy) % (deckY + 40f);
                    if (sy < -40f) sy += deckY + 40f;
                    float rot = s.angle + t * s.spin;
                    float glint = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 2.2f + s.phase));
                    ApplyVirtualRotation(baseMat, rot, new Vector2(sx, sy));
                    GUI.color = new Color(s.color.r, s.color.g, s.color.b, s.alpha * glint);
                    GUI.DrawTexture(new Rect(sx - s.size * 0.5f, sy - s.size * 0.5f, s.size, s.size), glassShardTex);
                    GUI.matrix = baseMat;
                }
            }

            // 0F. COMPLETE, FULL-LENGTH ANIMATED TACTICAL ASSAULT RIFLE HERO (Stock -> Receiver -> Mag -> Optic -> Barrel -> Muzzle!)
            if (fullRifleTex != null)
            {
                float rifleW = Mathf.Min(820f, sw * 0.68f);
                float rifleH = rifleW * (340f / 1024f);
                float floatY = Mathf.Sin(t * 1.55f) * 7.5f;
                float floatX = Mathf.Cos(t * 0.85f) * 6.0f;
                float tiltDeg = -5.5f + Mathf.Sin(t * 1.05f) * 2.2f;
                Vector2 riflePivot = new Vector2(sw * 0.50f + floatX, heroMidY + floatY);
                Rect rifleRect = new Rect(riflePivot.x - rifleW * 0.5f, riflePivot.y - rifleH * 0.5f, rifleW, rifleH);

                // Soft grounded shadow behind the floating full rifle
                ApplyVirtualRotation(baseMat, tiltDeg, riflePivot);
                GUI.color = new Color(0f, 0f, 0f, 0.56f);
                GUI.DrawTexture(new Rect(rifleRect.x + 10f, rifleRect.y + 14f, rifleRect.width, rifleRect.height), fullRifleTex);

                // Crisp full metallic rifle pass
                GUI.color = Color.white;
                GUI.DrawTexture(rifleRect, fullRifleTex);

                // Animated muzzle heat-glint pulse at the compensator tip
                if (softGlowTex != null)
                {
                    float mPulse = 0.28f + 0.25f * Mathf.Sin(t * 4.2f);
                    GUI.color = new Color(1f, 0.62f, 0.18f, mPulse);
                    GUI.DrawTexture(new Rect(rifleRect.xMax - 54f, rifleRect.y + rifleRect.height * 0.28f, 64f, 64f), softGlowTex);
                }
                GUI.matrix = baseMat;
            }
            GUI.color = savedCol;

            // 1. Top Telemetry Strip (Always fully visible, never clipped!)
            DrawRect(new Rect(0, 0, sw, 40), new Color(0.04f, 0.03f, 0.03f, 0.90f));
            DrawRect(new Rect(0, 39, sw, 1), new Color(0.84f, 0.70f, 0.36f, 0.35f));
            GUI.Label(new Rect(20, 9, sw - 210, 24), "DOOM: HELL'S ASCENSION  //  UNITY 6 DEMONIC BLACK-OPS FPS  //  MARKDHANIEL G. AGCAOILI", smallMonoStyle);
            if (GUI.Button(new Rect(sw - 170, 6, 154, 28), "ARMORY SHOP [B]", buttonBrassStyle)) ToggleShop();

            // 3. Colossal Brushed-Brass DOOM Title & Subtitle positioned cleanly ABOVE the Command Deck!
            GUI.Label(new Rect(4, titleY + 5, sw, 90), "D  O  O  M", titleShadowStyle);
            GUI.Label(new Rect(0, titleY, sw, 90), "D  O  O  M", titleBrassStyle);

            // Organic circular jagged bullet-hole crater nicking the lower rim of the brass title
            if (bulletCraterTex != null)
            {
                GUI.DrawTexture(new Rect(sw * 0.5f - 110f, titleY + 42f, 34f, 34f), bulletCraterTex);
            }
            GUI.Label(new Rect(0, subY, sw, 30), "———  H E L L ' S   A S C E N S I O N ™  ———", subtitleStyle);

            // 4. Sleek Tactical Command Deck (Zero overlaps at every window size!)
            Rect deckRect = new Rect(deckX, deckY, deckW, deckH);
            DrawRect(deckRect, new Color(0.05f, 0.04f, 0.04f, 0.92f));
            DrawRect(new Rect(deckX, deckY, deckW, 2), new Color(0.86f, 0.71f, 0.36f, 0.65f));

            // Row 1: 3 Equal Mode Cards
            float pad = 16f;
            float cardW = (deckW - pad * 4f) / 3f;
            float cardH = 78f;
            selectedStartLevel = Mathf.Clamp(selectedStartLevel, 1, highestLevelUnlocked);
            StratumDef previewRealm = GetRealmForLevel(selectedStartLevel);
            string campDesc = selectedStartLevel < GameCatalog.MaxCampaignLevel
                ? $"Clear Level {selectedStartLevel} to unlock Level {selectedStartLevel + 1} · Boss: {previewRealm.boss.bossName} (Lvl {previewRealm.boss.bossLevel})"
                : $"FINAL SHOWDOWN: Defeat {previewRealm.boss.bossName} (Level 100 Final Boss)!";
            DrawModeCard(new Rect(deckX + pad, deckY + 14f, cardW, cardH), GameModeType.Campaign,
                $"OP // 01  CAMPAIGN (LVL {selectedStartLevel} / UNLOCKED {highestLevelUnlocked})",
                campDesc);
            DrawModeCard(new Rect(deckX + pad * 2f + cardW, deckY + 14f, cardW, cardH), GameModeType.EndlessHorde,
                "OP // 02  ENDLESS HORDE",
                "Infinite Escalating Waves · Unique Realm Boss every 3rd Wave");
            DrawModeCard(new Rect(deckX + pad * 3f + cardW * 2f, deckY + 14f, cardW, cardH), GameModeType.PurgatoryBlitz,
                "OP // 03  PURGATORY BLITZ",
                "150s Doomsday Clock (+2.5s / Kill) across unlocked levels");

            // Row 2: Threat + Strictly Locked Level Selector on Left | Launch & Shop Buttons on Right (Zero Overlap!)
            float rowY = deckY + 106f;
            GUI.Label(new Rect(deckX + pad, rowY + 8f, 62f, 24f), "THREAT:", smallMonoStyle);
            if (GUI.Button(new Rect(deckX + 78f, rowY + 2f, 74f, 34f), DifficultyMult < 0.9f ? "[INIT]" : "INIT", buttonDarkStyle)) DifficultyMult = 0.75f;
            if (GUI.Button(new Rect(deckX + 156f, rowY + 2f, 82f, 34f), (DifficultyMult >= 0.9f && DifficultyMult <= 1.1f) ? "[WARDEN]" : "WARDEN", buttonDarkStyle)) DifficultyMult = 1.0f;
            if (GUI.Button(new Rect(deckX + 242f, rowY + 2f, 82f, 34f), DifficultyMult > 1.1f ? "[NGHTM]" : "NGHTM", buttonDarkStyle)) DifficultyMult = 1.35f;

            // Strictly Sequential Level Selector: ONLY unlocked levels (1 .. highestLevelUnlocked) can be selected!
            float lvlX = deckX + 338f;
            GUI.enabled = selectedStartLevel > 1;
            if (GUI.Button(new Rect(lvlX, rowY + 2f, 30f, 34f), "<", buttonDarkStyle))
            {
                selectedStartLevel = Mathf.Max(1, selectedStartLevel - 1);
                if (worldBuilder != null) worldBuilder.ApplyStratumAtmosphere(GetRealmForLevel(selectedStartLevel));
            }
            GUI.enabled = true;

            DrawRect(new Rect(lvlX + 34f, rowY + 2f, 92f, 34f), new Color(0.14f, 0.11f, 0.08f, 0.95f));
            GUI.Label(new Rect(lvlX + 34f, rowY + 6f, 92f, 26f),
                selectedStartLevel % 10 == 0 ? $"BOSS {selectedStartLevel}" : $"LVL {selectedStartLevel}/100",
                controlsCenterStyle);

            GUI.enabled = selectedStartLevel < highestLevelUnlocked;
            if (GUI.Button(new Rect(lvlX + 130f, rowY + 2f, 30f, 34f), ">", buttonDarkStyle))
            {
                selectedStartLevel = Mathf.Min(highestLevelUnlocked, selectedStartLevel + 1);
                if (worldBuilder != null) worldBuilder.ApplyStratumAtmosphere(GetRealmForLevel(selectedStartLevel));
            }
            GUI.enabled = true;

            if (highestLevelUnlocked <= 1)
            {
                DrawRect(new Rect(lvlX + 166f, rowY + 2f, 114f, 34f), new Color(0.12f, 0.08f, 0.08f, 0.92f));
                GUI.Label(new Rect(lvlX + 166f, rowY + 6f, 114f, 26f), "LVL 2 LOCKED", controlsCenterStyle);
            }
            else
            {
                if (GUI.Button(new Rect(lvlX + 166f, rowY + 2f, 114f, 34f), "RESET LVL 1", buttonDarkStyle))
                {
                    highestLevelUnlocked = 1;
                    selectedStartLevel = 1;
                    PlayerPrefs.SetInt(PrefHighestLevel, 1);
                    PlayerPrefs.Save();
                    if (worldBuilder != null) worldBuilder.ApplyStratumAtmosphere(GetRealmForLevel(1));
                }
            }

            string deployText = currentMode == GameModeType.EndlessHorde
                ? "▶ DEPLOY HORDE WAVE 1 (PLAY)"
                : $"▶ DEPLOY LEVEL {selectedStartLevel} (PLAY)";
            if (GUI.Button(new Rect(deckRect.xMax - 498f, rowY, 304f, 38f), deployText, buttonBrassStyle))
                StartNewGame();
            if (GUI.Button(new Rect(deckRect.xMax - 186f, rowY, 170f, 38f), "🛒 ARMORY SHOP [B]", buttonDarkStyle))
                ToggleShop();

            // Row 3: Clean 2-Line Centered Controls Strip with full vertical room
            DrawRect(new Rect(deckX + pad, deckY + 158f, deckW - pad * 2f, 52f), new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(new Rect(deckX + pad + 8f, deckY + 162f, deckW - pad * 2f - 16f, 44f),
                "[WASD] Move  ·  [MOUSE] 3D Look  ·  [L-CLICK] Fire  ·  [R-CLICK] ADS / Operator Scope  ·  [1-5] Valorant Weapons\n[R] Tactical Reload  ·  [Y] Inspect  ·  [V] 3D Melee  ·  [X] Special Ability  ·  [SHIFT] Phase-Dash  ·  [Q/E] Overdrive  ·  [B] Shop",
                controlsCenterStyle);
        }

        private void DrawModeCard(Rect r, GameModeType mode, string title, string desc)
        {
            bool sel = currentMode == mode;
            DrawRect(r, sel ? new Color(0.42f, 0.26f, 0.08f, 0.72f) : new Color(0.11f, 0.10f, 0.12f, 0.85f));
            if (sel) DrawRect(new Rect(r.x, r.y, r.width, 2f), new Color(1f, 0.82f, 0.32f, 0.95f));
            if (GUI.Button(r, "", GUIStyle.none)) currentMode = mode;
            GUI.Label(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, 22f), title, sel ? cardTitleSelStyle : cardTitleStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 34f, r.width - 24f, 38f), desc, cardDescStyle);
        }

        private void DrawGameplayHUD(float sw, float sh)
        {
            var p = FPSPlayerController.Instance;
            if (p == null) return;

            // Screen-Space Flashes
            if (p.DamageFlashTimer > 0f) DrawRect(new Rect(0, 0, sw, sh), new Color(1f, 0.08f, 0f, p.DamageFlashTimer * 1.1f));
            if (OverdriveActive) DrawRect(new Rect(0, 0, sw, sh), new Color(0.65f, 0.25f, 1f, 0.12f));
            if (p.PickupFlashTimer > 0f) DrawRect(new Rect(0, 0, sw, sh), new Color(0.14f, 0.85f, 1f, p.PickupFlashTimer * 0.7f));
            if (p.DashFlashTimer > 0f) DrawRect(new Rect(0, 0, sw, sh), new Color(1f, 1f, 1f, p.DashFlashTimer * 0.6f));

            // VALORANT Operator Full-Screen Sniper Scope Overlay (Right-Click Zoom with Operator!)
            if (!IsPaused && p.IsSniperScoped)
            {
                float cx = sw * 0.5f, cy = sh * 0.5f;
                float scopeR = Mathf.Min(sw, sh) * 0.36f;
                Color blk = new Color(0.02f, 0.01f, 0.03f, 0.96f);
                DrawRect(new Rect(0, 0, cx - scopeR, sh), blk);
                DrawRect(new Rect(cx + scopeR, 0, sw - (cx + scopeR), sh), blk);
                DrawRect(new Rect(cx - scopeR, 0, scopeR * 2f, cy - scopeR), blk);
                DrawRect(new Rect(cx - scopeR, cy + scopeR, scopeR * 2f, sh - (cy + scopeR)), blk);
                // Razor-thin sniper crosshairs + Reaver violet center dot
                DrawRect(new Rect(cx - scopeR, cy - 1, scopeR * 2f, 2), new Color(0.1f, 0.05f, 0.12f, 0.85f));
                DrawRect(new Rect(cx - 1, cy - scopeR, 2, scopeR * 2f), new Color(0.1f, 0.05f, 0.12f, 0.85f));
                DrawRect(new Rect(cx - 3, cy - 3, 6, 6), new Color(0.82f, 0.32f, 1f, 1f));
            }

            // Center Dynamic Crosshair & Hitmarker (Hidden during ADS Iron Sights / Sniper Scope)
            if (!IsPaused && !p.IsADS)
            {
                float cx = sw * 0.5f, cy = sh * 0.5f;
                Color chCol = new Color(0.18f, 1f, 0.78f, 0.95f); // Crisp Valorant cyan-green crosshair!
                DrawRect(new Rect(cx - 1, cy - 10, 2, 6), chCol);
                DrawRect(new Rect(cx - 1, cy + 4, 2, 6), chCol);
                DrawRect(new Rect(cx - 10, cy - 1, 6, 2), chCol);
                DrawRect(new Rect(cx + 4, cy - 1, 6, 2), chCol);
            }
            if (p.HitmarkerTimer > 0f)
            {
                float cx = sw * 0.5f, cy = sh * 0.5f;
                Color hmCol = p.LastHitWasHeadshot ? Color.yellow : Color.red;
                DrawRect(new Rect(cx - 9, cy - 9, 18, 2), hmCol);
                DrawRect(new Rect(cx - 9, cy + 7, 18, 2), hmCol);
            }
            // VALORANT-Style Multi-Kill Banner Emblem above bottom center HUD!
            if (killBannerTimer > 0f && !IsPaused)
            {
                float bw = 380f;
                Rect br = new Rect(sw * 0.5f - bw * 0.5f, sh - 174f, bw, 38f);
                Color bannerCol = killStreakCount >= 5 ? new Color(0.55f, 0.10f, 0.78f, 0.92f) : new Color(0.68f, 0.10f, 0.14f, 0.88f);
                DrawRect(br, bannerCol);
                GUI.Label(br, killBannerText, bannerStyle);
            }
            if (p.IsReloading)
            {
                Rect reloadRect = new Rect(sw * 0.5f - 115f, sh * 0.5f + 26f, 230f, 28f);
                DrawRect(reloadRect, new Color(0.25f, 0.05f, 0.04f, 0.88f));
                GUI.Label(reloadRect, "RELOADING ORDNANCE...", hudCenterSmallStyle);
            }
            if (BeaconUnlocked)
            {
                Rect portalBanner = new Rect(sw * 0.5f - 260f, sh * 0.5f + 58f, 520f, 32f);
                DrawRect(portalBanner, new Color(0.15f, 0.11f, 0.03f, 0.92f));
                DrawRect(new Rect(portalBanner.x, portalBanner.y, portalBanner.width, 2f), new Color(1f, 0.82f, 0.28f, 0.85f));
                GUI.Label(portalBanner, "PORTAL UNSEALED — ENTER THE GOLDEN RIFT TO ADVANCE!", hudCenterSmallStyle);
            }

            // TOP-LEFT: Mission Level (1-100), Realm & Killfeed
            DrawRect(new Rect(14, 14, 384, 92), new Color(0.06f, 0.05f, 0.08f, 0.88f));
            StratumDef curRealm = GetRealmForLevel(currentLevel);
            string sName = currentMode == GameModeType.EndlessHorde
                ? $"CITADEL HORDE // WAVE {endlessWave}"
                : $"LEVEL {currentLevel}/100 {(currentLevel % 10 == 0 ? "[FINAL BOSS]" : "")} — {curRealm.name.Split('(')[0].Trim()}";
            GUI.Label(new Rect(24, 20, 364, 22), sName, smallMonoStyle);
            GUI.Label(new Rect(24, 44, 364, 22), BeaconUnlocked ? "ENTER GOLDEN PORTAL FOR NEXT LEVEL!" : $"HOSTILES REMAINING: {ActiveEnemies.Count} / {Mathf.Max(1, totalStratumEnemies)}", labelStyle);
            DrawBar(new Rect(24, 74, 364, 10), (float)ActiveEnemies.Count / Mathf.Max(1, totalStratumEnemies), currentLevel % 10 == 0 ? new Color(1f, 0.78f, 0.18f) : new Color(1f, 0.23f, 0.13f));

            for (int i = 0; i < killfeed.Count; i++)
            {
                Rect kfRect = new Rect(14, 112 + i * 24, 384, 22);
                DrawRect(kfRect, new Color(0.14f, 0.04f, 0.03f, 0.78f));
                GUI.Label(new Rect(kfRect.x + 8f, kfRect.y, kfRect.width - 16f, kfRect.height), killfeed[i], smallMonoStyle);
            }

            // TOP-CENTER: Clock & Dynamic Unique Final Boss HP Bar
            Rect clockRect = new Rect(sw * 0.5f - 100f, 14f, 200f, 32f);
            DrawRect(clockRect, new Color(0.06f, 0.05f, 0.08f, 0.88f));
            float tVal = currentMode == GameModeType.PurgatoryBlitz ? blitzTimer : elapsedTime;
            GUI.Label(clockRect, $"{(currentMode == GameModeType.PurgatoryBlitz ? "DOOMSDAY:" : "TIME:")} {tVal:F1}s", hudCenterSmallStyle);

            DemonEnemyAI boss = ActiveEnemies.Find(e => e != null && e.isBoss && !e.IsDead);
            if (boss != null)
            {
                DrawRect(new Rect(sw * 0.5f - 265f, 52f, 530f, 48f), new Color(0.12f, 0.03f, 0.16f, 0.92f));
                string phaseTag = boss.BossPhase > 1 ? $" [PHASE {boss.BossPhase}]" : "";
                GUI.Label(new Rect(sw * 0.5f - 255f, 55f, 510f, 22f),
                    $"LVL {currentLevel} BOSS: {boss.displayName}, {boss.bossTitle}{phaseTag} ({Mathf.CeilToInt(boss.hp)} / {Mathf.CeilToInt(boss.maxHp)})",
                    hudCenterSmallStyle);
                DrawBar(new Rect(sw * 0.5f - 255f, 80f, 510f, 12f), boss.hp / Mathf.Max(1f, boss.maxHp), new Color(0.85f, 0.22f, 1f));
            }

            // TOP-RIGHT: Live 2D Top-Down Radar + Score & Soul Credits (Zero text wrap/clipping!)
            DrawRect(new Rect(sw - 332f, 14f, 318f, 124f), new Color(0.06f, 0.05f, 0.08f, 0.88f));
            DrawTopDownRadar(new Rect(sw - 322f, 22f, 108f, 108f));
            GUI.Label(new Rect(sw - 202f, 22f, 180f, 22f), $"SCORE: {score:D6}", smallMonoStyle);
            GUI.Label(new Rect(sw - 202f, 48f, 180f, 26f), $"CREDITS: ${p.credits}", hudValueStyle);
            GUI.Label(new Rect(sw - 202f, 80f, 180f, 22f), $"CHAIN: {multiplier:F1}x", smallMonoStyle);
            DrawBar(new Rect(sw - 202f, 108f, 176f, 8f), comboTimer / 4.5f, new Color(1f, 0.82f, 0.32f));

            // BOTTOM DECK: All 3 bottom panels aligned cleanly at y = sh - 122, height = 108
            float botY = sh - 122f;
            float botH = 108f;

            // BOTTOM-LEFT: Vitality HP, Aegis Armor, Phase-Dash
            DrawRect(new Rect(14f, botY, 300f, botH), new Color(0.06f, 0.05f, 0.08f, 0.88f));
            GUI.Label(new Rect(24f, botY + 12f, 114f, 22f), $"VITALITY: {p.hp}", smallMonoStyle);
            DrawBar(new Rect(142f, botY + 17f, 160f, 12f), p.hp / (float)p.maxHp, new Color(0.16f, 0.94f, 0.53f));
            GUI.Label(new Rect(24f, botY + 44f, 114f, 22f), $"AEGIS: {p.armor}", smallMonoStyle);
            DrawBar(new Rect(142f, botY + 49f, 160f, 12f), p.armor / (float)p.maxArmor, new Color(0.14f, 0.85f, 1f));
            GUI.Label(new Rect(24f, botY + 76f, 114f, 22f), p.DashReady ? "[SHIFT] READY" : "[SHIFT] CHRG", smallMonoStyle);
            DrawBar(new Rect(142f, botY + 82f, 160f, 10f), p.DashCooldownRatio, new Color(1f, 0.65f, 0.18f));

            // BOTTOM-CENTER: Ascension Resonance Core + Melee/Special/Shop Strip (Centered & Unclipped!)
            float bcW = 420f;
            float bcX = sw * 0.5f - bcW * 0.5f;
            DrawRect(new Rect(bcX, botY, bcW, botH), new Color(0.06f, 0.05f, 0.08f, 0.88f));
            string ascLabel = OverdriveActive ? $"OVERDRIVE ACTIVE ({overdriveTimer:F1}s)!" : $"ASCENSION RESONANCE [Q/E]: {Mathf.RoundToInt(ascensionGauge)}%";
            GUI.Label(new Rect(bcX + 10f, botY + 8f, bcW - 20f, 22f), ascLabel, hudCenterSmallStyle);
            DrawBar(new Rect(bcX + 14f, botY + 34f, bcW - 28f, 14f), OverdriveActive ? (overdriveTimer / 8f) : (ascensionGauge / 100f), new Color(0.85f, 0.32f, 1f));
            int abChg = p.abilityCharges.ContainsKey(p.equippedAbility) ? p.abilityCharges[p.equippedAbility] : 0;
            GUI.Label(new Rect(bcX + 10f, botY + 55f, bcW - 20f, 22f), $"[V] {p.melees[p.meleeIndex].name}  |  [X] {p.equippedAbility.ToUpper()} ({abChg})", hudCenterSmallStyle);
            GUI.Label(new Rect(bcX + 10f, botY + 79f, bcW - 20f, 22f), "[B] SHOP  ·  [R] RELOAD  ·  [Y] INSPECT  ·  [RMB] ADS", hudCenterSmallStyle);

            // BOTTOM-RIGHT: Active Valorant-Style Weapon & 5-Slot Bar (Single-line Weapon Title + Right-Aligned Ammo + Centered Slots!)
            WeaponDef w = p.weapons[p.weaponIndex];
            float brW = 370f;
            float brX = sw - brW - 14f;
            DrawRect(new Rect(brX, botY, brW, botH), new Color(0.06f, 0.05f, 0.08f, 0.88f));
            GUI.Label(new Rect(brX + 10f, botY + 8f, 246f, 24f), $"{w.name} (MK-{w.level})", hudValueStyle);
            string ammoStr = w.ammoType == "infinite" ? $"{p.mags[p.weaponIndex]} / INF" : $"{p.mags[p.weaponIndex]} / {(w.ammoType == "shells" ? p.shells : p.cells)}";
            GUI.Label(new Rect(brX + 258f, botY + 8f, 102f, 24f), ammoStr, hudAmmoRightStyle);
            GUI.Label(new Rect(brX + 10f, botY + 35f, brW - 20f, 20f), w.ammoLabel, smallMonoStyle);

            float slotW = 66f;
            float slotGap = 5f;
            for (int i = 0; i < p.weapons.Count; i++)
            {
                Rect sr = new Rect(brX + 10f + i * (slotW + slotGap), botY + 62f, slotW, 34f);
                DrawRect(sr, i == p.weaponIndex ? new Color(0.85f, 0.20f, 0.14f, 0.85f) : new Color(0.14f, 0.12f, 0.16f, 0.88f));
                string tag = p.weapons[i].owned ? $"{i + 1}:{p.weapons[i].shortName.Substring(0, Mathf.Min(4, p.weapons[i].shortName.Length))}" : "LCK";
                GUI.Label(sr, tag, hudSlotStyle);
            }
        }

        private void DrawTopDownRadar(Rect r)
        {
            DrawRect(r, new Color(0.02f, 0.02f, 0.04f, 0.95f));
            if (worldBuilder != null)
            {
                foreach (var b in worldBuilder.RadarObstacles)
                {
                    Vector2 c = WorldToRadar(r, b.center.x, b.center.z);
                    DrawRect(new Rect(c.x - 3, c.y - 3, 6, 6), new Color(0.28f, 0.24f, 0.34f));
                }
            }
            Vector2 portPt = WorldToRadar(r, 0f, -31.2f);
            DrawRect(new Rect(portPt.x - 4, portPt.y - 4, 8, 8), BeaconUnlocked ? Color.yellow : new Color(0.6f, 0.2f, 1f));

            foreach (var e in ActiveEnemies)
            {
                if (e == null || e.IsDead) continue;
                Vector2 ep = WorldToRadar(r, e.transform.position.x, e.transform.position.z);
                DrawRect(new Rect(ep.x - 2.5f, ep.y - 2.5f, 5, 5), e.isBoss ? Color.yellow : Color.red);
            }
            if (FPSPlayerController.Instance != null)
            {
                Vector3 pp3 = FPSPlayerController.Instance.transform.position;
                Vector2 pp = WorldToRadar(r, pp3.x, pp3.z);
                DrawRect(new Rect(pp.x - 3, pp.y - 3, 6, 6), Color.green);
            }
        }

        private static Vector2 WorldToRadar(Rect r, float wx, float wz)
        {
            float nx = Mathf.Clamp01((wx + 28f) / 56f);
            float ny = Mathf.Clamp01((wz + 38f) / 58f);
            return new Vector2(r.x + nx * r.width, r.y + ny * r.height);
        }

        private void DrawShopModal(float sw, float sh)
        {
            var p = FPSPlayerController.Instance;
            if (p == null) return;
            DrawRect(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.84f));

            Rect box = new Rect(sw * 0.06f, sh * 0.06f, sw * 0.88f, sh * 0.88f);
            DrawRect(box, new Color(0.08f, 0.07f, 0.06f, 0.97f));
            DrawRect(new Rect(box.x, box.y, box.width, 2f), new Color(0.86f, 0.71f, 0.36f, 0.85f));

            GUI.Label(new Rect(box.x + 22, box.y + 14, 580, 30), "BLACK-MARKET DEMONIC ARMORY & LOADOUT SHOP [B]", headerStyle);
            GUI.Label(new Rect(box.xMax - 390, box.y + 14, 195, 30), $"CREDITS: ${p.credits}", hudAmmoRightStyle);
            if (GUI.Button(new Rect(box.xMax - 180, box.y + 12, 160, 32), "CLOSE / RESUME [B]", buttonBrassStyle)) ToggleShop();

            // Category Tabs
            if (GUI.Button(new Rect(box.x + 22, box.y + 56, 200, 32), shopCategory == 0 ? "[1] FIREARMS & UPGRADES" : "1. FIREARMS & UPGRADES", shopCategory == 0 ? buttonBrassStyle : buttonDarkStyle)) shopCategory = 0;
            if (GUI.Button(new Rect(box.x + 232, box.y + 56, 200, 32), shopCategory == 1 ? "[2] MELEE WEAPONS [V]" : "2. MELEE WEAPONS [V]", shopCategory == 1 ? buttonBrassStyle : buttonDarkStyle)) shopCategory = 1;
            if (GUI.Button(new Rect(box.x + 442, box.y + 56, 240, 32), shopCategory == 2 ? "[3] SPECIAL ABILITIES [X]" : "3. SPECIAL ABILITIES [X]", shopCategory == 2 ? buttonBrassStyle : buttonDarkStyle)) shopCategory = 2;

            Rect listView = new Rect(box.x + 22, box.y + 102, box.width - 44, box.height - 122);
            GUILayout.BeginArea(listView);
            shopScroll = GUILayout.BeginScrollView(shopScroll);

            if (shopCategory == 0)
            {
                for (int i = 0; i < p.weapons.Count; i++)
                {
                    WeaponDef w = p.weapons[i];
                    GUILayout.BeginVertical(panelBoxStyle);
                    GUILayout.Label($"SLOT [{i + 1}] {w.name}  —  {(w.owned ? $"MK-{w.level} | DMG: {w.damage}{(w.pellets > 1 ? $"x{w.pellets}" : "")} | MAG: {w.magSize}" : "LOCKED")}", headerStyle);
                    GUILayout.Label(w.desc, labelStyle);
                    GUILayout.BeginHorizontal();
                    int cost = w.owned ? w.upgradeCost * w.level : w.buyCost;
                    if (!w.owned)
                    {
                        GUI.enabled = p.credits >= cost;
                        if (GUILayout.Button($"UNLOCK FIREARM (${cost})", buttonBrassStyle, GUILayout.Height(30)))
                        {
                            p.credits -= cost; w.owned = true; p.shells = Mathf.Min(p.maxShells, p.shells + 20);
                            p.mags[i] = w.magSize; p.SelectWeapon(i);
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayPickup();
                        }
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUI.enabled = p.credits >= cost && w.level < 5;
                        if (GUILayout.Button(w.level >= 5 ? "MAX LEVEL (MK-5)" : $"UPGRADE TO MK-{w.level + 1} (${cost})", buttonBrassStyle, GUILayout.Height(30)))
                        {
                            p.credits -= cost; w.level++;
                            w.damage = Mathf.Round(w.baseDamage * (1f + (w.level - 1) * 0.22f));
                            w.magSize += 4;
                            p.mags[i] = w.magSize;
                            p.shells = Mathf.Min(p.maxShells, p.shells + 16);
                            p.cells = Mathf.Min(p.maxCells, p.cells + 60);
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayReload(i);
                        }
                        GUI.enabled = true;
                        if (GUILayout.Button(p.weaponIndex == i ? "EQUIPPED" : "EQUIP", buttonDarkStyle, GUILayout.Height(30), GUILayout.Width(130)))
                        {
                            p.SelectWeapon(i);
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayPickup();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(6);
                }
            }
            else if (shopCategory == 1)
            {
                for (int i = 0; i < p.melees.Count; i++)
                {
                    MeleeDef m = p.melees[i];
                    GUILayout.BeginVertical(panelBoxStyle);
                    GUILayout.Label($"{m.name}  —  DMG: {m.damage} | REACH: {m.range}m", headerStyle);
                    GUILayout.Label(m.desc, labelStyle);
                    if (!m.owned)
                    {
                        GUI.enabled = p.credits >= m.cost;
                        if (GUILayout.Button($"BUY & EQUIP ON [V] (${m.cost})", buttonBrassStyle, GUILayout.Height(30)))
                        {
                            p.credits -= m.cost; m.owned = true; p.meleeIndex = i;
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayMelee(i);
                        }
                        GUI.enabled = true;
                    }
                    else
                    {
                        if (GUILayout.Button(p.meleeIndex == i ? "EQUIPPED ON [V]" : "EQUIP ON [V]", buttonDarkStyle, GUILayout.Height(30)))
                        {
                            p.meleeIndex = i;
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayMelee(i);
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(6);
                }
            }
            else
            {
                foreach (var ab in p.abilities)
                {
                    int chg = p.abilityCharges.ContainsKey(ab.id) ? p.abilityCharges[ab.id] : 0;
                    GUILayout.BeginVertical(panelBoxStyle);
                    GUILayout.Label($"{ab.name} [{ab.tag}]  —  {(ab.isInstant ? "INSTANT" : $"CHARGES: {chg}")}", headerStyle);
                    GUILayout.Label(ab.desc, labelStyle);
                    GUILayout.BeginHorizontal();
                    GUI.enabled = p.credits >= ab.cost;
                    if (GUILayout.Button(ab.isInstant ? $"BUY FULL RESUPPLY (${ab.cost})" : $"BUY +{ab.chargesPerBuy} CHARGES (${ab.cost})", buttonBrassStyle, GUILayout.Height(30)))
                    {
                        p.credits -= ab.cost;
                        if (ab.isInstant)
                        {
                            p.hp = p.maxHp; p.armor = p.maxArmor; p.shells = p.maxShells; p.cells = p.maxCells;
                            p.ResetLoadoutMags();
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayReload(p.weaponIndex);
                        }
                        else
                        {
                            p.abilityCharges[ab.id] = chg + ab.chargesPerBuy;
                            p.equippedAbility = ab.id;
                            if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayOverdrive();
                        }
                    }
                    GUI.enabled = true;
                    if (!ab.isInstant && GUILayout.Button(p.equippedAbility == ab.id ? "ACTIVE ON [X]" : "EQUIP ON [X]", buttonDarkStyle, GUILayout.Height(30), GUILayout.Width(140)))
                    {
                        p.equippedAbility = ab.id;
                        if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.PlayPickup();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(6);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStateModal(float sw, float sh)
        {
            DrawRect(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, 0.78f));
            Rect box = new Rect(sw * 0.5f - 310f, sh * 0.5f - 160f, 620f, 320f);
            DrawRect(box, new Color(0.09f, 0.07f, 0.10f, 0.96f));
            DrawRect(new Rect(box.x, box.y, box.width, 2f), new Color(0.86f, 0.71f, 0.36f, 0.85f));

            GUI.Label(new Rect(box.x + 24f, box.y + 18f, box.width - 48f, 44f), modalTitle, centerHeadStyle);
            GUI.Label(new Rect(box.x + 28f, box.y + 66f, box.width - 56f, 46f), modalSub, hudCenterBodyStyle);

            string rank = score >= 12000 ? "S+ SOVEREIGN" : score >= 7000 ? "S WARDEN" : score >= 3500 ? "A ASCENDANT" : "B INITIATE";
            GUI.Label(new Rect(box.x + 24f, box.y + 122f, box.width - 48f, 28f), $"SCORE: {score}   |   KILLS: {kills}   |   PEAK CHAIN: {peakMultiplier:F1}x   |   RANK: {rank}", hudCenterSmallStyle);

            float btnY = box.y + 178f;
            float leftBtnX = box.x + 30f;
            float rightBtnX = box.x + 320f;
            float halfBtnW = 270f;
            if (modalMode == "pause")
            {
                if (GUI.Button(new Rect(leftBtnX, btnY, halfBtnW, 38f), "▶ RESUME ASCENSION", buttonBrassStyle)) ResumeGame();
                if (GUI.Button(new Rect(rightBtnX, btnY, halfBtnW, 38f), "🛒 ARMORY SHOP [B]", buttonDarkStyle)) ToggleShop();
            }
            else if (modalMode == "stratum")
            {
                if (GUI.Button(new Rect(leftBtnX, btnY, 310f, 38f), $"▶ ASCEND TO LEVEL {currentLevel + 1}", buttonBrassStyle))
                {
                    if (FPSPlayerController.Instance != null)
                    {
                        FPSPlayerController.Instance.hp = Mathf.Min(FPSPlayerController.Instance.maxHp, FPSPlayerController.Instance.hp + 35);
                        FPSPlayerController.Instance.armor = Mathf.Min(FPSPlayerController.Instance.maxArmor, FPSPlayerController.Instance.armor + 25);
                    }
                    LoadCampaignLevel(currentLevel + 1);
                    ResumeGame();
                }
                if (GUI.Button(new Rect(box.x + 356f, btnY, 234f, 38f), "🛒 ARMORY SHOP [B]", buttonDarkStyle)) ToggleShop();
            }
            else if (modalMode == "defeat")
            {
                if (GUI.Button(new Rect(leftBtnX, btnY, halfBtnW, 38f), $"RETRY LEVEL {currentLevel}", buttonBrassStyle))
                {
                    if (FPSPlayerController.Instance != null) { FPSPlayerController.Instance.hp = 100; FPSPlayerController.Instance.armor = 50; }
                    LoadCampaignLevel(currentLevel);
                    if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.StartMusic();
                    ResumeGame();
                }
                if (GUI.Button(new Rect(rightBtnX, btnY, halfBtnW, 38f), "RESTART FROM LVL 1", buttonDarkStyle))
                {
                    selectedStartLevel = 1;
                    StartNewGame();
                }
            }
            else if (modalMode == "victory")
            {
                if (GUI.Button(new Rect(leftBtnX, btnY, 560f, 38f), "PLAY AGAIN FROM LEVEL 1", buttonBrassStyle))
                {
                    selectedStartLevel = 1;
                    StartNewGame();
                }
            }

            if (GUI.Button(new Rect(leftBtnX, btnY + 50f, 560f, 36f), "MAIN TITLE SCREEN", buttonDarkStyle))
            {
                IsRunning = false;
                IsPaused = false;
                modalMode = "";
                selectedStartLevel = Mathf.Clamp(highestLevelUnlocked, 1, GameCatalog.MaxCampaignLevel);
                if (worldBuilder != null) worldBuilder.ApplyStratumAtmosphere(GetRealmForLevel(selectedStartLevel));
                if (RealisticGunAudio.Instance != null) RealisticGunAudio.Instance.StopMusic();
                LockCursor(false);
            }
        }

        private static void ApplyVirtualRotation(Matrix4x4 baseMat, float angleDeg, Vector2 pivot)
        {
            GUI.matrix = baseMat
                * Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0f))
                * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angleDeg))
                * Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0f));
        }

        private void DrawRect(Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(r, whiteTex);
            GUI.color = prev;
        }

        private void DrawBar(Rect r, float fill01, Color fillCol)
        {
            DrawRect(r, new Color(1f, 1f, 1f, 0.12f));
            DrawRect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill01), r.height), fillCol);
        }
    }
}
