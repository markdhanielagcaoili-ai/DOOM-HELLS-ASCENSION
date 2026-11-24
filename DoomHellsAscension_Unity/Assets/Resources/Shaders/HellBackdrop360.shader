Shader "Hidden/DoomHellBackdrop360"
{
    Properties
    {
        _MainTex ("Hell Backdrop", 2D) = "black" {}
        _RealmTint ("Realm Tint", Color) = (1, 0.92, 0.85, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _RealmTint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                // Seamless mirrored 360-degree wraparound (3 repeats around the horizon cylinder)
                float rawU = frac(i.uv.x * 3.0);
                float u = 1.0 - abs(rawU * 2.0 - 1.0);
                float v = saturate(i.uv.y);

                // Remap cylinder V so the upper smoky sky blends cleanly overhead while waterfalls sit majestically above the fortress walls
                float imgV = saturate((v - 0.04) / 0.78);
                float2 baseUV = float2(u, imgV);
                fixed4 baseCol = tex2D(_MainTex, baseUV);

                // Detect glowing molten lava waterfalls & lava rivers in the backdrop artwork
                float lavaMask = smoothstep(0.42, 0.85, baseCol.r - baseCol.b * 0.65)
                               * smoothstep(0.14, 0.58, baseCol.g + baseCol.r * 0.25);

                // 1. CONTINUOUSLY FLOWING MOLTEN LAVA WATERFALLS (downward advection + heat shimmer!)
                float fallWave = sin(u * 65.0 + t * 2.6 + imgV * 18.0) * 0.0045;
                float2 lavaUV = float2(saturate(u + fallWave * lavaMask), saturate(imgV + frac(t * 0.24) * 0.065 * lavaMask));
                fixed4 lavaSample = tex2D(_MainTex, lavaUV);
                float lavaNoise = vnoise(float2(u * 26.0, imgV * 14.0 + t * 2.6)) * 0.55
                                + vnoise(float2(u * 52.0 - 1.7, imgV * 28.0 + t * 4.4)) * 0.45;
                fixed3 col = lerp(baseCol.rgb, max(baseCol.rgb, lavaSample.rgb), lavaMask * 0.65);
                col += fixed3(1.0, 0.46, 0.08) * lavaMask * (0.28 + 0.52 * lavaNoise);

                // 2. CONTINUOUSLY BILLOWING VOLCANIC ASH SMOKE CLOUDS in upper sky
                float smokeMask = smoothstep(0.45, 0.92, v);
                float s1 = vnoise(float2(i.uv.x * 12.0 + t * 0.06, v * 5.0 - t * 0.08));
                float s2 = vnoise(float2(i.uv.x * 24.0 - t * 0.09, v * 9.0 - t * 0.14));
                float smoke = s1 * 0.6 + s2 * 0.4;
                // Smooth top-of-dome blend into churning fiery ash sky
                float topFade = smoothstep(0.70, 0.96, v);
                fixed3 stormSky = lerp(fixed3(0.06, 0.025, 0.02), fixed3(0.24, 0.08, 0.03), smoke);
                col = lerp(col, stormSky, topFade);
                col += fixed3(0.22, 0.075, 0.025) * smoke * smokeMask * 0.42;

                // 3. CONTINUOUS RISING VOLCANIC FIRE EMBERS across the entire 360-degree sky!
                float2 eGrid = float2(i.uv.x * 48.0 + sin(v * 8.0 + t * 0.9) * 0.35, v * 18.0 - t * 1.15);
                float2 eCell = floor(eGrid);
                float2 eFrac = frac(eGrid) - 0.5;
                float rnd = hash21(eCell);
                if (rnd > 0.74)
                {
                    float2 jitter = float2(hash21(eCell + 7.1) - 0.5, hash21(eCell + 19.3) - 0.5) * 0.55;
                    float dist = length(eFrac - jitter);
                    float spark = smoothstep(0.16, 0.02, dist) * (0.55 + 0.45 * sin(t * 7.5 + rnd * 40.0));
                    col += fixed3(1.0, 0.62, 0.16) * spark * smoothstep(0.98, 0.25, v) * 0.95;
                }

                // Subtle realm atmosphere tint while preserving rich fiery lava contrast
                col *= lerp(fixed3(1, 1, 1), _RealmTint.rgb, 0.24);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
