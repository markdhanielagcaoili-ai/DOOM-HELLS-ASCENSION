using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Clean tactical FPS block-and-wall arena builder:
    /// Contains only the arena floor, 4 perimeter walls, clean tactical cover blocks,
    /// and the back-wall Ascension Portal (zero smoke clouds, lamps, wrecks, cables, or clutter).
    /// </summary>
    public class CitadelWorldBuilder : MonoBehaviour
    {
        public Transform portalRing;
        public Renderer portalRingRenderer;
        public Light sunLight;
        public List<Bounds> RadarObstacles = new List<Bounds>();

        [SerializeField] private Transform worldRoot;

        private void Start()
        {
            // Keep Directional Sun high in the sky so its editor gizmo never floats at eye level
            if (sunLight != null) sunLight.transform.position = new Vector3(0f, 95f, 0f);
            // Always rebuild cleanly at runtime so no stale clutter/smoke/lamps remain in older scene saves
            BuildEnvironment();
        }

        public static Material MakeStdMat(Color color, float metallic = 0.1f, float smoothness = 0.25f, Texture2D mainTex = null)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            m.color = color;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (mainTex != null) m.mainTexture = mainTex;
            return m;
        }

        public static Material MakeEmissiveMat(Color color, float intensity = 2.0f)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            Material m = new Material(sh);
            m.color = color;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * intensity);
            }
            return m;
        }

        public static Material MakeUnlitMat(Color color)
        {
            Shader sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            m.color = color;
            return m;
        }

        public static Texture2D CreateAshlarStoneTexture(Color baseCol, Color runeCol, bool addRunes)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            System.Random rng = new System.Random(1337);

            for (int y = 0; y < size; y++)
            {
                int row = y / 32;
                int shift = (row % 2 == 0) ? 0 : 32;
                for (int x = 0; x < size; x++)
                {
                    float noise = (float)(rng.NextDouble() * 0.10 - 0.05);
                    Color c = new Color(
                        Mathf.Clamp01(baseCol.r + noise),
                        Mathf.Clamp01(baseCol.g + noise),
                        Mathf.Clamp01(baseCol.b + noise),
                        1f
                    );

                    bool mortar = (y % 32 < 2) || (((x + shift) % 64) < 2);
                    if (mortar) c *= 0.48f;

                    if (addRunes && y >= 123 && y <= 125) c = runeCol;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        public void BuildEnvironment()
        {
            // Cleanly destroy any previously generated world roots or stale scene children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediateSafe(transform.GetChild(i).gameObject);
            }
            worldRoot = null;
            RadarObstacles.Clear();

            GameObject rootGo = new GameObject("CitadelWorldRoot");
            rootGo.transform.SetParent(transform, false);
            worldRoot = rootGo.transform;

            Texture2D wallTex = CreateAshlarStoneTexture(new Color(0.56f, 0.50f, 0.43f), new Color(1f, 0.23f, 0.13f), false);
            Texture2D blockTex = CreateAshlarStoneTexture(new Color(0.24f, 0.22f, 0.21f), new Color(0.95f, 0.28f, 0.14f), true);
            Texture2D floorTex = CreateAshlarStoneTexture(new Color(0.32f, 0.29f, 0.26f), Color.black, false);

            Material wallMat = MakeStdMat(Color.white, 0.08f, 0.22f, wallTex);
            Material blockMat = MakeStdMat(Color.white, 0.18f, 0.30f, blockTex);
            Material floorMat = MakeStdMat(Color.white, 0.06f, 0.16f, floorTex);
            floorMat.mainTextureScale = new Vector2(18f, 20f);

            // 1. Clean Tactical Arena Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(worldRoot, false);
            floor.transform.position = new Vector3(0f, -0.5f, -7f);
            floor.transform.localScale = new Vector3(56f, 1f, 64f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 2. Clean 4-Sided Perimeter Arena Walls
            AddBlock("NorthWall", new Vector3(0f, 5f, -35.5f), new Vector3(54f, 10f, 3f), wallMat, true);
            AddBlock("SouthWall", new Vector3(0f, 5f, 21.5f), new Vector3(54f, 10f, 3f), wallMat, true);
            AddBlock("LeftWall", new Vector3(-25.5f, 5f, -7f), new Vector3(3f, 10f, 58f), wallMat, true);
            AddBlock("RightWall", new Vector3(25.5f, 5f, -7f), new Vector3(3f, 10f, 58f), wallMat, true);

            // 3. Clean Symmetrical Tactical Cover Blocks (Clear sightlines to all enemies & Boss!)
            AddBlock("CoverBlock_FrontL", new Vector3(-10.5f, 1.25f, 4.5f), new Vector3(4.0f, 2.5f, 2.2f), blockMat, true);
            AddBlock("CoverBlock_FrontR", new Vector3(10.5f, 1.25f, 4.5f), new Vector3(4.0f, 2.5f, 2.2f), blockMat, true);

            AddBlock("CoverBlock_MidL", new Vector3(-11.0f, 1.4f, -3.5f), new Vector3(3.6f, 2.8f, 2.4f), blockMat, true);
            AddBlock("CoverBlock_MidR", new Vector3(11.0f, 1.4f, -3.5f), new Vector3(3.6f, 2.8f, 2.4f), blockMat, true);

            AddBlock("CoverBlock_CenterL", new Vector3(-5.5f, 1.1f, -9.5f), new Vector3(3.2f, 2.2f, 2.0f), blockMat, true);
            AddBlock("CoverBlock_CenterR", new Vector3(5.5f, 1.1f, -9.5f), new Vector3(3.2f, 2.2f, 2.0f), blockMat, true);

            AddBlock("CoverBlock_BackL", new Vector3(-13.5f, 1.6f, -24.5f), new Vector3(3.8f, 3.2f, 2.2f), blockMat, true);
            AddBlock("CoverBlock_BackR", new Vector3(13.5f, 1.6f, -24.5f), new Vector3(3.8f, 3.2f, 2.2f), blockMat, true);

            // 4. Clean Ascension Portal on the North Wall (Unseals after all enemies on the level are eliminated)
            GameObject portalRoot = new GameObject("CitadelAscensionPortal");
            portalRoot.transform.SetParent(worldRoot, false);
            portalRoot.transform.position = new Vector3(0f, 2.6f, -31.2f);
            portalRing = portalRoot.transform;

            GameObject ringOuter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringOuter.name = "PortalHalo";
            DestroyImmediateSafe(ringOuter.GetComponent<Collider>());
            ringOuter.transform.SetParent(portalRing, false);
            ringOuter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringOuter.transform.localScale = new Vector3(3.8f, 0.14f, 3.8f);
            portalRingRenderer = ringOuter.GetComponent<Renderer>();
            portalRingRenderer.sharedMaterial = MakeEmissiveMat(new Color(0.55f, 0.15f, 1f), 2.5f);
        }

        private GameObject AddBlock(string blockName, Vector3 pos, Vector3 scale, Material mat, bool addToRadar)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = blockName;
            go.transform.SetParent(worldRoot, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (addToRadar) RadarObstacles.Add(new Bounds(pos, scale));
            return go;
        }

        public void ApplyStratumAtmosphere(StratumDef stratum)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = stratum.fogColor;
            // Light atmospheric tint only so walls, cover blocks, and enemies remain crystal-clear
            RenderSettings.fogDensity = stratum.fogDensity * 0.35f;
            RenderSettings.ambientLight = stratum.ambientColor;

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = stratum.skyColor;
            }
            if (sunLight != null)
            {
                sunLight.color = stratum.sunColor;
            }
            if (portalRingRenderer != null)
            {
                portalRingRenderer.material = MakeEmissiveMat(new Color(0.55f, 0.15f, 1f), 2.5f);
            }
        }

        public void SetPortalUnlocked(bool unlocked)
        {
            if (portalRingRenderer != null)
            {
                Color c = unlocked ? new Color(1f, 0.82f, 0.28f) : new Color(0.55f, 0.15f, 1f);
                portalRingRenderer.material = MakeEmissiveMat(c, unlocked ? 4.0f : 2.2f);
            }
        }

        private void Update()
        {
            if (portalRing != null)
            {
                bool unlocked = GameManager.Instance != null && GameManager.Instance.BeaconUnlocked;
                portalRing.Rotate(0f, 0f, (unlocked ? 140f : 35f) * Time.deltaTime);
            }
        }

        private static void DestroyImmediateSafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
