Shader "Hidden/DoomDemonicPortalVortex"
{
    Properties
    {
        _UnlockProgress ("Unlock Progress", Range(0, 1)) = 0
        _FlashBurst ("Unlock Flash Burst", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _UnlockProgress;
            float _FlashBurst;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float u = saturate(_UnlockProgress);

                // Diamond-shaped SDF coordinates matching the obsidian diamond rift in the reference!
                float2 q = i.uv * 2.0 - 1.0;
                float diamondDist = abs(q.x) * 1.15 + abs(q.y) * 0.92;
                float insideMask = smoothstep(1.02, 0.84, diamondDist);
                if (insideMask <= 0.005) discard;

                // Polar coordinates for continuous swirling vortex animation
                float r = length(q * float2(1.1, 0.85));
                float ang = atan2(q.y, q.x);
                float spinSpeed = lerp(0.85, 3.6, u);
                float spiral = ang + r * lerp(3.5, 8.2, u) - t * spinSpeed;

                float n1 = vnoise(float2(cos(spiral) * 2.2 + t * 0.4, sin(spiral) * 2.2 - t * 0.6) + r * 3.0);
                float n2 = vnoise(float2(r * 6.5 - t * lerp(1.0, 3.2, u), ang * 2.5 + t * 0.9));
                float swirl = saturate(n1 * 0.55 + n2 * 0.55);

                // Rotating concentric arcane energy rings inside the unlocked vortex
                float ringWave = 0.5 + 0.5 * sin(r * 18.0 - t * lerp(2.0, 7.5, u));
                float rimGlow = smoothstep(0.45, 0.95, diamondDist);
                float coreGlow = exp(-r * r * lerp(4.5, 2.4, u));

                // SEALED STATE (dark brooding obsidian void with dim crimson lava rim)
                fixed3 sealedCol = lerp(fixed3(0.04, 0.008, 0.012), fixed3(0.42, 0.05, 0.02), swirl * 0.65);
                sealedCol += fixed3(0.75, 0.12, 0.04) * rimGlow * (0.55 + 0.35 * sin(t * 2.5 + ang * 2.0));

                // UNSEALED ACTIVE STATE (raging molten-gold, fiery orange & crimson Ascension vortex!)
                fixed3 openCol = lerp(fixed3(0.55, 0.06, 0.02), fixed3(1.0, 0.44, 0.06), swirl);
                openCol = lerp(openCol, fixed3(1.0, 0.84, 0.34), saturate(ringWave * 0.55 * swirl + coreGlow * 0.85));
                openCol += fixed3(1.0, 0.95, 0.72) * coreGlow * 0.75;
                openCol += fixed3(1.0, 0.52, 0.12) * rimGlow * (0.75 + 0.35 * sin(t * 6.0 - ang * 4.0));

                fixed3 col = lerp(sealedCol, openCol, u);
                col += fixed3(1.0, 0.88, 0.52) * _FlashBurst * (0.55 + 0.45 * coreGlow);

                float alpha = insideMask * lerp(0.92, 0.98, u);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
