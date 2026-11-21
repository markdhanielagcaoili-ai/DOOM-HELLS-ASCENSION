#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoomHellsAscension.Editor
{
    [InitializeOnLoad]
    public static class DoomSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainCitadel.unity";
        private const string AutoOpenKey = "DoomHellsAscension_AutoOpenedScene";

        static DoomSceneBuilder()
        {
            EditorApplication.delayCall += TryAutoOpenCitadelScene;
            EditorApplication.delayCall += DisableGameViewGizmos;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                DisableGameViewGizmos();
            }
        }

        /// <summary>
        /// Automatically turns OFF the Editor "Gizmos" overlay in Unity's Game View so Lightbulb,
        /// Sun, Camera, and AudioSource editor icons never obstruct gameplay.
        /// </summary>
        private static void DisableGameViewGizmos()
        {
            if (Application.isBatchMode) return;
            try
            {
                var gvType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gvType == null) return;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                var gizmoField = gvType.GetField("m_Gizmos", flags);
                var gizmoProp = gvType.GetProperty("showGizmos", flags);
                foreach (var win in Resources.FindObjectsOfTypeAll(gvType))
                {
                    if (gizmoField != null) gizmoField.SetValue(win, false);
                    else if (gizmoProp != null && gizmoProp.CanWrite) gizmoProp.SetValue(win, false, null);
                    if (win is EditorWindow ew) ew.Repaint();
                }
            }
            catch { }
        }

        private static void TryAutoOpenCitadelScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(AutoOpenKey, false)) return;
            SessionState.SetBool(AutoOpenKey, true);

            Scene active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path))
            {
                if (File.Exists(ScenePath))
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                    Debug.Log("[DOOM: HELL'S ASCENSION] Auto-opened Assets/Scenes/MainCitadel.unity — press Play!");
                }
                else
                {
                    BuildProjectScene();
                }
            }
        }

        [MenuItem("DOOM: Hell's Ascension/Build & Open Citadel Scene")]
        public static void BuildProjectScene()
        {
            PlayerSettings.productName = "DOOM: HELL'S ASCENSION";
            PlayerSettings.companyName = "Markdhaniel Agcaoili";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Eclipse Sun Light (Positioned high overhead out of camera view)
            GameObject sunGo = new GameObject("CitadelSunLight");
            sunGo.transform.position = new Vector3(0f, 95f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.90f, 0.72f);
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(42f, -38f, 0f);

            // 2. Citadel 3D World Architecture
            GameObject worldGo = new GameObject("CitadelWorldBuilder");
            CitadelWorldBuilder world = worldGo.AddComponent<CitadelWorldBuilder>();
            world.sunLight = sun;
            world.BuildEnvironment();
            SaveGeneratedAssets(worldGo, "World");

            // 3. Player Rig + Main Camera + 3D Weapon Viewmodel
            GameObject playerGo = new GameObject("PlayerFPS");
            playerGo.transform.position = new Vector3(0f, 1.05f, 13f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.1f, 0f);
            FPSPlayerController fps = playerGo.AddComponent<FPSPlayerController>();

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(playerGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 260f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.60f, 0.44f);
            camGo.AddComponent<AudioListener>();

            WeaponViewmodel3D vm = camGo.AddComponent<WeaponViewmodel3D>();
            vm.BuildViewmodel();
            SaveGeneratedAssets(camGo, "Weapon");
            fps.playerCamera = cam;
            fps.viewmodel = vm;

            // 4. Realistic Multi-Layer Gun & Reload Audio Engine
            GameObject audioGo = new GameObject("RealisticGunAudio");
            audioGo.AddComponent<RealisticGunAudio>();

            // 5. GameManager (HUD, BLACK Landing Screen, Armory Shop [B], 6-Criteria Rubric)
            GameObject gmGo = new GameObject("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.worldBuilder = world;

            // Create clean Prefabs under Assets/Prefabs for easy reuse
            EnsureDir("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(playerGo, "Assets/Prefabs/PlayerFPS.prefab");
            PrefabUtility.SaveAsPrefabAsset(worldGo, "Assets/Prefabs/CitadelPlaza.prefab");
            PrefabUtility.SaveAsPrefabAsset(audioGo, "Assets/Prefabs/RealisticGunAudio.prefab");
            PrefabUtility.SaveAsPrefabAsset(gmGo, "Assets/Prefabs/GameManager.prefab");

            // Save Scene to Assets/Scenes/MainCitadel.unity
            EnsureDir(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register scene as index 0 in EditorBuildSettings
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DOOM: HELL'S ASCENSION] Built and saved Assets/Scenes/MainCitadel.unity & Prefabs successfully!");
        }

        [MenuItem("DOOM: Hell's Ascension/Build WebGL for Vercel")]
        public static void BuildWebGLForVercel()
        {
            BuildProjectScene();

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "WebGLBuild"));
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[DOOM: HELL'S ASCENSION] WebGL Build SUCCEEDED ({report.summary.totalSize / (1024f * 1024f):F2} MB) at: {outDir}");
            }
            else
            {
                Debug.LogError($"[DOOM: HELL'S ASCENSION] WebGL Build FAILED: {report.summary.result} (Errors: {report.summary.totalErrors})");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void EnsureDir(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void SaveGeneratedAssets(GameObject root, string prefix)
        {
            const string genDir = "Assets/Generated";
            EnsureDir(genDir);

            int idx = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && !AssetDatabase.Contains(mf.sharedMesh))
                {
                    AssetDatabase.CreateAsset(mf.sharedMesh, $"{genDir}/{prefix}_Mesh_{idx++}.asset");
                }
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                Material mat = r.sharedMaterial;
                if (mat == null) continue;
                if (mat.mainTexture != null && !AssetDatabase.Contains(mat.mainTexture))
                {
                    AssetDatabase.CreateAsset(mat.mainTexture, $"{genDir}/{prefix}_Tex_{idx++}.asset");
                }
                if (!AssetDatabase.Contains(mat))
                {
                    AssetDatabase.CreateAsset(mat, $"{genDir}/{prefix}_Mat_{idx++}.mat");
                }
            }
        }
    }
}
#endif
