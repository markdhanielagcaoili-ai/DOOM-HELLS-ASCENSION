using System;
using UnityEngine;

namespace DoomHellsAscension
{
    /// <summary>
    /// High-fidelity procedural 44.1kHz PCM Audio Synthesizer modeled after VALORANT's iconic
    /// weapon acoustics (Vandal punchy 7.62 crack, Phantom suppressed pneumatic tick, Judge booming
    /// 12G shotgun, Odin belt-fed heavy LMG, Operator cannon + bolt cycle), per-weapon multi-stage
    /// tactical reloads, low-ammo warning clinks, headshot helmet dinks, and escalating 5-kill Ace chimes!
    /// </summary>
    public class RealisticGunAudio : MonoBehaviour
    {
        public static RealisticGunAudio Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioClip[] gunClips = new AudioClip[5];
        private AudioClip[] reloadClips = new AudioClip[5];
        private AudioClip[] meleeClips = new AudioClip[4];
        private AudioClip[] killChimeClips = new AudioClip[5]; // Valorant 1K -> 2K -> 3K -> 4K -> 5K ACE!
        private AudioClip headshotDinkClip;
        private AudioClip bodyHitClip;
        private AudioClip lowAmmoTickClip;
        private AudioClip equipClip;
        private AudioClip adsZoomClip;
        private AudioClip damageClip;
        private AudioClip pickupClip;
        private AudioClip dashClip;
        private AudioClip overdriveClip;
        private AudioClip musicLoop;

        public bool AudioEnabled { get; private set; } = true;
        private const int SR = 44100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.90f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = 0.18f;

            GenerateAllClips();
        }

        public void ToggleAudio()
        {
            AudioEnabled = !AudioEnabled;
            if (!AudioEnabled) musicSource.Pause();
            else if (musicLoop != null && !musicSource.isPlaying) musicSource.Play();
        }

