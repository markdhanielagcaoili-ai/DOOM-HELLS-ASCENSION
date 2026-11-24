Shader "Hidden/DoomMoltenLavaFlow"
{
    Properties
    {
        _FlowSpeed ("Flow Speed", Float) = 0.55
        _GlowMult ("Glow Multiplier", Float) = 1.35
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _FlowSpeed;
            float _GlowMult;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 34.5);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i), hash21(i + float2(1, 0)), f.x),
                    lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x),
                    f.y
                );
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.05 + float2(13.4, 7.1);
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                float2 p = i.worldPos.xz * 0.24;
                // Flowing domain-warped molten magma rivers with dark cooling basalt crust!
                float2 warp = float2(
                    fbm(p + float2(0.0, t * 0.9)),
                    fbm(p + float2(4.2, -t * 0.7))
                );
                float n = fbm(p + warp * 1.45 + float2(0.0, t * 1.15));
                float pulse = 0.88 + 0.12 * sin(_Time.y * 2.4 + p.x * 1.8 + p.y);

                // Dark charred crust -> deep glowing crimson -> blazing molten orange -> incandescent yellow-white core
                float crust = smoothstep(0.32, 0.72, n);
                float hotCore = smoothstep(0.58, 0.84, n);

                fixed3 darkRock = fixed3(0.09, 0.025, 0.015);
                fixed3 deepRed  = fixed3(0.82, 0.14, 0.02);
                fixed3 magmaOrg = fixed3(1.00, 0.48, 0.05);
                fixed3 whiteHot = fixed3(1.00, 0.90, 0.42);

                fixed3 col = lerp(darkRock, deepRed, smoothstep(0.24, 0.52, n));
                col = lerp(col, magmaOrg, crust);
                col = lerp(col, whiteHot, hotCore);
                col *= pulse * _GlowMult;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
