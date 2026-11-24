using System.Collections.Generic;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Volcanic Hell Citadel 3D FPS Arena & Animated Obsidian Diamond Ascension Portal:
    /// - 360-degree continuous animated Volcanic Lava Waterfall & Smoky Ash Sky Dome (HellBackdrop360.shader)
    /// - Dark charred obsidian fortress walls with glowing carved red runes, jagged gothic battlements,
    ///   hanging iron chains, flickering flaming braziers, swaying crimson war banners, and continuous flowing
    ///   molten lava rivers (MoltenLavaFlow.shader)
    /// - Sculpted 3D Obsidian Diamond Ascension Portal with dramatic Unseal Burst Animation & continuous
    ///   swirling molten-gold vortex (DemonicPortalVortex.shader) once all enemies are eliminated!
    /// </summary>
    public class CitadelWorldBuilder : MonoBehaviour
    {
        public Transform portalRing;
        public Renderer portalRingRenderer;
        public Light sunLight;
        public List<Bounds> RadarObstacles = new List<Bounds>();

        [SerializeField] private Transform worldRoot;

        // Runtime animated references
        private Material backdropMat;
        private Material portalVortexMat;
        private Material portalRuneMat;
        private Material portalBeamMat;
        private Material shockwaveMat;
        private Transform portalOrbitRoot;
        private Transform portalSkyBeam;
        private Transform portalShockwave;
        private Transform portalSealA;
        private Transform portalSealB;
        private readonly List<Transform> portalShards = new List<Transform>();
        private readonly List<Transform> flameBillboards = new List<Transform>();
        private readonly List<Transform> swayingBanners = new List<Transform>();
        private ParticleSystem portalVortexPS;

        private bool portalUnlockedState;
        private float portalUnlockAnim;     // 0 = sealed, 1 = fully open swirling vortex
        private float portalFlashBurst;     // 1.8 -> 0 dramatic ignition flash
        private float shockwaveAnim = -1f;  // 0 -> 1 expanding blast wave on unlock
        private static Texture2D cachedSoftGlowTex;

        private void Start()
        {
            if (sunLight != null) sunLight.transform.position = new Vector3(0f, 95f, 0f);
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

        public static Material MakeObsidianRuneMat(Texture2D albedoTex, Texture2D emissionTex, Color emissionCol, float metallic = 0.16f, float smoothness = 0.26f)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            m.color = Color.white;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (albedoTex != null) m.mainTexture = albedoTex;
            if (emissionTex != null && m.HasProperty("_EmissionMap"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", emissionTex);
                m.SetColor("_EmissionColor", emissionCol);
            }
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

        private static Texture2D GetSoftRadialGlowTexture()
        {
            if (cachedSoftGlowTex != null) return cachedSoftGlowTex;
            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float half = (sz - 1) * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            cachedSoftGlowTex = tex;
            return tex;
        }

        private static Material MakeAdditiveSoftMat(Color tint, Texture2D tex = null)
        {
            Shader sh = Resources.Load<Shader>("Shaders/SoftAdditiveGlow");
            if (sh == null) sh = Shader.Find("Hidden/DoomSoftAdditiveGlow");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            if (tex != null) m.mainTexture = tex;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            return m;
        }

        public static Texture2D CreateAshlarStoneTexture(Color baseCol, Color runeCol, bool addRunes)
        {
            return CreateObsidianStonePair(baseCol, runeCol, addRunes, false, out _);
        }

        private static Texture2D CreateObsidianStonePair(Color baseCol, Color runeCol, bool horizontalBand, bool verticalRunes, out Texture2D emissionMap)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            emissionMap = new Texture2D(size, size, TextureFormat.RGBA32, true);
            System.Random rng = new System.Random(1337);

            for (int y = 0; y < size; y++)
            {
                int row = y / 32;
                int shift = (row % 2 == 0) ? 0 : 32;
                for (int x = 0; x < size; x++)
                {
                    float coarse = Mathf.PerlinNoise(x * 0.045f, y * 0.045f) * 0.08f - 0.04f;
                    float fine = (float)(rng.NextDouble() * 0.06 - 0.03);
                    float stoneVar = ((row * 17 + ((x + shift) / 64) * 31) % 7 - 3) * 0.012f;
                    float v = coarse + fine + stoneVar;

                    Color c = new Color(
                        Mathf.Clamp01(baseCol.r + v),
                        Mathf.Clamp01(baseCol.g + v * 0.85f),
                        Mathf.Clamp01(baseCol.b + v * 0.80f),
                        1f
                    );

                    bool mortar = (y % 32 < 3) || (((x + shift) % 64) < 3);
                    if (mortar) c *= 0.36f;

                    Color em = Color.black;

                    // Glowing carved molten line along cover blocks
                    if (horizontalBand && y >= 122 && y <= 126)
                    {
                        c = Color.Lerp(c, runeCol, 0.88f);
                        em = runeCol;
                    }

                    // Carved fiery vertical demonic glyphs matching the reference fortress masonry!
                    if (verticalRunes)
                    {
                        int gx = x % 128;
                        int gy = y % 64;
                        if (gx >= 56 && gx <= 72 && gy >= 10 && gy <= 52)
                        {
                            int lx = gx - 56;
                            int ly = gy - 10;
                            bool stroke =
                                (lx >= 7 && lx <= 9) ||                                       // central spine
                                (ly >= 8 && ly <= 10 && lx >= 2 && lx <= 14) ||               // upper cross rune
                                (ly >= 26 && ly <= 28 && lx >= 4 && lx <= 12) ||              // lower cross rune
                                (Mathf.Abs((lx - 8) - (ly - 16) / 2) <= 1 && ly >= 14 && ly <= 26) || // diagonal sigil
                                (Mathf.Abs((8 - lx) - (ly - 30) / 2) <= 1 && ly >= 30 && ly <= 40);
                            if (stroke)
                            {
                                c = Color.Lerp(c, runeCol, 0.92f);
                                em = runeCol;
                            }
                        }
                    }

                    tex.SetPixel(x, y, c);
                    emissionMap.SetPixel(x, y, em);
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            emissionMap.wrapMode = TextureWrapMode.Repeat;
            emissionMap.Apply();
            return tex;
        }

        public void BuildEnvironment()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediateSafe(transform.GetChild(i).gameObject);
            }
            worldRoot = null;
            RadarObstacles.Clear();
            portalShards.Clear();
            flameBillboards.Clear();
            swayingBanners.Clear();
            portalVortexPS = null;

            GameObject rootGo = new GameObject("CitadelWorldRoot");
            rootGo.transform.SetParent(transform, false);
            worldRoot = rootGo.transform;

            // =====================================================================
            // 1. 360-DEGREE CONTINUOUS ANIMATED VOLCANIC HELLSCAPE SKY-DOME BACKDROP
            // =====================================================================
            BuildVolcanicSkyBackdrop();

            // =====================================================================
            // 2. DARK CHARRED OBSIDIAN MATERIALS + GLOWING DEMONIC RUNES + MOLTEN LAVA
            // =====================================================================
            Texture2D wallEm, blockEm, floorEm;
            Texture2D wallTex = CreateObsidianStonePair(new Color(0.15f, 0.115f, 0.105f), new Color(1.0f, 0.24f, 0.08f), false, true, out wallEm);
            Texture2D blockTex = CreateObsidianStonePair(new Color(0.16f, 0.125f, 0.115f), new Color(1.0f, 0.30f, 0.08f), true, false, out blockEm);
            Texture2D floorTex = CreateObsidianStonePair(new Color(0.14f, 0.115f, 0.105f), new Color(0.95f, 0.28f, 0.05f), false, false, out floorEm);

            Material wallMat = MakeObsidianRuneMat(wallTex, wallEm, new Color(1.8f, 0.32f, 0.08f), 0.14f, 0.24f);
            wallMat.mainTextureScale = new Vector2(6f, 1.5f);
            if (wallMat.HasProperty("_EmissionMap")) wallMat.SetTextureScale("_EmissionMap", new Vector2(6f, 1.5f));

            Material blockMat = MakeObsidianRuneMat(blockTex, blockEm, new Color(1.6f, 0.38f, 0.08f), 0.22f, 0.32f);
            Material floorMat = MakeStdMat(Color.white, 0.10f, 0.22f, floorTex);
            floorMat.mainTextureScale = new Vector2(18f, 20f);

            Material darkIronMat = MakeStdMat(new Color(0.08f, 0.065f, 0.065f), 0.72f, 0.42f);
            Material obsidianTrimMat = MakeStdMat(new Color(0.10f, 0.075f, 0.075f), 0.28f, 0.36f);
            Material bannerRedMat = MakeStdMat(new Color(0.52f, 0.05f, 0.04f), 0.05f, 0.12f);

            Material lavaMat = CreateMoltenLavaMaterial();

            // =====================================================================
            // 3. SOLID OBSIDIAN ARENA FLOOR + FLOWING MOLTEN LAVA RIVERS & BRIDGE
            // =====================================================================
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(worldRoot, false);
            floor.transform.position = new Vector3(0f, -0.5f, -7f);
            floor.transform.localScale = new Vector3(56f, 1f, 64f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Continuous flowing molten lava canals lining the left & right edges + North Sanctuary lava moat
            CreateDecorativeMeshQuad("LavaCanal_Left", new Vector3(-22.2f, 0.025f, -5.5f), new Vector3(3.4f, 1f, 48f), lavaMat);
            CreateDecorativeMeshQuad("LavaCanal_Right", new Vector3(22.2f, 0.025f, -5.5f), new Vector3(3.4f, 1f, 48f), lavaMat);
            CreateDecorativeMeshQuad("LavaMoat_North", new Vector3(0f, 0.028f, -28.4f), new Vector3(47.8f, 1f, 4.6f), lavaMat);

            // Low charred stone curbs bordering the lava canals (no colliders so combat movement stays fluid!)
            AddDecorBlock("LavaCurb_L", new Vector3(-20.4f, 0.12f, -5.5f), new Vector3(0.45f, 0.24f, 46f), obsidianTrimMat);
            AddDecorBlock("LavaCurb_R", new Vector3(20.4f, 0.12f, -5.5f), new Vector3(0.45f, 0.24f, 46f), obsidianTrimMat);
            AddDecorBlock("MoatCurb_L", new Vector3(-13.8f, 0.14f, -26.0f), new Vector3(17.5f, 0.28f, 0.55f), obsidianTrimMat);
            AddDecorBlock("MoatCurb_R", new Vector3(13.8f, 0.14f, -26.0f), new Vector3(17.5f, 0.28f, 0.55f), obsidianTrimMat);

            // Obsidian Gothic Battle-Bridge crossing the North Lava Moat straight to the Diamond Portal!
            AddDecorBlock("PortalBridgeDeck", new Vector3(0f, 0.055f, -28.6f), new Vector3(9.4f, 0.11f, 5.4f), floorMat);
            AddDecorBlock("PortalBridgeRimL", new Vector3(-4.7f, 0.42f, -28.4f), new Vector3(0.55f, 0.78f, 5.4f), obsidianTrimMat);
            AddDecorBlock("PortalBridgeRimR", new Vector3(4.7f, 0.42f, -28.4f), new Vector3(0.55f, 0.78f, 5.4f), obsidianTrimMat);
            AddDecorBlock("PortalSanctuaryPlinth", new Vector3(0f, 0.06f, -32.2f), new Vector3(13.5f, 0.12f, 4.4f), obsidianTrimMat);

            // =====================================================================
            // 4. DARK OBSIDIAN PERIMETER FORTRESS WALLS + SPIKED GOTHIC BATTLEMENTS
            //    (Height 6.2m so the colossal flowing lava waterfalls tower above!)
            // =====================================================================
            AddBlock("NorthWall", new Vector3(0f, 3.1f, -35.5f), new Vector3(54f, 6.2f, 3f), wallMat, true);
            AddBlock("SouthWall", new Vector3(0f, 3.1f, 21.5f), new Vector3(54f, 6.2f, 3f), wallMat, true);
            AddBlock("LeftWall", new Vector3(-25.5f, 3.1f, -7f), new Vector3(3f, 6.2f, 58f), wallMat, true);
            AddBlock("RightWall", new Vector3(25.5f, 3.1f, -7f), new Vector3(3f, 6.2f, 58f), wallMat, true);

            BuildFortressBattlementsAndChains(obsidianTrimMat, darkIronMat, bannerRedMat);

            // =====================================================================
            // 5. SYMMETRICAL TACTICAL COVER BLOCKS (Exact Combat Layout Preserved!)
            // =====================================================================
            AddBlock("CoverBlock_FrontL", new Vector3(-10.5f, 1.25f, 4.5f), new Vector3(4.0f, 2.5f, 2.2f), blockMat, true);
            AddBlock("CoverBlock_FrontR", new Vector3(10.5f, 1.25f, 4.5f), new Vector3(4.0f, 2.5f, 2.2f), blockMat, true);

            AddBlock("CoverBlock_MidL", new Vector3(-11.0f, 1.4f, -3.5f), new Vector3(3.6f, 2.8f, 2.4f), blockMat, true);
            AddBlock("CoverBlock_MidR", new Vector3(11.0f, 1.4f, -3.5f), new Vector3(3.6f, 2.8f, 2.4f), blockMat, true);

            AddBlock("CoverBlock_CenterL", new Vector3(-5.5f, 1.1f, -9.5f), new Vector3(3.2f, 2.2f, 2.0f), blockMat, true);
            AddBlock("CoverBlock_CenterR", new Vector3(5.5f, 1.1f, -9.5f), new Vector3(3.2f, 2.2f, 2.0f), blockMat, true);

            AddBlock("CoverBlock_BackL", new Vector3(-13.5f, 1.6f, -24.5f), new Vector3(3.8f, 3.2f, 2.2f), blockMat, true);
            AddBlock("CoverBlock_BackR", new Vector3(13.5f, 1.6f, -24.5f), new Vector3(3.8f, 3.2f, 2.2f), blockMat, true);

            // =====================================================================
            // 6. FLAMING FORTRESS WALL BRAZIERS & BRIDGE TORCHES (Zero Light Gizmos!)
            // =====================================================================
            Vector3[] brazierPositions =
            {
                new Vector3(-5.0f, 0.9f, -26.1f),
                new Vector3( 5.0f, 0.9f, -26.1f),
                new Vector3(-8.5f, 1.0f, -33.2f),
                new Vector3( 8.5f, 1.0f, -33.2f),
                new Vector3(-23.2f, 2.8f, -18f),
                new Vector3( 23.2f, 2.8f, -18f),
                new Vector3(-23.2f, 2.8f,   2f),
                new Vector3( 23.2f, 2.8f,   2f)
            };
            for (int i = 0; i < brazierPositions.Length; i++)
            {
                BuildFlamingBrazier("HellBrazier_" + i, brazierPositions[i], darkIronMat);
            }

            // =====================================================================
            // 7. EPIC ANIMATED 3D OBSIDIAN DIAMOND ASCENSION PORTAL (North Wall!)
            // =====================================================================
            BuildObsidianDiamondPortal(obsidianTrimMat, darkIronMat, bannerRedMat);

            // =====================================================================
            // 8. CONTINUOUS ARENA-WIDE RISING VOLCANIC EMBERS (Soft Radial Glow!)
            // =====================================================================
            BuildArenaAmbientEmbers();
        }

        private void BuildVolcanicSkyBackdrop()
        {
            GameObject skyGo = new GameObject("VolcanicHellSkyDome");
            skyGo.transform.SetParent(worldRoot, false);
            skyGo.transform.position = new Vector3(0f, -10f, -7f);

            MeshFilter mf = skyGo.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateVolcanicSkyDomeMesh(94f, 88f, 40, 14);
            MeshRenderer mr = skyGo.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Shader sh = Resources.Load<Shader>("Shaders/HellBackdrop360");
            if (sh == null) sh = Shader.Find("Hidden/DoomHellBackdrop360");
            Texture2D backdropTex = Resources.Load<Texture2D>("World/hell_citadel_backdrop");
            if (sh != null)
            {
                backdropMat = new Material(sh);
                if (backdropTex != null)
                {
                    backdropTex.wrapMode = TextureWrapMode.Clamp;
                    backdropMat.mainTexture = backdropTex;
                }
                backdropMat.SetColor("_RealmTint", new Color(1f, 0.92f, 0.85f, 1f));
                mr.sharedMaterial = backdropMat;
            }
            else
            {
                mr.sharedMaterial = MakeUnlitMat(new Color(0.08f, 0.03f, 0.02f));
            }
        }

        private static Mesh CreateVolcanicSkyDomeMesh(float radius, float height, int segs, int rings)
        {
            Mesh m = new Mesh { name = "VolcanicSkyDomeMesh" };
            Vector3[] verts = new Vector3[(segs + 1) * (rings + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segs * rings * 6];

            int vi = 0;
            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                // Keep lower 68% nearly vertical so towering lava waterfalls rise straight behind the walls,
                // then curve smoothly inward overhead to close the stormy ash sky dome!
                float domeCurve = v > 0.65f ? Mathf.InverseLerp(0.65f, 1.0f, v) : 0f;
                float ringRad = radius * Mathf.Cos(domeCurve * Mathf.PI * 0.495f);
                float y = (v <= 0.65f)
                    ? Mathf.Lerp(0f, height * 0.68f, v / 0.65f)
                    : height * 0.68f + Mathf.Sin(domeCurve * Mathf.PI * 0.5f) * (height * 0.34f);

                for (int s = 0; s <= segs; s++)
                {
                    float u = (float)s / segs;
                    float ang = u * Mathf.PI * 2f;
                    verts[vi] = new Vector3(Mathf.Cos(ang) * ringRad, y, Mathf.Sin(ang) * ringRad);
                    uvs[vi] = new Vector2(u, v);
                    vi++;
                }
            }

            int ti = 0;
            int stride = segs + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segs; s++)
                {
                    int a = r * stride + s;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    // Inward-facing triangles
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = c;
                    tris[ti++] = b; tris[ti++] = d; tris[ti++] = c;
                }
            }

            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private Material CreateMoltenLavaMaterial()
        {
            Shader sh = Resources.Load<Shader>("Shaders/MoltenLavaFlow");
            if (sh == null) sh = Shader.Find("Hidden/DoomMoltenLavaFlow");
            if (sh != null)
            {
                Material m = new Material(sh);
                m.SetFloat("_FlowSpeed", 0.55f);
                m.SetFloat("_GlowMult", 1.35f);
                return m;
            }
            return MakeEmissiveMat(new Color(1.0f, 0.35f, 0.05f), 2.5f);
        }

        private void CreateDecorativeMeshQuad(string objName, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = objName;
            DestroyImmediateSafe(go.GetComponent<Collider>());
            go.transform.SetParent(worldRoot, false);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.z, 1f);
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void BuildFortressBattlementsAndChains(Material trimMat, Material ironMat, Material bannerMat)
        {
            // Jagged Gothic Battlements & Spires crowning all 4 fortress walls
            for (float x = -24f; x <= 24f; x += 6f)
            {
                if (Mathf.Abs(x) > 5.5f)
                {
                    AddDecorBlock("MerlonN_" + x, new Vector3(x, 6.95f, -34.3f), new Vector3(2.4f, 1.6f, 0.9f), trimMat);
                    AddDecorBlock("SpikeN_" + x, new Vector3(x, 8.2f, -34.3f), new Vector3(0.45f, 1.2f, 0.45f), ironMat);
                }
                AddDecorBlock("MerlonS_" + x, new Vector3(x, 6.95f, 20.3f), new Vector3(2.4f, 1.6f, 0.9f), trimMat);
                AddDecorBlock("SpikeS_" + x, new Vector3(x, 8.2f, 20.3f), new Vector3(0.45f, 1.2f, 0.45f), ironMat);
            }
            for (float z = -30f; z <= 18f; z += 6.5f)
            {
                AddDecorBlock("MerlonL_" + z, new Vector3(-24.3f, 6.95f, z), new Vector3(0.9f, 1.6f, 2.4f), trimMat);
                AddDecorBlock("SpikeL_" + z, new Vector3(-24.3f, 8.2f, z), new Vector3(0.45f, 1.2f, 0.45f), ironMat);
                AddDecorBlock("MerlonR_" + z, new Vector3(24.3f, 6.95f, z), new Vector3(0.9f, 1.6f, 2.4f), trimMat);
                AddDecorBlock("SpikeR_" + z, new Vector3(24.3f, 8.2f, z), new Vector3(0.45f, 1.2f, 0.45f), ironMat);
            }

            // Corner Gothic Bastion Towers (framing the arena silhouette like Image 2!)
            Vector3[] corners =
            {
                new Vector3(-24.8f, 5.2f, -34.5f),
                new Vector3( 24.8f, 5.2f, -34.5f),
                new Vector3(-24.8f, 5.2f,  20.5f),
                new Vector3( 24.8f, 5.2f,  20.5f)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                AddDecorBlock("CornerTower_" + i, corners[i], new Vector3(5.2f, 10.4f, 5.2f), trimMat);
                AddDecorBlock("CornerSpire_" + i, corners[i] + new Vector3(0f, 6.8f, 0f), new Vector3(1.1f, 3.6f, 1.1f), ironMat);
            }

            // Hanging Dark Iron Chains along the North & Side Walls
            Vector3[] chainAnchors =
            {
                new Vector3(-16.5f, 6.1f, -33.7f),
                new Vector3( 16.5f, 6.1f, -33.7f),
                new Vector3(-23.8f, 6.1f, -12.0f),
                new Vector3( 23.8f, 6.1f, -12.0f),
                new Vector3(-23.8f, 6.1f,   8.0f),
                new Vector3( 23.8f, 6.1f,   8.0f)
            };
            for (int i = 0; i < chainAnchors.Length; i++)
            {
                for (int link = 0; link < 7; link++)
                {
                    Vector3 lp = chainAnchors[i] + new Vector3(0f, -link * 0.55f, 0f);
                    GameObject l = AddDecorBlock("Chain_" + i + "_" + link, lp, new Vector3(0.16f, 0.42f, 0.24f), ironMat);
                    l.transform.localRotation = Quaternion.Euler(0f, (link % 2 == 0) ? 0f : 90f, 0f);
                }
            }

            // Crimson Gothic War Banners flanking the North Sanctuary
            CreateSwayingBanner("NorthBanner_L", new Vector3(-12.5f, 5.4f, -33.7f), new Vector3(2.2f, 3.8f, 0.08f), bannerMat);
            CreateSwayingBanner("NorthBanner_R", new Vector3( 12.5f, 5.4f, -33.7f), new Vector3(2.2f, 3.8f, 0.08f), bannerMat);
        }

        private void CreateSwayingBanner(string bName, Vector3 topAnchorPos, Vector3 size, Material mat)
        {
            GameObject pivot = new GameObject(bName);
            pivot.transform.SetParent(worldRoot, false);
            pivot.transform.position = topAnchorPos;

            GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = bName + "_Cloth";
            DestroyImmediateSafe(cloth.GetComponent<Collider>());
            cloth.transform.SetParent(pivot.transform, false);
            cloth.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            cloth.transform.localScale = size;
            cloth.GetComponent<Renderer>().sharedMaterial = mat;

            swayingBanners.Add(pivot.transform);
        }

        private void BuildFlamingBrazier(string bName, Vector3 basePos, Material ironMat)
        {
            GameObject root = new GameObject(bName);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = basePos;

            // Pedestal / Iron Bowl
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Bowl";
            DestroyImmediateSafe(bowl.GetComponent<Collider>());
            bowl.transform.SetParent(root.transform, false);
            bowl.transform.localPosition = Vector3.zero;
            bowl.transform.localScale = new Vector3(1.1f, 0.35f, 1.1f);
            bowl.GetComponent<Renderer>().sharedMaterial = ironMat;

            // Glowing coal bed
            GameObject coals = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coals.name = "Coals";
            DestroyImmediateSafe(coals.GetComponent<Collider>());
            coals.transform.SetParent(root.transform, false);
            coals.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            coals.transform.localScale = new Vector3(0.86f, 0.08f, 0.86f);
            coals.GetComponent<Renderer>().sharedMaterial = MakeEmissiveMat(new Color(1f, 0.42f, 0.06f), 3.0f);

            // Crossed soft-glow additive flames (zero opaque squares, zero PointLight gizmos!)
            Texture2D softGlow = GetSoftRadialGlowTexture();
            Material outerFlameMat = MakeAdditiveSoftMat(new Color(1.0f, 0.42f, 0.06f, 0.85f), softGlow);
            Material innerFlameMat = MakeAdditiveSoftMat(new Color(1.0f, 0.88f, 0.38f, 0.92f), softGlow);

            GameObject flameGroup = new GameObject("FlameGroup");
            flameGroup.transform.SetParent(root.transform, false);
            flameGroup.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            for (int k = 0; k < 2; k++)
            {
                GameObject fOuter = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediateSafe(fOuter.GetComponent<Collider>());
                fOuter.transform.SetParent(flameGroup.transform, false);
                fOuter.transform.localRotation = Quaternion.Euler(0f, k * 90f, 0f);
                fOuter.transform.localScale = new Vector3(1.45f, 1.95f, 1f);
                Renderer r1 = fOuter.GetComponent<Renderer>();
                r1.sharedMaterial = outerFlameMat;
                r1.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                GameObject fInner = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediateSafe(fInner.GetComponent<Collider>());
                fInner.transform.SetParent(flameGroup.transform, false);
                fInner.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                fInner.transform.localRotation = Quaternion.Euler(0f, k * 90f + 45f, 0f);
                fInner.transform.localScale = new Vector3(0.85f, 1.25f, 1f);
                Renderer r2 = fInner.GetComponent<Renderer>();
                r2.sharedMaterial = innerFlameMat;
                r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            flameBillboards.Add(flameGroup.transform);
        }

        private void BuildObsidianDiamondPortal(Material obsidianMat, Material ironMat, Material bannerMat)
        {
            GameObject portalRoot = new GameObject("CitadelAscensionPortal");
            portalRoot.transform.SetParent(worldRoot, false);
            portalRoot.transform.position = new Vector3(0f, 3.65f, -31.8f);
            portalRing = portalRoot.transform;

            portalRuneMat = MakeEmissiveMat(new Color(0.85f, 0.14f, 0.05f), 2.2f);

            // 1. Sculpted 3D Jagged Obsidian Diamond Gate Archway (Exact homage to center of Image 2!)
            Vector3[] sidePos =
            {
                new Vector3(-1.35f,  1.65f, 0f), // Top-Left bevel
                new Vector3( 1.35f,  1.65f, 0f), // Top-Right bevel
                new Vector3(-1.35f, -1.65f, 0f), // Bottom-Left bevel
                new Vector3( 1.35f, -1.65f, 0f)  // Bottom-Right bevel
            };
            float[] sideAngles = { 38f, -38f, -38f, 38f };
            for (int i = 0; i < 4; i++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "DiamondFrame_" + i;
                DestroyImmediateSafe(beam.GetComponent<Collider>());
                beam.transform.SetParent(portalRoot.transform, false);
                beam.transform.localPosition = sidePos[i];
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, sideAngles[i]);
                beam.transform.localScale = new Vector3(0.75f, 4.35f, 0.95f);
                beam.GetComponent<Renderer>().sharedMaterial = obsidianMat;

                // Glowing rune trim strip running along the inner edge of the obsidian diamond
                GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "DiamondRune_" + i;
                DestroyImmediateSafe(trim.GetComponent<Collider>());
                trim.transform.SetParent(beam.transform, false);
                trim.transform.localPosition = new Vector3((i % 2 == 0) ? 0.42f : -0.42f, 0f, 0.35f);
                trim.transform.localScale = new Vector3(0.18f, 0.92f, 0.28f);
                trim.GetComponent<Renderer>().sharedMaterial = portalRuneMat;
            }

            // Crown jagged obsidian apex horn + twin side guardian pillars
            AddChildDecor(portalRoot.transform, "DiamondCrownPeak", new Vector3(0f, 3.85f, 0f), new Vector3(0.9f, 1.55f, 0.9f), Quaternion.Euler(0, 0, 45f), obsidianMat);
            AddChildDecor(portalRoot.transform, "PortalPillarL", new Vector3(-3.35f, -0.95f, -0.35f), new Vector3(1.4f, 5.4f, 1.5f), Quaternion.identity, obsidianMat);
            AddChildDecor(portalRoot.transform, "PortalPillarR", new Vector3( 3.35f, -0.95f, -0.35f), new Vector3(1.4f, 5.4f, 1.5f), Quaternion.identity, obsidianMat);
            AddChildDecor(portalRoot.transform, "PortalHornL", new Vector3(-3.45f, 2.35f, -0.25f), new Vector3(0.42f, 1.65f, 0.42f), Quaternion.Euler(0, 0, 14f), ironMat);
            AddChildDecor(portalRoot.transform, "PortalHornR", new Vector3( 3.45f, 2.35f, -0.25f), new Vector3(0.42f, 1.65f, 0.42f), Quaternion.Euler(0, 0, -14f), ironMat);

            // 2. Inner Animated Diamond Rift Vortex (DemonicPortalVortex.shader!)
            GameObject vortexQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vortexQuad.name = "DiamondPortalVortex";
            DestroyImmediateSafe(vortexQuad.GetComponent<Collider>());
            vortexQuad.transform.SetParent(portalRoot.transform, false);
            vortexQuad.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            vortexQuad.transform.localScale = new Vector3(4.9f, 6.2f, 1f);
            portalRingRenderer = vortexQuad.GetComponent<Renderer>();
            portalRingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Shader vortexSh = Resources.Load<Shader>("Shaders/DemonicPortalVortex");
            if (vortexSh == null) vortexSh = Shader.Find("Hidden/DoomDemonicPortalVortex");
            if (vortexSh != null)
            {
                portalVortexMat = new Material(vortexSh);
                portalVortexMat.SetFloat("_UnlockProgress", 0f);
                portalVortexMat.SetFloat("_FlashBurst", 0f);
                portalRingRenderer.sharedMaterial = portalVortexMat;
            }
            else
            {
                portalRingRenderer.sharedMaterial = MakeEmissiveMat(new Color(0.85f, 0.14f, 0.05f), 2.5f);
            }

            // 3. Crisscrossed Crimson Seal Chains (Shatter dramatically when all enemies die!)
            Material sealMat = MakeEmissiveMat(new Color(0.95f, 0.14f, 0.06f), 2.8f);
            portalSealA = AddChildDecor(portalRoot.transform, "SealChainA", new Vector3(0f, 0f, 0.32f), new Vector3(0.18f, 4.6f, 0.18f), Quaternion.Euler(0, 0, 34f), sealMat).transform;
            portalSealB = AddChildDecor(portalRoot.transform, "SealChainB", new Vector3(0f, 0f, 0.36f), new Vector3(0.18f, 4.6f, 0.18f), Quaternion.Euler(0, 0, -34f), sealMat).transform;

            // 4. 8 Floating Orbiting Obsidian Rune Shards around the Diamond Gate
            GameObject orbitGo = new GameObject("PortalRuneOrbit");
            orbitGo.transform.SetParent(portalRoot.transform, false);
            orbitGo.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            portalOrbitRoot = orbitGo.transform;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * (Mathf.PI * 2f / 8f);
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "RuneShard_" + i;
                DestroyImmediateSafe(shard.GetComponent<Collider>());
                shard.transform.SetParent(portalOrbitRoot, false);
                shard.transform.localPosition = new Vector3(Mathf.Cos(ang) * 2.95f, Mathf.Sin(ang) * 3.55f, 0f);
                shard.transform.localRotation = Quaternion.Euler(25f, 45f, ang * Mathf.Rad2Deg);
                shard.transform.localScale = new Vector3(0.32f, 0.72f, 0.24f);
                shard.GetComponent<Renderer>().sharedMaterial = portalRuneMat;
                portalShards.Add(shard.transform);
            }

            // 5. Soaring Vertical Hellfire Ascension Sky Beam (Hidden until Portal Unseals!)
            Texture2D softGlow = GetSoftRadialGlowTexture();
            portalBeamMat = MakeAdditiveSoftMat(new Color(1.0f, 0.72f, 0.22f, 0f), softGlow);
            GameObject beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamGo.name = "PortalAscensionBeam";
            DestroyImmediateSafe(beamGo.GetComponent<Collider>());
            beamGo.transform.SetParent(portalRoot.transform, false);
            beamGo.transform.localPosition = new Vector3(0f, 22f, 0.2f);
            beamGo.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            Renderer beamR = beamGo.GetComponent<Renderer>();
            beamR.sharedMaterial = portalBeamMat;
            beamR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            portalSkyBeam = beamGo.transform;

            // 6. Expanding Molten-Gold Shockwave Burst Ring on Portal Unseal
            shockwaveMat = MakeAdditiveSoftMat(new Color(1.0f, 0.68f, 0.18f, 0f), softGlow);
            GameObject swGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            swGo.name = "PortalShockwave";
            DestroyImmediateSafe(swGo.GetComponent<Collider>());
            swGo.transform.SetParent(portalRoot.transform, false);
            swGo.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            swGo.transform.localScale = Vector3.zero;
            Renderer swR = swGo.GetComponent<Renderer>();
            swR.sharedMaterial = shockwaveMat;
            swR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            portalShockwave = swGo.transform;

            // 7. Continuous Swirling Portal Ember Vortex Particle System (Soft Circular Glow!)
            GameObject psGo = new GameObject("PortalVortexEmbers");
            psGo.transform.SetParent(portalRoot.transform, false);
            psGo.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            portalVortexPS = psGo.AddComponent<ParticleSystem>();
            var main = portalVortexPS.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
            main.startColor = new Color(1f, 0.28f, 0.08f, 0.85f);
            main.maxParticles = 140;
            var em = portalVortexPS.emission;
            em.rateOverTime = 14f;
            var shape = portalVortexPS.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 2.0f;
            shape.donutRadius = 0.35f;
            var vel = portalVortexPS.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalZ = 2.6f;
            vel.radial = -0.35f;
            var colLife = portalVortexPS.colorOverLifetime;
            colLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.32f, 0.06f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.25f), new GradientAlphaKey(0f, 1f) }
            );
            colLife.color = grad;
            ParticleSystemRenderer psr = portalVortexPS.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = MakeAdditiveSoftMat(new Color(1f, 0.65f, 0.25f, 0.9f), softGlow);
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Swaying Crimson War Banner hanging right on the bridge archway face beneath the portal
            CreateSwayingBanner("BridgeCenterBanner", new Vector3(0f, 0.55f, -25.85f), new Vector3(1.65f, 0.52f, 0.06f), bannerMat);

            // Reset portal visual state
            SetPortalUnlocked(false);
        }

        private void BuildArenaAmbientEmbers()
        {
            GameObject embGo = new GameObject("ArenaVolcanicEmbers");
            embGo.transform.SetParent(worldRoot, false);
            embGo.transform.position = new Vector3(0f, 0.6f, -7f);

            ParticleSystem ps = embGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.68f, 0.18f, 0.9f), new Color(1f, 0.24f, 0.06f, 0.85f));
            main.maxParticles = 160;
            main.gravityModifier = -0.06f;

            var em = ps.emission;
            em.rateOverTime = 26f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(48f, 1.5f, 54f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.65f;
            noise.frequency = 0.35f;

            var colLife = ps.colorOverLifetime;
            colLife.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.82f, 0.34f), 0f), new GradientColorKey(new Color(1f, 0.28f, 0.06f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colLife.color = g;

            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = MakeAdditiveSoftMat(new Color(1f, 0.65f, 0.22f, 0.9f), GetSoftRadialGlowTexture());
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

        private GameObject AddDecorBlock(string blockName, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = blockName;
            DestroyImmediateSafe(go.GetComponent<Collider>());
            go.transform.SetParent(worldRoot, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private GameObject AddChildDecor(Transform parent, string blockName, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = blockName;
            DestroyImmediateSafe(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public void ApplyStratumAtmosphere(StratumDef stratum)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Deep moody volcanic charcoal-crimson atmosphere matching Image 2 (never flat tan!)
            RenderSettings.fogColor = Color.Lerp(new Color(0.06f, 0.022f, 0.018f), stratum.fogColor * 0.45f, 0.25f);
            RenderSettings.fogDensity = 0.0085f;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.28f, 0.16f, 0.14f), stratum.ambientColor, 0.22f);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.04f, 0.015f, 0.014f);
            }
            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(new Color(1.0f, 0.62f, 0.38f), stratum.sunColor, 0.28f);
                sunLight.intensity = 1.28f;
            }
            if (backdropMat != null)
            {
                backdropMat.SetColor("_RealmTint", Color.Lerp(new Color(1f, 0.90f, 0.80f), stratum.sunColor, 0.25f));
            }
        }

        public void SetPortalUnlocked(bool unlocked)
        {
            bool justUnlocked = unlocked && !portalUnlockedState;
            portalUnlockedState = unlocked;

            if (!unlocked)
            {
                portalUnlockAnim = 0f;
                portalFlashBurst = 0f;
                shockwaveAnim = -1f;
                if (portalVortexMat != null)
                {
                    portalVortexMat.SetFloat("_UnlockProgress", 0f);
                    portalVortexMat.SetFloat("_FlashBurst", 0f);
                }
                if (portalSealA != null) { portalSealA.gameObject.SetActive(true); portalSealA.localPosition = new Vector3(0f, 0f, 0.32f); portalSealA.localScale = new Vector3(0.18f, 4.6f, 0.18f); }
                if (portalSealB != null) { portalSealB.gameObject.SetActive(true); portalSealB.localPosition = new Vector3(0f, 0f, 0.36f); portalSealB.localScale = new Vector3(0.18f, 4.6f, 0.18f); }
                if (portalSkyBeam != null) portalSkyBeam.localScale = new Vector3(1.6f, 0.01f, 1.6f);
                if (portalShockwave != null) portalShockwave.localScale = Vector3.zero;
                if (portalBeamMat != null && portalBeamMat.HasProperty("_TintColor"))
                    portalBeamMat.SetColor("_TintColor", new Color(1f, 0.72f, 0.22f, 0f));
                if (portalRuneMat != null && portalRuneMat.HasProperty("_EmissionColor"))
                    portalRuneMat.SetColor("_EmissionColor", new Color(0.85f, 0.14f, 0.05f) * 2.2f);
                if (portalVortexPS != null)
                {
                    var em = portalVortexPS.emission; em.rateOverTime = 14f;
                    var main = portalVortexPS.main; main.startColor = new Color(0.95f, 0.20f, 0.06f, 0.80f);
                    var vel = portalVortexPS.velocityOverLifetime; vel.orbitalZ = 2.0f;
                }
            }
            else if (justUnlocked)
            {
                // Dramatic Unseal Ignition Burst when all enemies are eliminated!
                portalFlashBurst = 1.85f;
                shockwaveAnim = 0f;
                if (portalVortexPS != null)
                {
                    var em = portalVortexPS.emission; em.rateOverTime = 72f;
                    var main = portalVortexPS.main; main.startColor = new Color(1.0f, 0.82f, 0.28f, 0.96f);
                    var vel = portalVortexPS.velocityOverLifetime; vel.orbitalZ = 6.8f;
                    portalVortexPS.Emit(48);
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            // 1. Animate Flaming Braziers continuously (organic fire dance!)
            for (int i = 0; i < flameBillboards.Count; i++)
            {
                Transform fg = flameBillboards[i];
                if (fg == null) continue;
                float fl = 0.86f + 0.20f * Mathf.Sin(t * 9.5f + i * 1.7f) + 0.10f * Mathf.Cos(t * 15.2f + i * 2.9f);
                float sx = 0.94f + 0.10f * Mathf.Sin(t * 7.2f + i * 2.1f);
                fg.localScale = new Vector3(sx, fl, sx);
            }

            // 2. Gently sway Crimson Gothic War Banners in the furnace draft
            for (int i = 0; i < swayingBanners.Count; i++)
            {
                Transform b = swayingBanners[i];
                if (b == null) continue;
                float sway = Mathf.Sin(t * 2.1f + i * 1.4f) * 4.5f + Mathf.Cos(t * 3.6f + i) * 1.8f;
                b.localRotation = Quaternion.Euler(sway, 0f, sway * 0.35f);
            }

            // 3. Animate the Obsidian Diamond Ascension Portal (Sealed -> Unseal Burst -> Continuous Swirling Vortex!)
            if (portalUnlockedState)
            {
                portalUnlockAnim = Mathf.MoveTowards(portalUnlockAnim, 1f, dt * 0.95f);
            }
            if (portalFlashBurst > 0f)
            {
                portalFlashBurst = Mathf.Max(0f, portalFlashBurst - dt * 1.65f);
            }

            if (portalVortexMat != null)
            {
                portalVortexMat.SetFloat("_UnlockProgress", portalUnlockAnim);
                portalVortexMat.SetFloat("_FlashBurst", portalFlashBurst);
            }

            // Shatter & dissolve Seal Chains during unlock transition
            if (portalSealA != null && portalSealB != null)
            {
                if (portalUnlockAnim <= 0.01f)
                {
                    float pulse = 0.96f + 0.06f * Mathf.Sin(t * 3.2f);
                    portalSealA.localScale = new Vector3(0.18f * pulse, 4.6f, 0.18f * pulse);
                    portalSealB.localScale = new Vector3(0.18f * pulse, 4.6f, 0.18f * pulse);
                }
                else if (portalUnlockAnim < 0.45f)
                {
                    float s = 1f - (portalUnlockAnim / 0.45f);
                    portalSealA.localScale = new Vector3(0.18f * s, 4.6f * s, 0.18f * s);
                    portalSealB.localScale = new Vector3(0.18f * s, 4.6f * s, 0.18f * s);
                    portalSealA.localPosition = new Vector3(-portalUnlockAnim * 2.2f, portalUnlockAnim * 1.2f, 0.32f);
                    portalSealB.localPosition = new Vector3( portalUnlockAnim * 2.2f, portalUnlockAnim * 1.2f, 0.36f);
                }
                else
                {
                    if (portalSealA.gameObject.activeSelf) portalSealA.gameObject.SetActive(false);
                    if (portalSealB.gameObject.activeSelf) portalSealB.gameObject.SetActive(false);
                }
            }

            // Orbiting Obsidian Rune Shards (slow menacing drift when sealed -> blazing golden halo spin when unlocked!)
            if (portalOrbitRoot != null)
            {
                float orbitSpeed = Mathf.Lerp(18f, 115f, portalUnlockAnim);
                portalOrbitRoot.Rotate(0f, 0f, orbitSpeed * dt);
                float radiusMult = Mathf.Lerp(1.0f, 1.18f, portalUnlockAnim) + Mathf.Sin(t * 3.2f) * 0.04f;
                for (int i = 0; i < portalShards.Count; i++)
                {
                    Transform sh = portalShards[i];
                    if (sh == null) continue;
                    float ang = i * (Mathf.PI * 2f / 8f);
                    float bob = Mathf.Sin(t * 3.8f + i * 0.9f) * 0.16f;
                    sh.localPosition = new Vector3(Mathf.Cos(ang) * 2.95f * radiusMult, Mathf.Sin(ang) * 3.55f * radiusMult, bob);
                    sh.Rotate(0f, (portalUnlockedState ? 180f : 40f) * dt, 0f, Space.Self);
                }
            }

            // Glowing portal diamond runes intensify from dark crimson -> blazing molten gold!
            if (portalRuneMat != null && portalRuneMat.HasProperty("_EmissionColor"))
            {
                float pGlow = 0.85f + 0.25f * Mathf.Sin(t * (portalUnlockedState ? 5.5f : 2.2f));
                Color c = Color.Lerp(new Color(0.85f, 0.14f, 0.05f) * 2.2f, new Color(1.0f, 0.78f, 0.24f) * 4.5f, portalUnlockAnim) * pGlow;
                portalRuneMat.SetColor("_EmissionColor", c);
            }

            // Soaring Vertical Hellfire Ascension Sky Beam shoots upward & pulses continuously when unlocked!
            if (portalSkyBeam != null && portalBeamMat != null)
            {
                float beamHeight = Mathf.Lerp(0.01f, 24f, portalUnlockAnim);
                float beamRad = Mathf.Lerp(0.4f, 1.85f, portalUnlockAnim) * (0.92f + 0.12f * Mathf.Sin(t * 5.0f));
                portalSkyBeam.localScale = new Vector3(beamRad, beamHeight, beamRad);
                portalSkyBeam.localPosition = new Vector3(0f, beamHeight, 0.2f);
                if (portalBeamMat.HasProperty("_TintColor"))
                {
                    float alpha = portalUnlockAnim * (0.38f + 0.14f * Mathf.Sin(t * 4.5f));
                    portalBeamMat.SetColor("_TintColor", new Color(1.0f, 0.72f, 0.22f, alpha));
                }
            }

            // Expanding Molten-Gold Shockwave Blast Ring on Unlock
            if (shockwaveAnim >= 0f && portalShockwave != null && shockwaveMat != null)
            {
                shockwaveAnim += dt * 1.15f;
                if (shockwaveAnim >= 1f)
                {
                    shockwaveAnim = -1f;
                    portalShockwave.localScale = Vector3.zero;
                }
                else
                {
                    float ease = 1f - Mathf.Pow(1f - shockwaveAnim, 3f);
                    float sz = Mathf.Lerp(1.5f, 26f, ease);
                    portalShockwave.localScale = new Vector3(sz, sz, 1f);
                    if (shockwaveMat.HasProperty("_TintColor"))
                        shockwaveMat.SetColor("_TintColor", new Color(1.0f, 0.74f, 0.22f, (1f - shockwaveAnim) * 0.85f));
                }
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
