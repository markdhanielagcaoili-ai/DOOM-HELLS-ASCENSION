using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// Iconic VALORANT Reaver Vandal Soul-Pillar Kill Finisher (matches 1:08-1:12 in reference video):
    /// Expands a rotating dark-violet arcane rune circle on the ground, unleashes a towering glowing
    /// electric-violet Soul Pillar beam with rising rings & shadow tendrils on Boss/Final kills,
    /// then collapses cleanly into the abyssal portal.
    /// </summary>
    public class ReaverFinisherFX : MonoBehaviour
    {
        private Transform runeCircle;
        private Transform pillarOuter;
        private Transform pillarCore;
        private Transform[] risingRings;
        private Material circleMat;
        private Material outerMat;
        private Material coreMat;
        private Material ringMat;

        private bool fullPillar;
        private float elapsed;
        private float duration;

        public void Init(Texture2D runeCircleTex, bool isFullPillar)
        {
            fullPillar = isFullPillar;
            duration = fullPillar ? 2.35f : 0.78f;

            Shader spriteSh = Shader.Find("Sprites/Default");
            if (spriteSh == null) spriteSh = Shader.Find("Unlit/Transparent");

            // 1. Rotating Dark-Purple Arcane Rune Circle on Arena Floor (1:08)
            GameObject circleGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circleGo.name = "ReaverRuneCircle";
            Destroy(circleGo.GetComponent<Collider>());
            circleGo.transform.SetParent(transform, false);
            circleGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            circleGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            circleGo.transform.localScale = Vector3.one * 0.2f;
            circleMat = new Material(spriteSh);
            circleMat.mainTexture = runeCircleTex;
            circleMat.color = new Color(1f, 0.92f, 1f, 0.95f);
            circleGo.GetComponent<Renderer>().material = circleMat;
            runeCircle = circleGo.transform;

            if (fullPillar)
            {
                // 2. Towering Glowing Electric-Violet Soul Pillar Beam (1:10)
                GameObject outerGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                outerGo.name = "SoulPillarOuter";
                Destroy(outerGo.GetComponent<Collider>());
                outerGo.transform.SetParent(transform, false);
                outerGo.transform.localPosition = new Vector3(0f, 9.0f, 0f);
                outerGo.transform.localScale = new Vector3(0.05f, 9.0f, 0.05f);
                outerMat = new Material(spriteSh);
                outerMat.color = new Color(0.56f, 0.16f, 1.0f, 0.48f);
                outerGo.GetComponent<Renderer>().material = outerMat;
                pillarOuter = outerGo.transform;

                GameObject coreGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coreGo.name = "SoulPillarCore";
                Destroy(coreGo.GetComponent<Collider>());
                coreGo.transform.SetParent(transform, false);
                coreGo.transform.localPosition = new Vector3(0f, 9.0f, 0f);
                coreGo.transform.localScale = new Vector3(0.02f, 9.0f, 0.02f);
                coreMat = new Material(spriteSh);
                coreMat.color = new Color(0.92f, 0.76f, 1.0f, 0.85f);
                coreGo.GetComponent<Renderer>().material = coreMat;
                pillarCore = coreGo.transform;

                // 3. Ascending Spectral Energy Rings swirling up the Soul Pillar!
                ringMat = new Material(spriteSh);
                ringMat.mainTexture = runeCircleTex;
                ringMat.color = new Color(0.84f, 0.50f, 1.0f, 0.78f);
                risingRings = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject rGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    rGo.name = $"SoulRing_{i}";
                    Destroy(rGo.GetComponent<Collider>());
                    rGo.transform.SetParent(transform, false);
                    rGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                    rGo.transform.localRotation = Quaternion.Euler(90f, i * 45f, 0f);
                    rGo.transform.localScale = Vector3.one * 2.4f;
                    rGo.GetComponent<Renderer>().material = ringMat;
                    risingRings[i] = rGo.transform;
                }
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));

            if (runeCircle != null)
            {
                runeCircle.Rotate(0f, 0f, (fullPillar ? 95f : 140f) * Time.deltaTime, Space.Self);
                float maxRadius = fullPillar ? 4.4f : 2.0f;
                float expand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f));
                float shrink = (t > 0.76f) ? Mathf.SmoothStep(1f, 0f, (t - 0.76f) / 0.24f) : 1f;
                float sc = maxRadius * expand * shrink;
                runeCircle.localScale = new Vector3(sc, sc, 1f);
                if (circleMat != null)
                {
                    float alpha = (t > 0.74f) ? Mathf.Clamp01((1f - t) / 0.26f) : 0.96f;
                    circleMat.color = new Color(1f, 0.90f, 1f, alpha);
                }
            }

            if (fullPillar && pillarOuter != null && pillarCore != null)
            {
                // 0.00-0.14: Rune circle opens -> 0.14-0.72: Giant Violet Soul Pillar erupts! -> 0.72-1.00: Collapses!
                float beamAmp = 0f;
                if (t > 0.12f && t < 0.88f)
                {
                    float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.12f) / 0.14f));
                    float down = (t > 0.64f) ? Mathf.SmoothStep(1f, 0f, (t - 0.64f) / 0.24f) : 1f;
                    beamAmp = up * down;
                }
                float pulse = 1f + 0.10f * Mathf.Sin(elapsed * 28f);
                pillarOuter.localScale = new Vector3(2.25f * beamAmp * pulse, 9.5f, 2.25f * beamAmp * pulse);
                pillarCore.localScale = new Vector3(0.92f * beamAmp * pulse, 9.5f, 0.92f * beamAmp * pulse);
                if (outerMat != null) outerMat.color = new Color(0.56f, 0.16f, 1.0f, 0.46f * beamAmp);
                if (coreMat != null) coreMat.color = new Color(0.95f, 0.82f, 1.0f, 0.88f * beamAmp);

                if (risingRings != null)
                {
                    for (int i = 0; i < risingRings.Length; i++)
                    {
                        if (risingRings[i] == null) continue;
                        float rt = Mathf.Repeat(elapsed * 1.15f + i * 0.33f, 1f);
                        risingRings[i].localPosition = new Vector3(0f, 0.4f + rt * 11.5f, 0f);
                        float rs = Mathf.Lerp(2.8f, 1.4f, rt) * beamAmp;
                        risingRings[i].localScale = new Vector3(rs, rs, 1f);
                        risingRings[i].Rotate(0f, 0f, -150f * Time.deltaTime, Space.Self);
                    }
                    if (ringMat != null) ringMat.color = new Color(0.84f, 0.50f, 1.0f, 0.76f * beamAmp);
                }
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
