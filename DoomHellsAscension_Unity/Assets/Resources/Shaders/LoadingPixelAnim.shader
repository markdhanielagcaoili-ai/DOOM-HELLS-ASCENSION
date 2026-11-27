Shader "Hidden/DoomLoadingPixelAnim"
{
    Properties
    {
        _MainTex ("Loading Texture", 2D) = "white" {}
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

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _AnimTime;
                // Top-down normalized coords (0=top, 1=bottom) matching 1024x571 pixel art
                float2 p = float2(i.uv.x, 1.0 - i.uv.y);
                float2 uv = p;

                // Subtle rhythmic Super Shotgun firing beat + heavy combat breathing
                float breath = sin(t * 2.05);
                float fireBeat = pow(max(0.0, sin(t * 3.4)), 6.0);
                float fastFlicker = 0.5 + 0.5 * sin(t * 19.0) * cos(t * 11.5);

                // 1. DOOM SLAYER (Right side): Breathing torso, helmet bob, shotgun recoil & muzzle blast!
                float footAnchor = smoothstep(0.92, 0.64, p.y);
                float torsoMask = gauss(p, float2(0.735, 0.365), float2(0.118, 0.165)) * footAnchor;
                uv.y += breath * 0.0115 * torsoMask;
                uv.x -= (p.x - 0.735) * breath * 0.040 * torsoMask;

                float headMask = gauss(p, float2(0.735, 0.185), float2(0.055, 0.088));
                uv.y += sin(t * 2.05 - 0.25) * 0.0105 * headMask;
                uv.x += sin(t * 1.1) * 0.0032 * headMask;

                // Super Shotgun + Forearms punchy kickback on fireBeat + steady aim sway
                float gunMask = gauss(p, float2(0.775, 0.425), float2(0.130, 0.072));
                uv.x += (fireBeat * 0.0135 + sin(t * 2.05 - 0.5) * 0.0035) * gunMask;
                uv.y += (fireBeat * 0.0055 + breath * 0.0080) * gunMask;

                // Dynamic Roaring Muzzle Flash expand & flicker
                float muzzleMask = gauss(p, float2(0.900, 0.398), float2(0.052, 0.085));
                float muzzleScale = 0.08 * fireBeat + 0.035 * fastFlicker;
                uv -= (p - float2(0.882, 0.402)) * muzzleScale * muzzleMask;
                uv.y += sin(p.x * 80.0 - t * 22.0) * 0.0035 * muzzleMask;

                // 2. EXPLOSIVE FIREBURSTS behind Slayer (dynamic expansion + roaring heat wave)
                float burstMask = gauss(p, float2(0.625, 0.595), float2(0.068, 0.085))
                                + gauss(p, float2(0.825, 0.615), float2(0.062, 0.080))
                                + gauss(p, float2(0.565, 0.935), float2(0.075, 0.070))
                                + gauss(p, float2(0.895, 0.885), float2(0.065, 0.080));
                float burstWave = 0.035 * sin(t * 8.5) + 0.055 * fireBeat;
                uv += float2(sin(p.y * 55.0 - t * 9.0), cos(p.x * 55.0 + t * 8.0)) * 0.0035 * saturate(burstMask);
                uv -= (p - float2(0.73, 0.62)) * burstWave * saturate(burstMask) * 0.35;

                // 3. FLOATING SHATTERED ROCKS upper-right subtle tremor
                float rockMask = gauss(p, float2(0.68, 0.09), float2(0.16, 0.085)) * (1.0 - saturate(headMask * 1.4));
                uv.y += sin(t * 1.6 + p.x * 14.0) * 0.0055 * rockMask;

                // Base texture sample
                float2 sampleUV = clamp(uv, float2(0.002, 0.002), float2(0.998, 0.998));
                fixed4 col = tex2D(_MainTex, float2(sampleUV.x, 1.0 - sampleUV.y));

                // 4. CONTINUOUS POURING MOLTEN LAVA WATERFALL & BOILING POOL (strictly on orange/yellow lava pixels!)
                float lavaPix = smoothstep(0.45, 0.88, col.r - col.b * 0.6) * smoothstep(0.15, 0.62, col.g);
                float fallUpper = gauss(p, float2(0.240, 0.155), float2(0.048, 0.075));
                float fallLower = gauss(p, float2(0.240, 0.735), float2(0.055, 0.110))
                                + gauss(p, float2(0.408, 0.855), float2(0.024, 0.075));
                float fallMask = saturate(fallUpper + fallLower) * lavaPix;
                if (fallMask > 0.02)
                {
                    float2 luv = float2(sampleUV.x + sin(p.y * 46.0 - t * 7.2) * 0.0042 * fallMask,
                                        sampleUV.y - frac(t * 0.55) * 0.048 * fallMask);
                    luv = clamp(luv, float2(0.002, 0.002), float2(0.998, 0.998));
                    fixed4 lc = tex2D(_MainTex, float2(luv.x, 1.0 - luv.y));
                    col = lerp(col, max(col, lc), fallMask * 0.72);
                }

                float poolMask = gauss(p, float2(0.415, 0.945), float2(0.095, 0.068)) * lavaPix;
                if (poolMask > 0.02)
                {
                    float2 puv = sampleUV + float2(sin(p.y * 60.0 + t * 4.8) * 0.0055, cos(p.x * 45.0 - t * 3.6) * 0.0028) * poolMask;
                    puv = clamp(puv, float2(0.002, 0.002), float2(0.998, 0.998));
                    col = lerp(col, tex2D(_MainTex, float2(puv.x, 1.0 - puv.y)), poolMask * 0.75);
                }

                // 5. DANCING FIRE FLAMES LICKING OFF "DEVIL" TITLE (letters stay crisp & metallic!)
                float titleFlameArea = gauss(p, float2(0.335, 0.375), float2(0.255, 0.195));
                float titleFlame = smoothstep(0.52, 0.92, col.r - col.b * 0.55) * smoothstep(0.22, 0.70, col.g) * titleFlameArea;
                if (titleFlame > 0.02)
                {
                    float2 fuv = sampleUV + float2(sin(p.y * 62.0 - t * 7.8) * 0.0038, -cos(p.x * 48.0 + t * 6.2) * 0.0025) * titleFlame;
                    fuv = clamp(fuv, float2(0.002, 0.002), float2(0.998, 0.998));
                    col = lerp(col, tex2D(_MainTex, float2(fuv.x, 1.0 - fuv.y)), titleFlame * 0.78);
                }

                // 6. DYNAMIC LIGHTING GLOW PULSES (Muzzle Blast, Lava Heat, Firebursts, Visor, Gargoyle Eyes)
                float pulse1 = 0.5 + 0.5 * sin(t * 3.4);
                float pulse2 = 0.5 + 0.5 * sin(t * 2.5 + 1.1);

                // Blazing Super Shotgun Muzzle Flash Illumination
                col.rgb += float3(0.85, 0.45, 0.10) * muzzleMask * (0.22 + 0.65 * fireBeat + 0.20 * fastFlicker);
                // Pouring Lava Waterfall & Pool Molten Glow
                col.rgb += float3(0.45, 0.16, 0.02) * saturate(fallMask + poolMask) * (0.20 + 0.30 * pulse1);
                // Explosive Firebursts Glow
                col.rgb += float3(0.55, 0.22, 0.04) * saturate(burstMask) * (0.18 + 0.35 * fireBeat + 0.18 * fastFlicker);
                // Blazing DEVIL Title Flames + ABYSSAL*REIGN center ember dot
                float titleDot = gauss(p, float2(0.379, 0.628), float2(0.022, 0.022));
                col.rgb += float3(0.35, 0.12, 0.02) * titleFlame * (0.15 + 0.25 * fastFlicker);
                col.rgb += float3(0.75, 0.22, 0.04) * titleDot * (0.30 + 0.45 * pulse1);
                // Slayer Visor subtle tactical green-cyan glint
                float visorGlow = gauss(p, float2(0.735, 0.188), float2(0.026, 0.022));
                col.rgb += float3(0.12, 0.42, 0.34) * visorGlow * (0.18 + 0.24 * pulse2);
                // Gargoyle Eyes & Column Sigil ominous crimson pulse
                float gargoyleGlow = gauss(p, float2(0.086, 0.128), float2(0.018, 0.018))
                                   + gauss(p, float2(0.914, 0.128), float2(0.018, 0.018))
                                   + gauss(p, float2(0.068, 0.805), float2(0.026, 0.055));
                col.rgb += float3(0.65, 0.08, 0.04) * gargoyleGlow * (0.22 + 0.32 * pulse2);

                return col;
            }
            ENDCG
        }
    }
}