        public void StartMusic()
        {
            if (!AudioEnabled || musicLoop == null) return;
            if (musicSource.clip != musicLoop) musicSource.clip = musicLoop;
            if (!musicSource.isPlaying) musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlayShoot(int weaponId, int ammoLeftInMag, int magSize)
        {
            if (!AudioEnabled || weaponId < 0 || weaponId >= gunClips.Length) return;
            sfxSource.pitch = UnityEngine.Random.Range(0.975f, 1.025f);
            sfxSource.PlayOneShot(gunClips[weaponId], 0.95f);

            // Signature Valorant low-ammo audible metallic warning tick on last 6 rounds!
            if (magSize >= 7 && ammoLeftInMag <= 6 && lowAmmoTickClip != null)
            {
                float urgency = 1f + (6 - ammoLeftInMag) * 0.06f;
                sfxSource.pitch = urgency;
                sfxSource.PlayOneShot(lowAmmoTickClip, 0.45f);
                sfxSource.pitch = 1f;
            }
        }

        public void PlayReload(int weaponId)
        {
            if (!AudioEnabled) return;
            int idx = Mathf.Clamp(weaponId, 0, reloadClips.Length - 1);
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(reloadClips[idx], 0.98f);
        }

        public void PlayEquip()
        {
            if (!AudioEnabled || equipClip == null) return;
            sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            sfxSource.PlayOneShot(equipClip, 0.72f);
        }

        public void PlayADS(bool zoomIn)
        {
            if (!AudioEnabled || adsZoomClip == null) return;
            sfxSource.pitch = zoomIn ? 1.15f : 0.85f;
            sfxSource.PlayOneShot(adsZoomClip, 0.48f);
        }

        public void PlayMelee(int meleeId)
        {
            if (!AudioEnabled || meleeId < 0 || meleeId >= meleeClips.Length) return;
            sfxSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(meleeClips[meleeId], 0.92f);
        }

        public void PlayHit(bool isKill, bool isHeadshot = false, int streakCount = 1)
        {
            if (!AudioEnabled) return;
            sfxSource.pitch = 1f;
            if (isHeadshot && headshotDinkClip != null)
            {
                // Iconic Valorant crisp helmet headshot DINK-CRUNCH!
                sfxSource.PlayOneShot(headshotDinkClip, 0.95f);
            }
            else if (!isKill && bodyHitClip != null)
            {
                sfxSource.PlayOneShot(bodyHitClip, 0.65f);
            }

            if (isKill)
            {
                int chimeIdx = Mathf.Clamp(streakCount - 1, 0, killChimeClips.Length - 1);
                sfxSource.PlayOneShot(killChimeClips[chimeIdx], 0.92f);
            }
        }

        public void PlayDamage() { if (AudioEnabled) { sfxSource.pitch = 1f; sfxSource.PlayOneShot(damageClip, 0.85f); } }
        public void PlayPickup() { if (AudioEnabled) { sfxSource.pitch = 1f; sfxSource.PlayOneShot(pickupClip, 0.75f); } }
        public void PlayDash() { if (AudioEnabled) { sfxSource.pitch = 1f; sfxSource.PlayOneShot(dashClip, 0.72f); } }
        public void PlayOverdrive() { if (AudioEnabled) { sfxSource.pitch = 1f; sfxSource.PlayOneShot(overdriveClip, 0.92f); } }

        private void GenerateAllClips()
        {
            // 5 Iconic Valorant-Inspired Realistic Gunshots
            gunClips[0] = SynthVandalReaverShot();       // [1] Vandal: Heavy punchy 7.62 crack + metallic slap + subtle Reaver dark bell
            gunClips[1] = SynthPhantomSuppressedShot();  // [2] Phantom: Tight pneumatic silencer PFFFT-TICK + muted bolt
            gunClips[2] = SynthJudgeShotgunShot();       // [3] Judge: Heavy booming 12G shotgun cannon + drum rattle
            gunClips[3] = SynthOdinHeavyLmgShot();       // [4] Odin: Heavy open-bolt machine gun thump + brass belt link clink
            gunClips[4] = SynthOperatorSniperShot();     // [5] Operator: Colossal sniper cannon + dramatic bolt-action cycle!

            // 5 Dedicated Per-Weapon Valorant Multi-Stage Mechanical Reloads
            reloadClips[0] = SynthReloadVandal(1.85f);
            reloadClips[1] = SynthReloadPhantom(1.45f);
            reloadClips[2] = SynthReloadJudge(1.75f);
            reloadClips[3] = SynthReloadOdin(2.25f);
            reloadClips[4] = SynthReloadOperator(1.95f);

            // Melee Clips (Karambit spin-slash, Katana energy slice, Chainsword rev, Warhammer slam)
            meleeClips[0] = SynthBladeSlash(0.24f, 2600f, false, false);
            meleeClips[1] = SynthBladeSlash(0.32f, 2200f, false, false);
            meleeClips[2] = SynthBladeSlash(0.42f, 1200f, true, false);
            meleeClips[3] = SynthBladeSlash(0.55f, 450f, false, true);

            // Valorant Escalating Multi-Kill Chimes (D minor / Phrygian Reaver bell chord progression!)
            float[] killNotes = { 293.66f, 349.23f, 440.00f, 523.25f, 587.33f }; // D4, F4, A4, C5, D5 (ACE!)
            for (int i = 0; i < 5; i++)
            {
                killChimeClips[i] = SynthValorantKillChime(killNotes[i], i == 4);
            }

            headshotDinkClip = SynthValorantHeadshotDink();
            bodyHitClip = SynthToneBurst(0.055f, 420f, 180f, 0.55f);
            lowAmmoTickClip = SynthMetallicImpulse(0.045f, 4400f, 2900f, 0.65f);
            equipClip = SynthEquipSlideClick();
            adsZoomClip = SynthMetallicImpulse(0.06f, 1600f, 2400f, 0.45f);
            damageClip = SynthToneBurst(0.22f, 135f, 38f, 0.78f);
            pickupClip = SynthPickupChime(0.22f);
            dashClip = SynthToneBurst(0.16f, 260f, 680f, 0.55f);
            overdriveClip = SynthOverdriveRoar(0.55f);
            musicLoop = SynthIndustrialMetalLoop(3.04f);
        }

        // =========================================================================
        // 1. VANDAL "REAVER" — Punchy 7.62 Crack + Deep Dark Gothic Bell & Spectral Tail
        // =========================================================================
        private AudioClip SynthVandalReaverShot()
        {
            float dur = 0.68f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(101);
            float lp = 0f, hp = 0f, phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.18f * (n - lp);
                hp += 0.42f * (n - hp);

                // Razor-sharp Vandal initial transient snap (0-18ms)
                float crack = (n - hp) * Mathf.Exp(-t * 118f) * 2.15f;
                // Signature Reaver deep hollow bark
                float bark = lp * Mathf.Exp(-t * 21f) * 1.60f;
                // Heavy chest sub-punch (185Hz -> 36Hz)
                phase += 2f * Mathf.PI * Mathf.Lerp(185f, 36f, Mathf.Min(1f, t / 0.075f)) / SR;
                float punch = Mathf.Sin(phase) * Mathf.Exp(-t * 24f) * 1.25f;

                // Crisp steel bolt carrier slap at 20ms
                float bolt = 0f;
                if (t > 0.020f && t < 0.068f)
                {
                    float bt = t - 0.020f;
                    bolt = (Mathf.Sin(2f * Mathf.PI * 2450f * bt) * 0.50f + Mathf.Sin(2f * Mathf.PI * 4120f * bt) * 0.30f) * Mathf.Exp(-bt * 82f);
                }

                // Iconic VALORANT Reaver dark gothic cathedral bell harmonics + spectral whisper tail!
                float reaverBell = Mathf.Sin(2f * Mathf.PI * 220.00f * t) * Mathf.Exp(-t * 8.5f) * 0.30f
                                 + Mathf.Sin(2f * Mathf.PI * 293.66f * t) * Mathf.Exp(-t * 10.5f) * 0.28f
                                 + Mathf.Sin(2f * Mathf.PI * 440.00f * t) * Mathf.Exp(-t * 14.0f) * 0.18f
                                 + Mathf.Sin(2f * Mathf.PI * 622.25f * t) * Mathf.Exp(-t * 19.0f) * 0.12f;

                // Eerie hollow spectral wind reverberation tail
                float spectralTail = (t > 0.055f) ? lp * Mathf.Exp(-(t - 0.055f) * 7.2f) * 0.42f : 0f;

                // Dark metallic shell casing double-clink
                float brass = BrassCasingSample(t, 0.30f, 6400f);

                s[i] = (float)Math.Tanh((crack + bark + punch + bolt + reaverBell + spectralTail + brass) * 1.42f);
            }
            return MakeClip("VandalShot", s);
        }

