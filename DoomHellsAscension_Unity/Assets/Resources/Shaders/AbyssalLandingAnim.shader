Shader "Hidden/DoomAbyssalLandingAnim"
{
    Properties
    {
        _MainTex ("Landing Texture", 2D) = "white" {}
        _AnimTime ("Animation Time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _AnimTime;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float gauss(float2 p, float2 c, float2 r)
            {
                float2 d = (p - c) / r;
                return exp(-dot(d, d));
            }

            float2 rotAround(float2 p, float2 pivot, float ang)
            {
                float s = sin(ang), c = cos(ang);
                float2 d = p - pivot;
                return pivot + float2(d.x * c - d.y * s, d.x * s + d.y * c);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _AnimTime;
                // Convert Unity bottom-left UV to top-down (0=top, 1=bottom) for exact landmark alignment
                float2 p = float2(i.uv.x, 1.0 - i.uv.y);
                float2 uv = p;

                // Protect DEVIL: ABYSSAL REIGN title & Slayer silhouette from sky cloud drift
                float titleMask = gauss(p, float2(0.495, 0.165), float2(0.185, 0.125));
                float slayerProtect = gauss(p, float2(0.505, 0.505), float2(0.135, 0.220));
                float skyMask = smoothstep(0.52, 0.14, p.y) * (1.0 - clamp(titleMask * 1.45, 0.0, 1.0)) * (1.0 - clamp(slayerProtect * 1.3, 0.0, 1.0));

                // 1. LIVING BACKGROUND: Drifting apocalyptic storm clouds + atmospheric heat waves
                float cloudWave = sin(p.y * 18.0 + t * 0.75) * 0.0042 + cos(p.x * 11.0 - t * 0.55) * 0.0032;
                uv.x -= (cloudWave + sin(t * 0.42 + p.y * 7.0) * 0.0038) * skyMask;
                uv.y += sin(p.x * 14.0 + t * 0.65) * 0.0018 * skyMask;

                // 2. Left Fortress Hanging Crimson Banner wind cloth ripple
                float bannerMask = gauss(p, float2(0.145, 0.345), float2(0.056, 0.145));
                float bannerHang = smoothstep(0.19, 0.44, p.y);
                uv.x += sin(p.y * 30.0 - t * 3.3) * 0.0085 * bannerMask * bannerHang;
                uv.y += cos(p.y * 22.0 - t * 3.3) * 0.0022 * bannerMask * bannerHang;

                // 3. Left Hanging Heavy Iron Chain gentle pendulum sway
                float chainMask = gauss(p, float2(0.232, 0.405), float2(0.045, 0.095));
                uv.x += sin(t * 1.65 + 0.5) * 0.0048 * chainMask;

                // 4. Right Citadel Crimson Banner cloth ripple
                float citBanner = gauss(p, float2(0.746, 0.388), float2(0.032, 0.092));
                uv.x += sin(p.y * 34.0 - t * 3.6 + 1.2) * 0.0048 * citBanner;

                // 5. Floating Island / Dark Monolith slow levitation bob
                float floatIsland = gauss(p, float2(0.628, 0.415), float2(0.032, 0.048));
                uv.y += sin(t * 1.35) * 0.0065 * floatIsland;

                // 6. Upper-Right & Lower-Right Winged Gargoyles hovering + flapping wings!
                float g1 = gauss(p, float2(0.903, 0.228), float2(0.044, 0.044));
                float g1Wing = abs(p.x - 0.903) * 14.0;
                uv.y += (sin(t * 2.7) * 0.0095 + sin(t * 5.4) * 0.009 * g1Wing) * g1;
                uv.x += cos(t * 1.35) * 0.0045 * g1;

                float g2 = gauss(p, float2(0.858, 0.652), float2(0.056, 0.066));
                float g2Wing = abs(p.x - 0.858) * 11.0;
                uv.y += (sin(t * 2.3 + 1.3) * 0.0085 + sin(t * 4.6 + 1.3) * 0.008 * g2Wing) * g2;

                // 7. Foreground & Midground Demon Horde snarling breath
                float dRight = gauss(p, float2(0.946, 0.665), float2(0.065, 0.115));
                uv.y += sin(t * 2.0 + 0.7) * 0.0055 * dRight;
                float dMid = gauss(p, float2(0.732, 0.748), float2(0.060, 0.075));
                uv.y += sin(t * 2.2 + 2.1) * 0.0050 * dMid;
                float dLeft = gauss(p, float2(0.165, 0.715), float2(0.055, 0.070));
                uv.y += sin(t * 1.9 + 3.0) * 0.0048 * dLeft;

                // 8. Citadel Magma Furnace rising heat-haze distortion
                float magmaHaze = gauss(p, float2(0.738, 0.605), float2(0.058, 0.095))
                                + gauss(p, float2(0.916, 0.455), float2(0.045, 0.075));
                uv.x += sin(p.y * 65.0 - t * 5.2) * 0.0028 * magmaHaze;

                // 9. THE DOOM SLAYER ("YUNG TAO"): Heavy Combat Breathing + Helmet Scan + Dual Weapon Sway!
                float breath = sin(t * 1.55);
                float breathLag = sin(t * 1.55 - 0.48);
                float breathLead = sin(t * 1.55 + 0.42);
                float footAnchor = smoothstep(0.74, 0.56, p.y); // Boots stay 100% locked onto the skull rock!

                // Torso & Broad Armored Shoulders breathing rise + chest expansion
                float torsoMask = gauss(p, float2(0.502, 0.445), float2(0.088, 0.125)) * footAnchor;
                uv.y += breath * 0.0115 * torsoMask;
                uv.x -= (p.x - 0.502) * breath * 0.052 * torsoMask;

                // Helmet & Visor subtle combat readiness nod & scan
                float headMask = gauss(p, float2(0.508, 0.342), float2(0.044, 0.056));
                uv.y += sin(t * 1.55 - 0.22) * 0.0105 * headMask;
                uv.x += sin(t * 0.85) * 0.0038 * headMask;

                // Right Arm + Heavy Super Shotgun pivoting from right shoulder
                float rArmMask = gauss(p, float2(0.408, 0.575), float2(0.068, 0.155)) * smoothstep(0.77, 0.62, p.y);
                float2 rRot = rotAround(p, float2(0.446, 0.425), breathLag * 0.024);
                uv += (rRot - p) * rArmMask;
                uv.y += breathLag * 0.0075 * rArmMask;

                // Left Arm + Glowing Crucible Blade pivoting from left elbow + crackling fire shimmer
                float lArmMask = gauss(p, float2(0.596, 0.595), float2(0.070, 0.128)) * smoothstep(0.75, 0.60, p.y);
                float2 lRot = rotAround(p, float2(0.548, 0.505), -breathLead * 0.028);
                uv += (lRot - p) * lArmMask;
                uv.y += breathLead * 0.0068 * lArmMask;
                float bladeMask = gauss(p, float2(0.614, 0.635), float2(0.048, 0.065));
                uv.x += sin(p.y * 72.0 - t * 6.8) * 0.0022 * bladeMask;

                // 10. Blazing fire shimmer on top flames of DEVIL title
                float fireTopMask = gauss(p, float2(0.495, 0.115), float2(0.165, 0.068));
                float2 sampleUV = clamp(uv, float2(0.002, 0.002), float2(0.998, 0.998));
                fixed4 col = tex2D(_MainTex, float2(sampleUV.x, 1.0 - sampleUV.y));

                // Apply fiery wave only to bright orange flame pixels on the title so letters stay razor sharp!
                float fireLum = smoothstep(0.45, 0.88, col.r - col.b * 0.5) * fireTopMask;
                if (fireLum > 0.02)
                {
                    float2 fuv = sampleUV + float2(sin(p.y * 55.0 - t * 6.5) * 0.0035, -cos(p.x * 42.0 + t * 5.2) * 0.0025) * fireLum;
                    fuv = clamp(fuv, float2(0.002, 0.002), float2(0.998, 0.998));
                    col = lerp(col, tex2D(_MainTex, float2(fuv.x, 1.0 - fuv.y)), fireLum * 0.85);
                }

                // 11. DYNAMIC LIGHTING PULSE OVERLAYS (Crucible Blade, Visor, Sky Beam, Magma Arch, Demon Eyes, Title Fire)
                float pulse1 = 0.5 + 0.5 * sin(t * 3.1);
                float pulse2 = 0.5 + 0.5 * sin(t * 2.3 + 1.1);
                float flicker = 0.5 + 0.5 * sin(t * 7.4) * cos(t * 4.3);

                // Crucible Blade crimson energy glow
                float bladeGlow = gauss(p, float2(0.612, 0.632), float2(0.065, 0.080));
                col.rgb += float3(0.55, 0.11, 0.04) * bladeGlow * (0.28 + 0.32 * pulse1);

                // Slayer Visor cyan HUD shimmer
                float visorGlow = gauss(p, float2(0.515, 0.348), float2(0.024, 0.020));
                col.rgb += float3(0.10, 0.52, 0.65) * visorGlow * (0.22 + 0.25 * pulse2);

                // Citadel Vertical Ascension Sky Beam surging energy
                float beamX = exp(-pow((p.x - 0.742) / 0.018, 2.0)) * smoothstep(0.32, 0.02, p.y);
                float beamSurge = 0.65 + 0.35 * sin(p.y * 28.0 + t * 5.5);
                col.rgb += float3(0.32, 0.52, 0.75) * beamX * beamSurge * (0.22 + 0.18 * pulse2);

                // Citadel Molten Magma Archway flicker
                float archGlow = gauss(p, float2(0.738, 0.612), float2(0.068, 0.085));
                col.rgb += float3(0.45, 0.12, 0.03) * archGlow * (0.20 + 0.26 * flicker);

                // Glowing Demon Eyes (Right Foreground + Mid-Right Brute)
                float eyeGlow = gauss(p, float2(0.945, 0.620), float2(0.022, 0.018))
                              + gauss(p, float2(0.733, 0.718), float2(0.018, 0.016));
                col.rgb += float3(0.65, 0.08, 0.04) * eyeGlow * (0.25 + 0.35 * pulse1);

                // Blazing DEVIL title ember pulse
                col.rgb += float3(0.28, 0.09, 0.02) * fireLum * (0.18 + 0.28 * flicker);

                return col;
            }
            ENDCG
        }
    }
}