        // =========================================================================
        // 2. PHANTOM "INFERNO" — Iconic Suppressed Pneumatic Silencer PFFFT-TICK!
        // =========================================================================
        private AudioClip SynthPhantomSuppressedShot()
        {
            float dur = 0.36f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(202);
            float lp = 0f, bp = 0f, phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.12f * (n - lp);
                bp += 0.32f * (n - bp);

                // Tight pneumatic silencer air puff (no loud boom, crisp filtered hiss!)
                float puff = (bp - lp) * Mathf.Exp(-t * 48f) * 1.55f;
                // Supersonic sub-bass thud inside suppressor baffles (140Hz -> 55Hz)
                phase += 2f * Mathf.PI * Mathf.Lerp(145f, 52f, Mathf.Min(1f, t / 0.045f)) / SR;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 42f) * 0.85f;
                // Crisp high-tech mechanical bolt click (signature Phantom tick!)
                float tick = (Mathf.Sin(2f * Mathf.PI * 3350f * t) * 0.45f + (n - bp) * 0.55f) * Mathf.Exp(-t * 145f);
                float brass = BrassCasingSample(t, 0.22f, 7450f);

                s[i] = (float)Math.Tanh((puff + thump + tick + brass) * 1.35f);
            }
            return MakeClip("PhantomShot", s);
        }

        // =========================================================================
        // 3. JUDGE "HELL-BREACHER" — Booming 12-Gauge Drum Shotgun Cannon
        // =========================================================================
        private AudioClip SynthJudgeShotgunShot()
        {
            float dur = 0.68f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(303);
            float lp = 0f, phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.15f * (n - lp);

                phase += 2f * Mathf.PI * Mathf.Lerp(135f, 26f, Mathf.Min(1f, t / 0.14f)) / SR;
                float boom = Mathf.Sin(phase) * Mathf.Exp(-t * 14f) * 1.45f;
                float blast = lp * Mathf.Exp(-t * 13f) * 1.75f + n * Mathf.Exp(-t * 85f) * 1.25f;
                float tail = (t > 0.10f) ? lp * Mathf.Exp(-(t - 0.10f) * 6.5f) * 0.45f : 0f;

                // Heavy semi-auto drum cycling clank at 130ms
                float drum = (t > 0.12f && t < 0.19f)
                    ? (Mathf.Sin(2f * Mathf.PI * 1450f * (t - 0.12f)) * 0.4f + n * 0.25f) * Mathf.Exp(-(t - 0.12f) * 55f)
                    : 0f;

                s[i] = (float)Math.Tanh((boom + blast + tail + drum) * 1.5f);
            }
            return MakeClip("JudgeShot", s);
        }

        // =========================================================================
        // 4. ODIN "SOUL-SHREDDER" — Heavy Belt-Fed LMG Thump + Clattering Brass Links
        // =========================================================================
        private AudioClip SynthOdinHeavyLmgShot()
        {
            float dur = 0.42f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(404);
            float lp = 0f, phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.22f * (n - lp);

                phase += 2f * Mathf.PI * Mathf.Lerp(165f, 46f, Mathf.Min(1f, t / 0.07f)) / SR;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 25f) * 1.2f;
                float bark = lp * Mathf.Exp(-t * 22f) * 1.45f + n * Mathf.Exp(-t * 110f) * 1.2f;
                // Distinct metallic belt-link clatter on every Odin shot!
                float links = (t > 0.018f && t < 0.075f)
                    ? (Mathf.Sin(2f * Mathf.PI * 3950f * (t - 0.018f)) * 0.45f + Mathf.Sin(2f * Mathf.PI * 5700f * (t - 0.018f)) * 0.3f) * Mathf.Exp(-(t - 0.018f) * 75f)
                    : 0f;

                s[i] = (float)Math.Tanh((thump + bark + links) * 1.4f);
            }
            return MakeClip("OdinShot", s);
        }

        // =========================================================================
        // 5. OPERATOR "REAVER RAIL" — Earth-Shattering Sniper Cannon + Bolt-Action Cycle!
        // =========================================================================
        private AudioClip SynthOperatorSniperShot()
        {
            float dur = 1.12f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(505);
            float lp = 0f, hp = 0f, phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.14f * (n - lp);
                hp += 0.45f * (n - hp);

                // Colossal Operator shockwave sub-boom (155Hz -> 22Hz)
                phase += 2f * Mathf.PI * Mathf.Lerp(155f, 22f, Mathf.Min(1f, t / 0.18f)) / SR;
                float cannon = Mathf.Sin(phase) * Mathf.Exp(-t * 8.5f) * 1.65f;
                // Piercing supersonic whip-crack
                float crack = (n - hp) * Mathf.Exp(-t * 70f) * 2.2f + lp * Mathf.Exp(-t * 10f) * 1.75f;
                // Hollow Reaver rail resonance ring
                float rail = (Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.18f) * Mathf.Exp(-t * 7f);
                // Long stadium/citadel canyon echo tail
                float echo = (t > 0.14f) ? lp * Mathf.Exp(-(t - 0.14f) * 4.5f) * 0.48f : 0f;

                // Iconic Valorant Operator Bolt-Action Cycle (0.48s lift-pull -> 0.72s push-lock!)
                float bolt = 0f;
                bolt += MechClickSample(t, 0.48f, 1850f, 2900f, 0.08f, n, 0.55f); // Bolt handle lift & pull back
                bolt += MechClickSample(t, 0.72f, 3200f, 1450f, 0.09f, n, 0.65f); // Bolt slam forward & lock!
                float brass = BrassCasingSample(t, 0.62f, 5900f);

                s[i] = (float)Math.Tanh((cannon + crack + rail + echo + bolt + brass) * 1.45f);
            }
            return MakeClip("OperatorShot", s);
        }

        // =========================================================================
        // 5 DEDICATED VALORANT MULTI-STAGE TACTICAL RELOAD AUDIO CLIPS
        // =========================================================================
        private AudioClip SynthReloadVandal(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(1111);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.18f * (n - lp);
                float v = 0f;
                // 0.08s: Telekinetic spectral unlatch + ghostly downward swoosh as mag floats out!
                v += MechClickSample(t, 0.08f, 2100f, 920f, 0.08f, n, 0.50f);
                v += FrictionSlideSample(t, 0.10f, 0.36f, lp, 0.56f);
                if (t > 0.08f && t < 0.52f)
                {
                    float wt = t - 0.08f;
                    v += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 155f, wt / 0.44f) * wt) * Mathf.Sin(Mathf.PI * wt / 0.44f) * 0.28f;
                }
                // 0.72s -> 1.12s: Rising dark spectral hum as fresh mag levitates up & snaps home with bell chime!
                if (t > 0.70f && t < 1.12f)
                {
                    float rt = t - 0.70f;
                    v += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(170f, 360f, rt / 0.42f) * rt) * Mathf.Sin(Mathf.PI * rt / 0.42f) * 0.26f;
                    v += FrictionSlideSample(t, 0.72f, 0.38f, lp, 0.44f);
                }
                v += PalmThudSample(t, 1.10f, 130f, 0.78f);
                v += MechClickSample(t, 1.12f, 1800f, 3300f, 0.07f, n, 0.68f);
                if (t > 1.10f && t < 1.50f)
                {
                    float bt = t - 1.10f;
                    v += Mathf.Sin(2f * Mathf.PI * 293.66f * bt) * Mathf.Exp(-bt * 9.5f) * 0.22f;
                }
                // 1.38s -> 1.60s: Telekinetic bolt yank & gothic metallic SHING-CLACK!
                v += FrictionSlideSample(t, 1.34f, 0.18f, n - lp, 0.48f);
                v += MechClickSample(t, 1.36f, 1500f, 2800f, 0.08f, n, 0.56f);
                v += PalmThudSample(t, 1.58f, 220f, 0.68f);
                v += MechClickSample(t, 1.58f, 3800f, 1900f, 0.09f, n, 0.80f);
                if (t > 1.58f)
                {
                    float st = t - 1.58f;
                    v += (Mathf.Sin(2f * Mathf.PI * 587.33f * st) * 0.16f + Mathf.Sin(2f * Mathf.PI * 880f * st) * 0.10f) * Mathf.Exp(-st * 11f);
                }
                s[i] = Mathf.Clamp(v, -0.95f, 0.95f);
            }
            return MakeClip("ReloadVandal", s);
        }

        private AudioClip SynthReloadPhantom(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(2222);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.22f * (n - lp);
                float v = 0f;
                // 0.05s: Crisp tactical mag button CLICK + straight STANAG mag drop
                v += MechClickSample(t, 0.05f, 3100f, 1900f, 0.045f, n, 0.55f);
                v += FrictionSlideSample(t, 0.09f, 0.16f, lp, 0.42f);
                // 0.62s: Smooth mag insertion + clean baseplate tap
                v += FrictionSlideSample(t, 0.56f, 0.09f, lp, 0.35f);
                v += PalmThudSample(t, 0.64f, 165f, 0.68f);
                v += MechClickSample(t, 0.65f, 2400f, 3600f, 0.055f, n, 0.68f);
                // 1.08s: Iconic Valorant Phantom left-hand Bolt-Release Paddle Slap (PING-CLACK!)
                v += PalmThudSample(t, 1.08f, 260f, 0.62f);
                v += MechClickSample(t, 1.09f, 4100f, 2100f, 0.075f, n, 0.82f);
                s[i] = Mathf.Clamp(v, -0.95f, 0.95f);
            }
            return MakeClip("ReloadPhantom", s);
        }

        private AudioClip SynthReloadJudge(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(3333);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.18f * (n - lp);
                float v = 0f;
                // 0.10s: Heavy drum latch unclick & twist off
                v += MechClickSample(t, 0.10f, 1600f, 820f, 0.09f, n, 0.65f);
                v += FrictionSlideSample(t, 0.16f, 0.24f, lp, 0.55f);
                // 0.82s: Heavy 12G drum mag slams home (THOOM-CLANK!)
                v += PalmThudSample(t, 0.82f, 105f, 0.88f);
                v += MechClickSample(t, 0.85f, 1400f, 2400f, 0.08f, n, 0.72f);
                // 1.32s -> 1.48s: Heavy pump/bolt double rack (CHCK-CHACK!)
                v += MechClickSample(t, 1.32f, 1500f, 2300f, 0.08f, n, 0.72f);
                v += MechClickSample(t, 1.48f, 2700f, 1300f, 0.09f, n, 0.82f);
                s[i] = Mathf.Clamp(v, -0.95f, 0.95f);
            }
            return MakeClip("ReloadJudge", s);
        }

        private AudioClip SynthReloadOdin(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(4444);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.20f * (n - lp);
                float v = 0f;
                // 0.12s: Top feed cover latches pop open (CLANK!)
                v += MechClickSample(t, 0.12f, 1900f, 950f, 0.10f, n, 0.68f);
                // 0.52s: Empty ammo box drops out
                v += FrictionSlideSample(t, 0.45f, 0.22f, lp, 0.55f);
                // 0.95s: Huge 100-round box mag slams into receiver (THOOM!)
                v += PalmThudSample(t, 0.95f, 95f, 0.90f);
                v += MechClickSample(t, 0.98f, 1600f, 2500f, 0.07f, n, 0.65f);
                // 1.32s: Brass bullet belt draped across feed tray (jingle-clink!)
                v += BrassCasingSample(t, 1.28f, 5200f) * 1.8f;
                v += BrassCasingSample(t, 1.40f, 6100f) * 1.8f;
                // 1.68s: Top feed cover SLAMS shut (KA-CLACK!)
                v += PalmThudSample(t, 1.68f, 150f, 0.85f);
                v += MechClickSample(t, 1.69f, 2800f, 1200f, 0.10f, n, 0.85f);
                // 1.98s: Heavy charging handle pull & release
                v += MechClickSample(t, 1.96f, 2200f, 3400f, 0.08f, n, 0.72f);
                v += MechClickSample(t, 2.08f, 3500f, 1500f, 0.09f, n, 0.78f);
                s[i] = Mathf.Clamp(v, -0.95f, 0.95f);
            }
            return MakeClip("ReloadOdin", s);
        }

        private AudioClip SynthReloadOperator(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(5555);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.18f * (n - lp);
                float v = 0f;
                // 0.12s: Heavy steel .50 mag unlatch & pull
                v += MechClickSample(t, 0.12f, 1750f, 880f, 0.09f, n, 0.68f);
                v += FrictionSlideSample(t, 0.18f, 0.24f, lp, 0.50f);
                // 0.88s: Fresh heavy box mag locks in (THUNK-CLANK!)
                v += PalmThudSample(t, 0.88f, 115f, 0.88f);
                v += MechClickSample(t, 0.91f, 2100f, 3400f, 0.08f, n, 0.75f);
                // 1.42s -> 1.66s: Iconic Operator bolt lift-pull-push-lock!
                v += MechClickSample(t, 1.40f, 1750f, 2850f, 0.10f, n, 0.72f);
                v += FrictionSlideSample(t, 1.46f, 0.15f, n - lp, 0.45f);
                v += PalmThudSample(t, 1.66f, 210f, 0.72f);
                v += MechClickSample(t, 1.66f, 3500f, 1550f, 0.10f, n, 0.88f);
                s[i] = Mathf.Clamp(v, -0.95f, 0.95f);
            }
            return MakeClip("ReloadOperator", s);
        }

        // =========================================================================
        // VALORANT HEADSHOT DINK & ESCALATING 5-KILL REAVER BELL CHIMES
        // =========================================================================
        private AudioClip SynthValorantHeadshotDink()
        {
            float dur = 0.18f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(888);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Crisp metallic helmet PING (2850Hz + 4320Hz) + meaty crunch!
                float ping = (Mathf.Sin(2f * Mathf.PI * 2850f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * 4320f * t) * 0.45f) * Mathf.Exp(-t * 32f);
                float crunch = n * Mathf.Exp(-t * 55f) * 0.65f + Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 40f) * 0.55f;
                s[i] = (float)Math.Tanh((ping + crunch) * 1.4f);
            }
            return MakeClip("HeadshotDink", s);
        }

        private AudioClip SynthValorantKillChime(float rootFreq, bool isAce)
        {
            float dur = isAce ? 1.35f : 0.72f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                // Deep dark Reaver bass drop
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 38f, Mathf.Min(1f, t / 0.28f)) * t) * Mathf.Exp(-t * (isAce ? 3.2f : 6.5f)) * 0.75f;
                // Resonant obsidian bell overtones (1x, 1.5x fifth, 2x octave, 2.756x bell harmonic)
                float bell = Mathf.Sin(2f * Mathf.PI * rootFreq * t) * Mathf.Exp(-t * (isAce ? 2.4f : 4.5f)) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * (rootFreq * 1.5f) * t) * Mathf.Exp(-t * 5.2f) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * (rootFreq * 2.0f) * t) * Mathf.Exp(-t * 6.8f) * 0.28f
                           + Mathf.Sin(2f * Mathf.PI * (rootFreq * 2.756f) * t) * Mathf.Exp(-t * 11f) * 0.18f;
                if (isAce)
                {
                    bell += Mathf.Sin(2f * Mathf.PI * (rootFreq * 0.5f) * t) * Mathf.Exp(-t * 2.0f) * 0.45f;
                }
                s[i] = (float)Math.Tanh((sub + bell) * 1.25f);
            }
            return MakeClip("KillChime", s);
        }

        private AudioClip SynthEquipSlideClick()
        {
            float dur = 0.24f;
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(321);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.22f * (n - lp);
                float v = FrictionSlideSample(t, 0.01f, 0.11f, lp, 0.38f)
                        + MechClickSample(t, 0.11f, 2600f, 1500f, 0.06f, n, 0.55f);
                s[i] = Mathf.Clamp(v, -0.9f, 0.9f);
            }
            return MakeClip("EquipClick", s);
        }

        // Reusable Acoustic Building Blocks
        private static float MechClickSample(float t, float start, float f1, float f2, float dur, float noise, float amp)
        {
            if (t < start || t > start + dur) return 0f;
            float lt = t - start;
            float freq = Mathf.Lerp(f1, f2, lt / dur);
            return (Mathf.Sin(2f * Mathf.PI * freq * lt) * 0.65f + noise * 0.35f) * Mathf.Exp(-lt * (5.5f / dur)) * amp;
        }

        private static float PalmThudSample(float t, float start, float f0, float amp)
        {
            if (t < start || t > start + 0.09f) return 0f;
            float lt = t - start;
            return Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0, 38f, lt / 0.09f) * lt) * Mathf.Exp(-lt * 42f) * amp;
        }

        private static float FrictionSlideSample(float t, float start, float dur, float filteredNoise, float amp)
        {
            if (t < start || t > start + dur) return 0f;
            float env = Mathf.Sin(Mathf.PI * ((t - start) / dur));
            return filteredNoise * env * amp;
        }

        private static float BrassCasingSample(float t, float start, float freq)
        {
            float v = 0f;
            if (t > start && t < start + 0.05f)
            {
                float lt = t - start;
                v += Mathf.Sin(2f * Mathf.PI * freq * lt) * Mathf.Exp(-lt * 75f) * 0.13f;
            }
            if (t > start + 0.085f && t < start + 0.13f)
            {
                float lt = t - (start + 0.085f);
                v += Mathf.Sin(2f * Mathf.PI * (freq * 1.07f) * lt) * Mathf.Exp(-lt * 85f) * 0.08f;
            }
            return v;
        }

        private AudioClip SynthMetallicImpulse(float dur, float f1, float f2, float vol)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(19);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = MechClickSample(t, 0f, f1, f2, dur, n, vol);
            }
            return MakeClip("MetalImp", s);
        }

        private AudioClip SynthBladeSlash(float dur, float steelFreq, bool chainsaw, bool hammer)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            System.Random rng = new System.Random(777);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.16f * (n - lp);
                float val;
                if (chainsaw)
                {
                    float rev = Mathf.Lerp(105f, 250f, t / dur);
                    float saw = ((t * rev) % 1f) * 2f - 1f;
                    val = (saw * 0.65f + n * 0.45f) * Mathf.Exp(-t * 3.2f);
                }
                else if (hammer)
                {
                    val = (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(145f, 26f, t / dur) * t) * 1.1f + lp * 0.8f) * Mathf.Exp(-t * 5.2f);
                }
                else
                {
                    float env = Mathf.Sin(Mathf.PI * (t / dur));
                    val = ((n - lp) * 0.55f + Mathf.Sin(2f * Mathf.PI * steelFreq * t) * 0.35f) * env;
                }
                s[i] = (float)Math.Tanh(val * 1.2f);
            }
            return MakeClip("MeleeSFX", s);
        }

        private AudioClip SynthToneBurst(float dur, float f1, float f2, float vol)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            float p = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                p += 2f * Mathf.PI * Mathf.Lerp(f1, f2, t / dur) / SR;
                s[i] = Mathf.Sin(p) * Mathf.Exp(-t * (5f / dur)) * vol;
            }
            return MakeClip("ToneSFX", s);
        }

        private AudioClip SynthPickupChime(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float f = t < 0.08f ? 520f : 880f;
                s[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 11f) * 0.6f;
            }
            return MakeClip("PickupSFX", s);
        }

        private AudioClip SynthOverdriveRoar(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float f1 = Mathf.Lerp(180f, 880f, t / dur);
                float f2 = Mathf.Lerp(270f, 660f, t / dur);
                s[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.4f) * Mathf.Exp(-t * 3.5f);
            }
            return MakeClip("OverdriveSFX", s);
        }

        private AudioClip SynthIndustrialMetalLoop(float dur)
        {
            int len = Mathf.FloorToInt(SR * dur);
            float[] s = new float[len];
            float[] riff = { 55f, 55f, 65.4f, 55f, 73.4f, 55f, 61.7f, 58.3f, 55f, 55f, 82.4f, 73.4f, 65.4f, 55f, 61.7f, 55f };
            float stepDur = dur / riff.Length;
            System.Random rng = new System.Random(666);

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                int stepIdx = Mathf.Min(riff.Length - 1, Mathf.FloorToInt(t / stepDur));
                float st = t - stepIdx * stepDur;
                float freq = riff[stepIdx];

                float saw = ((st * freq) % 1f) * 2f - 1f;
                float bass = (float)Math.Tanh(saw * 2.2f) * Mathf.Exp(-st * 7.5f) * 0.36f;
                float kick = (stepIdx % 4 == 0) ? Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(115f, 32f, Mathf.Min(1f, st / 0.12f)) * st) * Mathf.Exp(-st * 22f) * 0.48f : 0f;
                float snare = (stepIdx % 4 == 2) ? (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-st * 32f) * 0.22f : 0f;

                s[i] = Mathf.Clamp(bass + kick + snare, -0.9f, 0.9f);
            }
            return MakeClip("DoomIndustrialLoop", s);
        }

        private static AudioClip MakeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SR, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
