/**
 * DOOM: HELL'S ASCENSION — True 3D WebGL Demonic Tactical FPS Engine (Three.js)
 * Features:
 * - Full 3D Gothic-Classical Demonic Citadel Plaza (Columns, Pediment, Ruined Hulls, Smoke, Sparks, Cables)
 * - Detailed Right-Handed 3D Gun Viewmodel (G36-Pattern Hell-Carbine + Gloved Arms, Top Rail, Iron Sights)
 * - Right-Click Aim Down Sights (ADS), [R] Tactical Magazine Reload, Shell Ejection, Bullet Decals & Sparks
 * - 5 Articulated 3D Demonic Enemy Archetypes + Boss Archon Malakor
 * - Abyssal Phase-Dash ([SHIFT]), Vertical Jump ([SPACE]), and Chrono-Thermal Overdrive ([Q]/[E])
 */

(() => {
  'use strict';

  // =========================================================================
  // 1. UI NAVIGATION & RUBRIC FILTERING
  // =========================================================================
  const navTabs = document.querySelectorAll('.nav-tab');
  const viewSections = document.querySelectorAll('.view-section');

  function switchMainView(targetId) {
    navTabs.forEach(t => t.classList.toggle('active', t.dataset.view === targetId));
    viewSections.forEach(s => s.classList.toggle('active', s.id === targetId));
    if (targetId !== 'view-game' && gameState.running && !gameState.paused) {
      pauseGame();
    }
  }

  navTabs.forEach(tab => {
    tab.addEventListener('click', () => switchMainView(tab.dataset.view));
  });

  document.getElementById('btn-open-criteria-from-menu').addEventListener('click', () => {
    switchMainView('view-criteria');
  });

  document.getElementById('btn-return-to-fps').addEventListener('click', () => {
    switchMainView('view-game');
  });

  const critFilters = document.querySelectorAll('.crit-filter');
  const critCards = document.querySelectorAll('.criterion-card');
  critFilters.forEach(btn => {
    btn.addEventListener('click', () => {
      critFilters.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      const filter = btn.dataset.filter;
      critCards.forEach(card => {
        if (filter === 'all' || card.id === filter) {
          card.style.display = 'block';
          card.classList.toggle('full-span', filter !== 'all' || card.id === 'item-6');
        } else {
          card.style.display = 'none';
        }
      });
    });
  });

  const gameWrapper = document.getElementById('game-wrapper');
  document.getElementById('btn-toggle-crt').addEventListener('click', () => {
    gameWrapper.classList.toggle('scanlines-on');
  });

  // =========================================================================
  // 2. REALISTIC MULTI-LAYER BALLISTIC & MECHANICAL RELOAD SOUND ENGINE
  // =========================================================================
  class SoundEngine {
    constructor() {
      this.ctx = null;
      this.enabled = true;
      this.musicTimer = null;
      this.step = 0;
      this.noiseBuffer = null;
    }

    init() {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) {
          this.ctx = new AudioCtx();
          // Pre-build 2-second broadband white/pink noise buffer for realistic gun acoustics
          const sr = this.ctx.sampleRate;
          this.noiseBuffer = this.ctx.createBuffer(1, sr * 2, sr);
          const data = this.noiseBuffer.getChannelData(0);
          for (let i = 0; i < data.length; i++) {
            data[i] = Math.random() * 2 - 1;
          }
        }
      }
      if (this.ctx && this.ctx.state === 'suspended') {
        this.ctx.resume();
      }
    }

    toggle() {
      this.enabled = !this.enabled;
      const btn = document.getElementById('btn-toggle-music');
      btn.textContent = this.enabled ? '🔊 AUDIO: ON' : '🔇 AUDIO: OFF';
      if (!this.enabled) {
        this.stopMusic();
      } else if (gameState.running && !gameState.paused) {
        this.startMusic();
      }
    }

    makeDistortionCurve(amount = 60) {
      const n = 44100;
      const curve = new Float32Array(n);
      const deg = Math.PI / 180;
      for (let i = 0; i < n; ++i) {
        const x = (i * 2) / n - 1;
        curve[i] = ((3 + amount) * x * 20 * deg) / (Math.PI + amount * Math.abs(x));
      }
      return curve;
    }

    // Helper: Filtered noise burst with ADSR envelope & optional saturation
    burstNoise(startTime, duration, filterType, freq, qVal, gainPeak, saturate = false) {
      if (!this.ctx || !this.noiseBuffer) return;
      const src = this.ctx.createBufferSource();
      src.buffer = this.noiseBuffer;

      const filter = this.ctx.createBiquadFilter();
      filter.type = filterType;
      filter.frequency.setValueAtTime(freq, startTime);
      filter.Q.value = qVal;

      const gain = this.ctx.createGain();
      gain.gain.setValueAtTime(0.001, startTime);
      gain.gain.linearRampToValueAtTime(gainPeak, startTime + 0.004);
      gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);

      src.connect(filter);
      if (saturate) {
        const shaper = this.ctx.createWaveShaper();
        shaper.curve = this.makeDistortionCurve(85);
        filter.connect(shaper);
        shaper.connect(gain);
      } else {
        filter.connect(gain);
      }
      gain.connect(this.ctx.destination);
      src.start(startTime, Math.random() * 0.5, duration);
      src.stop(startTime + duration);
    }

    // Helper: Metallic mechanical click/clack impulse
    metallicClick(startTime, freq1, freq2, duration, gainVal = 0.22) {
      if (!this.ctx) return;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(freq1, startTime);
      osc.frequency.exponentialRampToValueAtTime(freq2, startTime + duration);
      gain.gain.setValueAtTime(gainVal, startTime);
      gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start(startTime);
      osc.stop(startTime + duration);
      // High-frequency steel transient
      this.burstNoise(startTime, duration * 0.65, 'bandpass', Math.max(1800, freq1 * 2), 3.5, gainVal * 0.8);
    }

    // Realistic 5-Layer Gunshot Acoustics (Supersonic Crack + Muzzle Thump + Powder Bark + Bolt Cycle + Plaza Echo + Brass Casing Clink)
    playShoot(weaponId) {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;

      if (weaponId === 0) {
        // M4A1 / G36 Hell-Carbine (5.56/7.62 NATO Tactical Crack + Bolt Clack + Canyon Echo)
        this.toneAt(now, 175, 38, 0.09, 'sine', 0.45); // Sub-bass chest punch
        this.burstNoise(now, 0.025, 'bandpass', 3200, 1.4, 0.42, true); // Supersonic bullet crack
        this.burstNoise(now, 0.13, 'lowpass', 950, 0.9, 0.38, true); // Gunpowder muzzle bark
        this.metallicClick(now + 0.032, 2800, 1400, 0.035, 0.18); // Steel bolt carrier cycle
        // Citadel stone plaza reflection echo
        this.burstNoise(now + 0.095, 0.26, 'lowpass', 620, 0.7, 0.14, false);
        this.playBrassCasingClink(now + 0.28);
      } else if (weaponId === 1) {
        // SPAS-12 Hellfire Breacher (Massive 12-Gauge Shotgun Cannon Boom + Pump/Bolt Clack)
        this.toneAt(now, 135, 28, 0.22, 'sawtooth', 0.55); // Deep 12G chest thump
        this.burstNoise(now, 0.035, 'bandpass', 2400, 1.2, 0.48, true); // Buckshot blast crack
        this.burstNoise(now, 0.26, 'lowpass', 780, 0.8, 0.52, true); // Heavy muzzle roar
        this.burstNoise(now + 0.11, 0.38, 'lowpass', 480, 0.7, 0.2, false); // Cathedral echo tail
        // Pump-action / semi-auto shell rack clack
        this.metallicClick(now + 0.22, 1600, 920, 0.055, 0.24);
        this.metallicClick(now + 0.31, 2200, 1300, 0.05, 0.24);
      } else if (weaponId === 2) {
        // M249 Soul-Shredder LMG (Heavy Open-Bolt Machine Gun Bark + Belt Link Clatter)
        this.toneAt(now, 160, 42, 0.085, 'sawtooth', 0.4);
        this.burstNoise(now, 0.022, 'bandpass', 2900, 1.5, 0.38, true);
        this.burstNoise(now, 0.11, 'lowpass', 1050, 1.0, 0.35, true);
        this.metallicClick(now + 0.025, 3600, 2100, 0.03, 0.2); // Belt link clatter
        this.playBrassCasingClink(now + 0.24);
      } else if (weaponId === 3) {
        // B.F.G.-9000 Singularity Launcher (Sub-Bass Core Discharge + Plasma Thunder)
        this.toneAt(now, 95, 24, 0.65, 'sawtooth', 0.5);
        this.toneAt(now, 540, 85, 0.55, 'sine', 0.35);
        this.burstNoise(now, 0.52, 'lowpass', 680, 1.2, 0.45, true);
      } else if (weaponId === 4) {
        // AWM Abyssal Rail-Sniper (.338 Lapua / Anti-Materiel Thunderous Crack + Long Canyon Echo + Bolt Action)
        this.toneAt(now, 145, 25, 0.28, 'sawtooth', 0.6); // Massive sniper recoil thump
        this.burstNoise(now, 0.04, 'bandpass', 3800, 1.2, 0.55, true); // Piercing supersonic whip-crack
        this.burstNoise(now, 0.32, 'lowpass', 890, 0.9, 0.52, true); // High-caliber muzzle blast
        this.burstNoise(now + 0.14, 0.48, 'lowpass', 520, 0.6, 0.24, false); // Long plaza echo
        // Bolt-action cycling after shot
        this.metallicClick(now + 0.38, 1900, 2800, 0.06, 0.25);
        this.metallicClick(now + 0.54, 2900, 1500, 0.065, 0.28);
        this.playBrassCasingClink(now + 0.62);
      }
    }

    playBrassCasingClink(startTime) {
      if (!this.ctx) return;
      // High-pitched brass cylinder bounce on stone floor
      [0, 0.085].forEach((offset, i) => {
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'sine';
        const f = 6400 + Math.random() * 1400;
        osc.frequency.setValueAtTime(f, startTime + offset);
        osc.frequency.exponentialRampToValueAtTime(f * 0.88, startTime + offset + 0.035);
        gain.gain.setValueAtTime(0.055 / (i + 1), startTime + offset);
        gain.gain.exponentialRampToValueAtTime(0.0005, startTime + offset + 0.035);
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start(startTime + offset);
        osc.stop(startTime + offset + 0.035);
      });
    }

    // Realistic 4-Stage Mechanical Firearm Reload Audio
    playReload(weaponId = 0) {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;

      // Stage 1 (0.04s): Magazine release catch click + empty mag sliding out of magwell
      this.metallicClick(now + 0.03, 2100, 1200, 0.04, 0.26);
      this.burstNoise(now + 0.06, 0.14, 'bandpass', 680, 2.2, 0.22, false);

      // Stage 2 (0.42s): Fresh heavy magazine slammed into magwell + baseplate palm thud + latch lock
      this.toneAt(now + 0.42, 145, 48, 0.075, 'sine', 0.38); // Palm slap thud
      this.metallicClick(now + 0.44, 1500, 2950, 0.05, 0.32); // Steel mag-catch lock CLACK!

      // Stage 3 (0.76s): Charging handle / bolt carrier pulled sharply to the rear
      this.burstNoise(now + 0.75, 0.11, 'bandpass', 1850, 2.5, 0.25, false);
      this.metallicClick(now + 0.76, 1300, 2400, 0.06, 0.28);

      // Stage 4 (0.93s): Bolt release slams forward chambering brass round (CHCK-CLACK!)
      this.toneAt(now + 0.93, 240, 68, 0.06, 'triangle', 0.35);
      this.metallicClick(now + 0.93, 3400, 1700, 0.065, 0.36);
    }

    playMelee(meleeId = 0) {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;
      if (meleeId === 2) {
        // Ripper Chainsword motorized rev + metallic teeth shred
        this.toneAt(now, 110, 260, 0.28, 'sawtooth', 0.36);
        this.burstNoise(now, 0.26, 'bandpass', 1400, 1.5, 0.35, true);
      } else if (meleeId === 3) {
        // Hell-Breaker Warhammer seismic slam
        this.toneAt(now, 140, 28, 0.42, 'sawtooth', 0.52);
        this.burstNoise(now, 0.35, 'lowpass', 520, 0.9, 0.48, true);
      } else {
        // Bowie Knife / Crucible Katana metallic air whoosh + razor slash
        this.burstNoise(now, 0.14, 'bandpass', 1600, 1.8, 0.32, false);
        this.metallicClick(now + 0.03, 3800, 1900, 0.08, 0.25);
      }
    }

    playHit(isKill = false) {
      if (!this.enabled || !this.ctx) return;
      const now = this.ctx.currentTime;
      if (isKill) {
        this.toneAt(now, 220, 58, 0.24, 'sawtooth', 0.28);
        this.toneAt(now, 620, 920, 0.12, 'triangle', 0.2);
      } else {
        this.toneAt(now, 820, 440, 0.05, 'triangle', 0.18);
      }
    }

    playPlayerDamage() {
      if (!this.enabled || !this.ctx) return;
      const now = this.ctx.currentTime;
      this.toneAt(now, 125, 42, 0.22, 'sawtooth', 0.34);
      this.burstNoise(now, 0.15, 'lowpass', 800, 1.0, 0.28, true);
    }

    playPickup() {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;
      this.toneAt(now, 460, 920, 0.14, 'sine', 0.22);
      this.metallicClick(now + 0.05, 2400, 3200, 0.05, 0.18);
    }

    playDash() {
      if (!this.enabled || !this.ctx) return;
      const now = this.ctx.currentTime;
      this.toneAt(now, 260, 640, 0.13, 'sine', 0.2);
      this.burstNoise(now, 0.11, 'bandpass', 1200, 1.2, 0.18, false);
    }

    playOverdrive() {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;
      this.toneAt(now, 200, 880, 0.48, 'sawtooth', 0.32);
      this.toneAt(now, 330, 660, 0.48, 'triangle', 0.25);
    }

    tone(startFreq, endFreq, duration, type = 'sawtooth', gainVal = 0.2) {
      if (!this.ctx) return;
      this.toneAt(this.ctx.currentTime, startFreq, endFreq, duration, type, gainVal);
    }

    toneAt(startTime, startFreq, endFreq, duration, type = 'sawtooth', gainVal = 0.2) {
      if (!this.ctx) return;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(startFreq, startTime);
      osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), startTime + duration);
      gain.gain.setValueAtTime(gainVal, startTime);
      gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start(startTime);
      osc.stop(startTime + duration);
    }

    startMusic() {
      if (!this.enabled) return;
      this.init();
      this.stopMusic();
      const riff = [55, 55, 65.4, 55, 73.4, 55, 61.7, 58.3];
      this.musicTimer = setInterval(() => {
        if (!this.enabled || !gameState.running || gameState.paused) return;
        const freq = riff[this.step % riff.length];
        this.tone(freq, freq * 0.98, 0.16, 'sawtooth', 0.055);
        if (this.step % 4 === 0) {
          this.tone(105, 32, 0.12, 'sine', 0.09);
        }
        this.step++;
      }, 190);
    }

    stopMusic() {
      if (this.musicTimer) {
        clearInterval(this.musicTimer);
        this.musicTimer = null;
      }
    }
  }

  const sound = new SoundEngine();
  document.getElementById('btn-toggle-music').addEventListener('click', () => sound.toggle());

  // =========================================================================
  // 3. WEAPONS, MELEE, SPECIAL ABILITIES & ENEMY ARCHETYPES
  // =========================================================================
  const WEAPONS = [
    {
      id: 0,
      name: 'M4A1 HELL-CARBINE',
      desc: '5.56/7.62 NATO demonic tactical carbine with G36 carry-handle iron sights and infinite reserve magazines.',
      ammoType: 'infinite',
      ammoLabel: '[R] RELOAD // 7.62 BRIMSTONE',
      magSize: 30,
      cost: 1,
      baseDamage: 28,
      damage: 28,
      cooldown: 0.11,
      pellets: 1,
      spread: 0.015,
      adsSpread: 0.0025,
      color: 0xff7722,
      auto: true,
      resonanceGain: 5,
      recoilKick: 0.045,
      owned: true,
      level: 1,
      upgradeCost: 220
    },
    {
      id: 1,
      name: 'SPAS-12 BREACHER',
      desc: 'Heavy 12-gauge combat shotgun firing 8 superheated brimstone buckshot pellets per shell.',
      ammoType: 'shells',
      ammoLabel: '[R] RELOAD // 12G SLAG SHELLS',
      magSize: 8,
      cost: 1,
      baseDamage: 21,
      damage: 21,
      cooldown: 0.54,
      pellets: 8,
      spread: 0.082,
      adsSpread: 0.042,
      color: 0xff3b22,
      auto: false,
      resonanceGain: 12,
      recoilKick: 0.11,
      owned: true,
      level: 1,
      upgradeCost: 250
    },
    {
      id: 2,
      name: 'M249 SOUL-SHREDDER',
      desc: 'High-RPM belt-fed machine gun with 60-round box magazine. Rapidly builds Ascension Resonance.',
      ammoType: 'cells',
      ammoLabel: '[R] RELOAD // PLASMA BELT',
      magSize: 60,
      cost: 1,
      baseDamage: 25,
      damage: 25,
      cooldown: 0.085,
      pellets: 1,
      spread: 0.026,
      adsSpread: 0.007,
      color: 0x24d8ff,
      auto: true,
      resonanceGain: 6,
      recoilKick: 0.055,
      owned: true,
      level: 1,
      upgradeCost: 280
    },
    {
      id: 3,
      name: 'B.F.G. SINGULARITY',
      desc: 'Discharges an unstable void-crimson singularity orb that detonates in a massive radial shockwave.',
      ammoType: 'cells',
      ammoLabel: '25 PLASMA CELLS / CORE',
      magSize: 1,
      cost: 25,
      baseDamage: 420,
      damage: 420,
      cooldown: 1.0,
      pellets: 1,
      spread: 0,
      adsSpread: 0,
      color: 0xb85cff,
      auto: false,
      isBFG: true,
      resonanceGain: 25,
      recoilKick: 0.16,
      owned: true,
      level: 1,
      upgradeCost: 350
    },
    {
      id: 4,
      name: 'AWM ABYSSAL SNIPER',
      desc: '.338 Anti-Materiel Rail-Sniper with high-zoom optic and 2.5× headshot damage multiplier.',
      ammoType: 'shells',
      ammoLabel: '[R] RELOAD // .338 AP SLUGS',
      magSize: 5,
      cost: 1,
      baseDamage: 195,
      damage: 195,
      cooldown: 0.82,
      pellets: 1,
      spread: 0.008,
      adsSpread: 0.0005,
      color: 0xffd15c,
      auto: false,
      isSniper: true,
      resonanceGain: 18,
      recoilKick: 0.14,
      owned: false,
      buyCost: 350,
      level: 1,
      upgradeCost: 300
    }
  ];

  const MELEE_WEAPONS = [
    {
      id: 0,
      name: 'BOWIE COMBAT KNIFE',
      desc: 'Standard-issue black-ops serrated combat blade for rapid close-quarters slashes ([V]).',
      damage: 60,
      range: 3.2,
      cooldown: 0.42,
      color: 0xc8d2dc,
      owned: true,
      cost: 0
    },
    {
      id: 1,
      name: 'CRUCIBLE BLOOD-KATANA',
      desc: 'Superheated demonic nodachi that cleaves multiple hostiles in a wide 4.4m crimson arc ([V]).',
      damage: 135,
      range: 4.4,
      cooldown: 0.48,
      color: 0xff2a1a,
      owned: false,
      cost: 250
    },
    {
      id: 2,
      name: 'RIPPER CHAINSWORD',
      desc: 'Motorized high-torque demonic chainsaw blade. Shreds armor and leeches +20 Vitality HP per hit ([V]).',
      damage: 185,
      range: 3.8,
      cooldown: 0.52,
      lifesteal: 20,
      color: 0xffaa22,
      owned: false,
      cost: 400
    },
    {
      id: 3,
      name: 'HELL-BREAKER WARHAMMER',
      desc: 'Colossal obsidian maul. Slams the ground to unleash a 7m 360° seismic shockwave around you ([V]).',
      damage: 260,
      range: 7.0,
      cooldown: 0.72,
      isShockwave: true,
      color: 0xb85cff,
      owned: false,
      cost: 550
    }
  ];

  const SPECIAL_ABILITIES = [
    {
      id: 'nova',
      name: 'HELLFIRE ORBITAL NOVA',
      desc: 'Press [X] to launch a 360° ring of 12 explosive brimstone missiles that obliterate surrounding demons.',
      cost: 200,
      chargesPerBuy: 2,
      tag: 'OFFENSIVE BARRAGE'
    },
    {
      id: 'aegis',
      name: 'VAMPIRIC BLOOD-AEGIS',
      desc: 'Press [X] for 6 seconds of 100% Invulnerability Shield + all bullets leech +10 HP per hit.',
      cost: 300,
      chargesPerBuy: 2,
      tag: 'DEFENSIVE / LIFESTEAL'
    },
    {
      id: 'meteor',
      name: 'BRIMSTONE METEOR STRIKE',
      desc: 'Press [X] to call down 8 cataclysmic molten meteors from the Citadel sky onto active demons.',
      cost: 420,
      chargesPerBuy: 2,
      tag: 'APOCALYPTIC AIRSTRIKE'
    },
    {
      id: 'resupply',
      name: 'FULL TACTICAL RESUPPLY CRATE',
      desc: 'Instantly restores 100 Vitality HP, 100 Aegis Armor, +32 Slag Shells, +100 Plasma Cells, and full mags.',
      cost: 150,
      isInstant: true,
      tag: 'INSTANT SUPPLY DROP'
    }
  ];

  const ENEMY_TYPES = {
    wretch: {
      type: 'wretch',
      name: 'ASH-WRETCH',
      hp: 75,
      speed: 7.4,
      damage: 12,
      attackRange: 2.6,
      attackCooldown: 0.95,
      ranged: false,
      score: 150,
      scale: 0.9,
      armorColor: 0x3b1712,
      skinColor: 0xc83e2b,
      glowColor: 0xffaa22
    },
    imp: {
      type: 'imp',
      name: 'CINDER-IMP',
      hp: 105,
      speed: 5.5,
      damage: 14,
      attackRange: 28,
      attackCooldown: 1.65,
      ranged: true,
      projColor: 0xff6b1a,
      projSpeed: 22,
      score: 250,
      scale: 1.0,
      armorColor: 0x29151a,
      skinColor: 0x9e2a1c,
      glowColor: 0x24d8ff
    },
    gorger: {
      type: 'gorger',
      name: 'IRON-GORGER',
      hp: 260,
      speed: 3.8,
      damage: 22,
      attackRange: 26,
      attackCooldown: 1.95,
      ranged: true,
      projColor: 0xff2a1a,
      projSpeed: 19,
      score: 450,
      scale: 1.38,
      armorColor: 0x4a423d,
      skinColor: 0x5c2218,
      glowColor: 0xff3b22
    },
    cantor: {
      type: 'cantor',
      name: 'VOID-CANTOR',
      hp: 165,
      speed: 5.8,
      damage: 18,
      attackRange: 32,
      attackCooldown: 1.45,
      ranged: true,
      projColor: 0xb85cff,
      projSpeed: 26,
      canBlink: true,
      score: 380,
      scale: 1.08,
      armorColor: 0x1c0d30,
      skinColor: 0x62229c,
      glowColor: 0xffd15c
    },
    boss: {
      type: 'boss',
      name: 'ARCHON MALAKOR',
      hp: 1500,
      speed: 4.4,
      damage: 26,
      attackRange: 38,
      attackCooldown: 1.4,
      ranged: true,
      isBoss: true,
      projColor: 0xffd15c,
      projSpeed: 24,
      score: 4000,
      scale: 2.15,
      armorColor: 0x1f081e,
      skinColor: 0xd91e18,
      glowColor: 0xffd15c
    }
  };

  // Three atmospheric strata matching the screenshot's war-torn classical citadel look + demonic eclipse
  const STRATA = [
    {
      name: 'STRATUM I: CITADEL OF ASH',
      skyTop: '#241c14',
      skyHorizon: '#baa378',
      fogColor: 0x6e5d49,
      fogDensity: 0.014,
      sunColor: 0xffe4b5,
      ambientColor: 0x665847,
      spawns: [
        { type: 'wretch', x: -12, z: -10 },
        { type: 'wretch', x: 10, z: -12 },
        { type: 'imp', x: -16, z: -22 },
        { type: 'imp', x: 14, z: -20 },
        { type: 'wretch', x: 0, z: -16 },
        { type: 'imp', x: -8, z: -28 },
        { type: 'gorger', x: 6, z: -27 },
        { type: 'wretch', x: 18, z: -8 },
        { type: 'imp', x: -18, z: -6 }
      ]
    },
    {
      name: 'STRATUM II: MOLTEN BASILICA',
      skyTop: '#1f0906',
      skyHorizon: '#9c3d25',
      fogColor: 0x4d1d14,
      fogDensity: 0.015,
      sunColor: 0xff6b3b,
      ambientColor: 0x5c2820,
      spawns: [
        { type: 'wretch', x: -14, z: -8 },
        { type: 'imp', x: 14, z: -10 },
        { type: 'cantor', x: -10, z: -22 },
        { type: 'gorger', x: 10, z: -22 },
        { type: 'wretch', x: 0, z: -14 },
        { type: 'cantor', x: -18, z: -18 },
        { type: 'imp', x: 18, z: -18 },
        { type: 'gorger', x: -4, z: -29 },
        { type: 'cantor', x: 6, z: -29 },
        { type: 'wretch', x: 0, z: -24 }
      ]
    },
    {
      name: 'STRATUM III: THE EMPYREAN BREACH',
      skyTop: '#11051c',
      skyHorizon: '#6e2246',
      fogColor: 0x2e0f29,
      fogDensity: 0.014,
      sunColor: 0xff3b6b,
      ambientColor: 0x4a1c40,
      spawns: [
        { type: 'wretch', x: -14, z: -12 },
        { type: 'imp', x: 14, z: -12 },
        { type: 'cantor', x: -16, z: -24 },
        { type: 'gorger', x: 16, z: -24 },
        { type: 'cantor', x: -8, z: -20 },
        { type: 'boss', x: 0, z: -26 }
      ]
    }
  ];

  // =========================================================================
  // 4. PROCEDURAL PBR TEXTURE GENERATORS (STONE, CONCRETE, GUNMETAL, SMOKE)
  // =========================================================================
  function makeStoneTexture(baseHex,accentHex,addRunes = false) {
    const c = document.createElement('canvas');
    c.width = 256;
    c.height = 256;
    const g = c.getContext('2d');
    g.fillStyle = baseHex;
    g.fillRect(0, 0, 256, 256);

    // Granular stone weather noise
    for (let i = 0; i < 3200; i++) {
      const x = Math.random() * 256;
      const y = Math.random() * 256;
      g.fillStyle = Math.random() < 0.5 ? 'rgba(0,0,0,0.14)' : 'rgba(255,255,255,0.08)';
      g.fillRect(x, y, 3, 3);
    }

    // Classical ashlar stone block joints
    g.strokeStyle = 'rgba(15, 10, 8, 0.45)';
    g.lineWidth = 2;
    for (let y = 0; y < 256; y += 32) {
      g.beginPath();
      g.moveTo(0, y);
      g.lineTo(256, y);
      g.stroke();
      const shift = (y / 32) % 2 === 0 ? 0 : 32;
      for (let x = shift; x < 256; x += 64) {
        g.beginPath();
        g.moveTo(x, y);
        g.lineTo(x, y + 32);
        g.stroke();
      }
    }

    if (addRunes) {
      g.fillStyle = accentHex;
      g.fillRect(0, 122, 256, 4);
      for (let x = 16; x < 256; x += 32) {
        g.fillRect(x, 114, 6, 20);
      }
    }

    const tex = new THREE.CanvasTexture(c);
    tex.wrapS = THREE.RepeatWrapping;
    tex.wrapT = THREE.RepeatWrapping;
    return tex;
  }

  function makeFlutedColumnTexture() {
    const c = document.createElement('canvas');
    c.width = 256;
    c.height = 256;
    const g = c.getContext('2d');
    g.fillStyle = '#9c9282';
    g.fillRect(0, 0, 256, 256);
    for (let x = 0; x < 256; x += 16) {
      const grad = g.createLinearGradient(x, 0, x + 16, 0);
      grad.addColorStop(0, '#5e564b');
      grad.addColorStop(0.5, '#b8ad9a');
      grad.addColorStop(1, '#4a433a');
      g.fillStyle = grad;
      g.fillRect(x, 0, 16, 256);
    }
    // Scorch & demonic blood soot at bottom
    const soot = g.createLinearGradient(0, 160, 0, 256);
    soot.addColorStop(0, 'rgba(20,10,8,0)');
    soot.addColorStop(1, 'rgba(25,8,5,0.65)');
    g.fillStyle = soot;
    g.fillRect(0, 160, 256, 96);

    const tex = new THREE.CanvasTexture(c);
    tex.wrapS = THREE.RepeatWrapping;
    tex.wrapT = THREE.RepeatWrapping;
    return tex;
  }

  function makeSmokeSpriteTexture() {
    const c = document.createElement('canvas');
    c.width = 128;
    c.height = 128;
    const g = c.getContext('2d');
    const grad = g.createRadialGradient(64, 64, 6, 64, 64, 60);
    grad.addColorStop(0, 'rgba(185, 175, 160, 0.52)');
    grad.addColorStop(0.5, 'rgba(110, 100, 90, 0.25)');
    grad.addColorStop(1, 'rgba(40, 35, 30, 0)');
    g.fillStyle = grad;
    g.fillRect(0, 0, 128, 128);
    return new THREE.CanvasTexture(c);
  }

  // =========================================================================
  // 5. THREE.JS SCENE, CAMERA, VIEWMODEL & CITADEL ARCHITECTURE
  // =========================================================================
  const container = document.getElementById('webgl-container');
  const miniCanvas = document.getElementById('minimap-canvas');
  const miniCtx = miniCanvas.getContext('2d');

  const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
  renderer.setSize(container.clientWidth || 960, container.clientHeight || 540);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.1;
  container.appendChild(renderer.domElement);

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x6e5d49, 0.014);

  const camera = new THREE.PerspectiveCamera(
    70,
    (container.clientWidth || 960) / (container.clientHeight || 540),
    0.08,
    260
  );
  camera.rotation.order = 'YXZ';
  scene.add(camera);

  window.addEventListener('resize', () => {
    const w = container.clientWidth || window.innerWidth;
    const h = container.clientHeight || window.innerHeight;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h);
  });

  // Lighting
  const ambientLight = new THREE.AmbientLight(0x665847, 1.15);
  scene.add(ambientLight);

  const sunLight = new THREE.DirectionalLight(0xffe4b5, 1.85);
  sunLight.position.set(-45, 55, -40);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.width = 1024;
  sunLight.shadow.mapSize.height = 1024;
  sunLight.shadow.camera.near = 5;
  sunLight.shadow.camera.far = 150;
  const d = 48;
  sunLight.shadow.camera.left = -d;
  sunLight.shadow.camera.right = d;
  sunLight.shadow.camera.top = d;
  sunLight.shadow.camera.bottom = -d;
  scene.add(sunLight);

  // Muzzle flash dynamic point light attached to camera
  const muzzleLight = new THREE.PointLight(0xff8822, 0, 14);
  camera.add(muzzleLight);
  muzzleLight.position.set(0.2, -0.1, -1.1);

  // Shared Textures
  const wallStoneTex = makeStoneTexture('#918675', '#ff3b22', false);
  const darkObsidianTex = makeStoneTexture('#2a2522', '#ff3b22', true);
  const groundTex = makeStoneTexture('#4a443c', '#ff5522', false);
  groundTex.repeat.set(24, 24);
  const columnTex = makeFlutedColumnTexture();
  const smokeTex = makeSmokeSpriteTexture();

  // Collision boxes in 3D plaza: { minX, maxX, minZ, maxZ, maxY }
  const colliders = [];
  const shootableMeshes = [];
  let skyDomeMesh = null;
  let portalGroup = null;
  let portalRingMesh = null;
  const smokePlumes = [];
  const sparkEmitters = [];

  function addBoxCollider(x, y, z, w, h, d, mat, castShadow = true) {
    const geo = new THREE.BoxGeometry(w, h, d);
    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.set(x, y, z);
    mesh.castShadow = castShadow;
    mesh.receiveShadow = true;
    scene.add(mesh);
    shootableMeshes.push(mesh);
    colliders.push({
      minX: x - w / 2,
      maxX: x + w / 2,
      minZ: z - d / 2,
      maxZ: z + d / 2,
      maxY: y + h / 2
    });
    return mesh;
  }

  // Build the iconic War-Torn Neoclassical/Gothic Demonic Citadel Plaza from the reference image!
  function buildCitadelWorld() {
    // 1. Sky Dome with volumetric cloud & demonic eclipse gradient
    const skyGeo = new THREE.SphereGeometry(190, 32, 24);
    const skyCanvas = document.createElement('canvas');
    skyCanvas.width = 512;
    skyCanvas.height = 256;
    skyDomeMesh = new THREE.Mesh(
      skyGeo,
      new THREE.MeshBasicMaterial({ side: THREE.BackSide, fog: false })
    );
    scene.add(skyDomeMesh);
    updateSkyTexture(0);

    // 2. Plaza Floor (Cobblestone & Scorched Concrete)
    const floorMat = new THREE.MeshStandardMaterial({
      map: groundTex,
      roughness: 0.88,
      metalness: 0.08
    });
    const floorMesh = new THREE.Mesh(new THREE.PlaneGeometry(140, 140), floorMat);
    floorMesh.rotation.x = -Math.PI / 2;
    floorMesh.receiveShadow = true;
    scene.add(floorMesh);
    shootableMeshes.push(floorMesh);

    // Glowing Demonic Summoning Sigil in the center of the courtyard
    const sigilGeo = new THREE.RingGeometry(4.2, 4.6, 32);
    const sigilMat = new THREE.MeshBasicMaterial({ color: 0xff3b22, side: THREE.DoubleSide });
    const sigilMesh = new THREE.Mesh(sigilGeo, sigilMat);
    sigilMesh.rotation.x = -Math.PI / 2;
    sigilMesh.position.set(0, 0.03, -12);
    scene.add(sigilMesh);

    // Materials
    const limestoneMat = new THREE.MeshStandardMaterial({
      map: wallStoneTex,
      roughness: 0.78,
      metalness: 0.1
    });
    const darkStoneMat = new THREE.MeshStandardMaterial({
      map: darkObsidianTex,
      roughness: 0.65,
      metalness: 0.22
    });
    const columnMat = new THREE.MeshStandardMaterial({
      map: columnTex,
      roughness: 0.7,
      metalness: 0.12
    });
    const rustTrimMat = new THREE.MeshStandardMaterial({
      color: 0x7a3b28,
      roughness: 0.55,
      metalness: 0.35
    });

    // 3. GRAND NEOCLASSICAL DEMONIC CITADEL (Center-Left Background, matching screenshot!)
    // Main Citadel Hall Building
    addBoxCollider(0, 9, -40, 38, 18, 12, limestoneMat);
    // Domed / Mansard Roof & Chimneys (matching screenshot skyline)
    addBoxCollider(0, 20.5, -40, 32, 5, 10, darkStoneMat);
    addBoxCollider(-8, 25, -39, 2.2, 5, 2.2, limestoneMat);
    addBoxCollider(8, 25, -39, 2.2, 5, 2.2, limestoneMat);

    // Portico Raised Steps & Porch
    addBoxCollider(0, 0.6, -31.5, 22, 1.2, 6, darkStoneMat, false);

    // 6 Towering Fluted Classical Columns across the Portico
    const colPositions = [-8.5, -5.1, -1.7, 1.7, 5.1, 8.5];
    colPositions.forEach(cx => {
      const colMesh = new THREE.Mesh(new THREE.CylinderGeometry(0.72, 0.82, 13.5, 18), columnMat);
      colMesh.position.set(cx, 7.8, -29.2);
      colMesh.castShadow = true;
      colMesh.receiveShadow = true;
      scene.add(colMesh);
      shootableMeshes.push(colMesh);
      colliders.push({
        minX: cx - 0.85,
        maxX: cx + 0.85,
        minZ: -29.2 - 0.85,
        maxZ: -29.2 + 0.85,
        maxY: 14.5
      });

      // Column Capital & Base
      const base = new THREE.Mesh(new THREE.BoxGeometry(1.9, 0.8, 1.9), limestoneMat);
      base.position.set(cx, 1.4, -29.2);
      scene.add(base);
      const cap = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.9, 2.0), limestoneMat);
      cap.position.set(cx, 14.4, -29.2);
      scene.add(cap);
    });

    // Entablature Frieze & Triangular Classical Pediment above the Columns!
    addBoxCollider(0, 15.6, -29.2, 20.5, 1.6, 2.6, limestoneMat);

    // Triangular Pediment (matching screenshot!)
    const pedShape = new THREE.Shape();
    pedShape.moveTo(-10.4, 0);
    pedShape.lineTo(10.4, 0);
    pedShape.lineTo(0, 4.8);
    pedShape.closePath();
    const pedGeo = new THREE.ExtrudeGeometry(pedShape, { depth: 2.2, bevelEnabled: false });
    const pedMesh = new THREE.Mesh(pedGeo, limestoneMat);
    pedMesh.position.set(0, 16.4, -30.3);
    pedMesh.castShadow = true;
    scene.add(pedMesh);
    shootableMeshes.push(pedMesh);

    // Glowing Hellfire Windows on the Citadel Facade
    const winMat = new THREE.MeshBasicMaterial({ color: 0xff9933 });
    [-12, -6.8, 6.8, 12].forEach(wx => {
      const win = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 5.5), winMat);
      win.position.set(wx, 8.5, -33.9);
      scene.add(win);
    });

    // 4. CITADEL ASCENSION PORTAL (Between the Center Columns at Z = -31)
    portalGroup = new THREE.Group();
    portalGroup.position.set(0, 4.2, -31.2);
    const pRingGeo = new THREE.TorusGeometry(2.3, 0.22, 16, 40);
    const pRingMat = new THREE.MeshBasicMaterial({ color: 0x7722ff });
    portalRingMesh = new THREE.Mesh(pRingGeo, pRingMat);
    portalGroup.add(portalRingMesh);

    const pCoreGeo = new THREE.CircleGeometry(2.1, 32);
    const pCoreMat = new THREE.MeshBasicMaterial({
      color: 0xff3b22,
      transparent: true,
      opacity: 0.35,
      side: THREE.DoubleSide
    });
    const pCoreMesh = new THREE.Mesh(pCoreGeo, pCoreMat);
    portalGroup.add(pCoreMesh);
    scene.add(portalGroup);

    // 5. RIGHT FOREGROUND WEATHERED STONE ARCH & PILLAR (Directly from the reference image!)
    addBoxCollider(8.5, 6.5, 4.5, 4.2, 13, 4.2, limestoneMat);
    // Rust-red metallic trim ledge on the right foreground pillar (matching screenshot!)
    const trimMesh = new THREE.Mesh(new THREE.BoxGeometry(4.5, 0.55, 4.5), rustTrimMat);
    trimMesh.position.set(8.5, 3.2, 4.5);
    scene.add(trimMesh);

    // 6. PERIMETER RUINED BUILDINGS & SKYLINE WALLS (Keeping player inside the 68x68 warzone)
    addBoxCollider(-28, 10, -10, 8, 20, 64, limestoneMat); // Left avenue buildings
    addBoxCollider(28, 10, -10, 8, 20, 64, limestoneMat);  // Right avenue buildings
    addBoxCollider(0, 8, 22, 64, 16, 6, darkStoneMat);     // Rear boundary wall

    // Distant Background Ruined Skyscraper Silhouettes (Left horizon like screenshot)
    const distantTower1 = new THREE.Mesh(new THREE.BoxGeometry(10, 42, 10), darkStoneMat);
    distantTower1.position.set(-36, 21, -48);
    scene.add(distantTower1);
    const distantTower2 = new THREE.Mesh(new THREE.BoxGeometry(8, 34, 8), limestoneMat);
    distantTower2.position.set(-22, 17, -54);
    scene.add(distantTower2);

    // 7. BURNED-OUT WRECKAGE HULLS, RUBBLE BARRICADES & SPARKING LAMPPOST (Matching screenshot!)
    const charredMetalMat = new THREE.MeshStandardMaterial({
      color: 0x24201d,
      roughness: 0.6,
      metalness: 0.65
    });

    // Wrecked Armored Vehicles / Demonic Transports on the left street
    const wreckCoords = [
      { x: -11, z: -4, ry: 0.28 },
      { x: -15, z: -12, ry: -0.42 },
      { x: -6, z: -9, ry: 0.12 },
      { x: 11, z: -11, ry: -0.3 },
      { x: 5, z: -18, ry: 0.45 },
      { x: -10, z: -21, ry: -0.18 }
    ];
    wreckCoords.forEach(w => {
      const hull = addBoxCollider(w.x, 0.85, w.z, 3.8, 1.5, 2.1, charredMetalMat);
      hull.rotation.y = w.ry;
      const cab = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.9, 1.8), charredMetalMat);
      cab.position.set(w.x, 1.9, w.z);
      cab.rotation.y = w.ry;
      scene.add(cab);
      shootableMeshes.push(cab);

      // Spawn volumetric smoke plume above wreck
      createSmokePlume(w.x, 1.2, w.z);
    });

    // Sparking Broken Streetlamp / Demonic Brazier in Center-Left Foreground (Just like screenshot!)
    const poleMesh = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.1, 4.2, 8), charredMetalMat);
    poleMesh.position.set(-2.2, 2.1, 1.5);
    poleMesh.rotation.z = -0.14;
    scene.add(poleMesh);

    const lampOrb = new THREE.Mesh(
      new THREE.SphereGeometry(0.32, 12, 12),
      new THREE.MeshBasicMaterial({ color: 0xffcc44 })
    );
    lampOrb.position.set(-1.9, 3.9, 1.5);
    scene.add(lampOrb);

    const sparkLight = new THREE.PointLight(0xffaa33, 1.6, 16);
    sparkLight.position.set(-1.9, 3.9, 1.5);
    scene.add(sparkLight);
    sparkEmitters.push({ pos: new THREE.Vector3(-1.9, 3.9, 1.5), light: sparkLight });

    // Overhead Swaying Power Cables / Chains across the Plaza (Matching screenshot!)
    const cableMat = new THREE.LineBasicMaterial({ color: 0x1a1612 });
    [
      [new THREE.Vector3(-24, 11.5, 6), new THREE.Vector3(0, 9.8, 2), new THREE.Vector3(24, 12.0, 5)],
      [new THREE.Vector3(-24, 13.0, -6), new THREE.Vector3(-2, 11.2, -10), new THREE.Vector3(24, 13.5, -8)]
    ].forEach(pts => {
      const curve = new THREE.QuadraticBezierCurve3(pts[0], pts[1], pts[2]);
      const cableGeo = new THREE.BufferGeometry().setFromPoints(curve.getPoints(24));
      scene.add(new THREE.Line(cableGeo, cableMat));
    });
  }

  function updateSkyTexture(stratumIdx) {
    const st = STRATA[stratumIdx % STRATA.length];
    const c = document.createElement('canvas');
    c.width = 512;
    c.height = 256;
    const g = c.getContext('2d');

    const grad = g.createLinearGradient(0, 0, 0, 256);
    grad.addColorStop(0, st.skyTop);
    grad.addColorStop(0.52, st.skyHorizon);
    grad.addColorStop(1, '#1a1410');
    g.fillStyle = grad;
    g.fillRect(0, 0, 512, 256);

    // Dramatic volumetric storm clouds & glowing sun/eclipse on left horizon (matching screenshot!)
    const sunGrad = g.createRadialGradient(140, 115, 6, 140, 115, 85);
    sunGrad.addColorStop(0, '#fffdf2');
    sunGrad.addColorStop(0.25, st.skyHorizon);
    sunGrad.addColorStop(1, 'rgba(0,0,0,0)');
    g.fillStyle = sunGrad;
    g.fillRect(0, 0, 512, 256);

    if (skyDomeMesh) {
      skyDomeMesh.material.map = new THREE.CanvasTexture(c);
      skyDomeMesh.material.needsUpdate = true;
    }
    scene.fog.color.setHex(st.fogColor);
    scene.fog.density = st.fogDensity;
    sunLight.color.setHex(st.sunColor);
    ambientLight.color.setHex(st.ambientColor);
  }

  function createSmokePlume(x, y, z) {
    for (let i = 0; i < 5; i++) {
      const spMat = new THREE.SpriteMaterial({
        map: smokeTex,
        transparent: true,
        opacity: 0.42,
        depthWrite: false
      });
      const sp = new THREE.Sprite(spMat);
      sp.position.set(x + (Math.random() - 0.5) * 1.5, y + i * 1.2, z + (Math.random() - 0.5) * 1.5);
      const sc = 3.2 + Math.random() * 2.8;
      sp.scale.set(sc, sc, 1);
      scene.add(sp);
      smokePlumes.push({
        sprite: sp,
        baseX: x,
        baseY: y,
        baseZ: z,
        speed: 1.1 + Math.random() * 0.8,
        phase: Math.random() * Math.PI * 2
      });
    }
  }

  buildCitadelWorld();

  // =========================================================================
  // 6. DETAILED 3D FIRST-PERSON WEAPON VIEWMODEL & GLOVED ARMS (LIKE SCREENSHOT!)
  // =========================================================================
  const weaponRig = new THREE.Group();
  camera.add(weaponRig);

  // Hip-fire position (bottom-right angled toward center, matching screenshot!)
  const HIP_POS = new THREE.Vector3(0.27, -0.25, -0.46);
  const HIP_ROT = new THREE.Euler(0.02, -0.08, 0.02, 'YXZ');

  // Right-Click ADS (Aim Down Sights) position aligned with camera center!
  const ADS_POS = new THREE.Vector3(0.0, -0.148, -0.34);
  const ADS_ROT = new THREE.Euler(0.0, 0.0, 0.0, 'YXZ');

  weaponRig.position.copy(HIP_POS);
  weaponRig.rotation.copy(HIP_ROT);

  const gunGroup = new THREE.Group();
  weaponRig.add(gunGroup);

  let boltMesh = null;
  let magMesh = null;
  let muzzleMesh = null;
  let runeStripMat = null;
  let barrelTwinGroup = null;
  let bfgCoreMesh = null;
  let sniperScopeGroup = null;
  let meleeViewGroup = null;
  let meleeBladeMat = null;

  function build3DWeaponViewmodel() {
    const gunmetalMat = new THREE.MeshStandardMaterial({
      color: 0x1c1e22,
      roughness: 0.32,
      metalness: 0.85
    });
    const darkPolymerMat = new THREE.MeshStandardMaterial({
      color: 0x111215,
      roughness: 0.65,
      metalness: 0.25
    });
    const chromeBoltMat = new THREE.MeshStandardMaterial({
      color: 0x8a929a,
      roughness: 0.22,
      metalness: 0.92
    });
    const gloveMat = new THREE.MeshStandardMaterial({
      color: 0x181a18,
      roughness: 0.8,
      metalness: 0.1
    });
    const sleeveMat = new THREE.MeshStandardMaterial({
      color: 0x2b3026,
      roughness: 0.88,
      metalness: 0.05
    });
    runeStripMat = new THREE.MeshBasicMaterial({ color: 0xff6611 });

    // 1. Main Receiver Body (G36C / Tactical Carbine profile from screenshot)
    const receiver = new THREE.Mesh(new THREE.BoxGeometry(0.068, 0.095, 0.46), gunmetalMat);
    receiver.position.set(0, 0, 0);
    gunGroup.add(receiver);

    // 2. Ejection Port & Moving Chrome Bolt Carrier
    boltMesh = new THREE.Mesh(new THREE.BoxGeometry(0.072, 0.032, 0.11), chromeBoltMat);
    boltMesh.position.set(0.004, 0.015, 0.02);
    gunGroup.add(boltMesh);

    // 3. Elevated Carry-Handle Top Picatinny Rail Bridge (Iconic G36 silhouette in screenshot!)
    const railFrontPost = new THREE.Mesh(new THREE.BoxGeometry(0.044, 0.045, 0.04), gunmetalMat);
    railFrontPost.position.set(0, 0.065, -0.18);
    gunGroup.add(railFrontPost);

    const railRearPost = new THREE.Mesh(new THREE.BoxGeometry(0.044, 0.045, 0.05), gunmetalMat);
    railRearPost.position.set(0, 0.065, 0.16);
    gunGroup.add(railRearPost);

    const topRailBridge = new THREE.Mesh(new THREE.BoxGeometry(0.046, 0.022, 0.42), gunmetalMat);
    topRailBridge.position.set(0, 0.092, -0.01);
    gunGroup.add(topRailBridge);

    // Picatinny Rail Slots along the top bridge
    for (let z = -0.19; z <= 0.17; z += 0.028) {
      const tooth = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.01, 0.012), darkPolymerMat);
      tooth.position.set(0, 0.106, z);
      gunGroup.add(tooth);
    }

    // 4. Front & Rear Circular Peep Iron Sights (Aligned with ADS_POS!)
    const frontHood = new THREE.Mesh(new THREE.TorusGeometry(0.018, 0.0035, 8, 16), gunmetalMat);
    frontHood.position.set(0, 0.128, -0.19);
    gunGroup.add(frontHood);

    const frontDot = new THREE.Mesh(
      new THREE.SphereGeometry(0.0038, 8, 8),
      new THREE.MeshBasicMaterial({ color: 0xff3b22 })
    );
    frontDot.position.set(0, 0.126, -0.19);
    gunGroup.add(frontDot);

    const rearAperture = new THREE.Mesh(new THREE.TorusGeometry(0.016, 0.0035, 8, 16), gunmetalMat);
    rearAperture.position.set(0, 0.128, 0.16);
    gunGroup.add(rearAperture);

    // 5. Forend Ribbed Handguard & Demonic Glowing Rune Vents
    const handguard = new THREE.Mesh(new THREE.BoxGeometry(0.062, 0.078, 0.28), darkPolymerMat);
    handguard.position.set(0, -0.005, -0.32);
    gunGroup.add(handguard);

    const runeCore = new THREE.Mesh(new THREE.BoxGeometry(0.066, 0.016, 0.22), runeStripMat);
    runeCore.position.set(0, -0.005, -0.32);
    gunGroup.add(runeCore);

    // 6. Primary Rifle Barrel & Flash Hider
    const barrel = new THREE.Mesh(new THREE.CylinderGeometry(0.014, 0.014, 0.26, 12), gunmetalMat);
    barrel.rotation.x = Math.PI / 2;
    barrel.position.set(0, 0.005, -0.52);
    gunGroup.add(barrel);

    // Optional Twin Shotgun Tubes (Visible when Slot 2 Shotgun equipped)
    barrelTwinGroup = new THREE.Group();
    const tubeL = new THREE.Mesh(new THREE.CylinderGeometry(0.018, 0.018, 0.3, 12), gunmetalMat);
    tubeL.rotation.x = Math.PI / 2;
    tubeL.position.set(-0.02, 0.01, -0.5);
    const tubeR = new THREE.Mesh(new THREE.CylinderGeometry(0.018, 0.018, 0.3, 12), gunmetalMat);
    tubeR.rotation.x = Math.PI / 2;
    tubeR.position.set(0.02, 0.01, -0.5);
    barrelTwinGroup.add(tubeL, tubeR);
    barrelTwinGroup.visible = false;
    gunGroup.add(barrelTwinGroup);

    // Optional BFG Singularity Core Orb (Visible when Slot 4 BFG equipped)
    bfgCoreMesh = new THREE.Mesh(
      new THREE.SphereGeometry(0.048, 16, 16),
      new THREE.MeshBasicMaterial({ color: 0xb85cff, wireframe: true })
    );
    bfgCoreMesh.position.set(0, 0.02, -0.56);
    bfgCoreMesh.visible = false;
    gunGroup.add(bfgCoreMesh);

    // Optional AWM Sniper Optic Scope & Long Fluted Barrel (Visible when Slot 5 Sniper equipped)
    sniperScopeGroup = new THREE.Group();
    const scopeTube = new THREE.Mesh(new THREE.CylinderGeometry(0.022, 0.022, 0.24, 14), gunmetalMat);
    scopeTube.rotation.x = Math.PI / 2;
    scopeTube.position.set(0, 0.135, -0.02);
    const scopeLens = new THREE.Mesh(
      new THREE.CircleGeometry(0.019, 12),
      new THREE.MeshBasicMaterial({ color: 0xffd15c })
    );
    scopeLens.position.set(0, 0.135, 0.101);
    const longBarrel = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.015, 0.38, 12), chromeBoltMat);
    longBarrel.rotation.x = Math.PI / 2;
    longBarrel.position.set(0, 0.005, -0.68);
    sniperScopeGroup.add(scopeTube, scopeLens, longBarrel);
    sniperScopeGroup.visible = false;
    gunGroup.add(sniperScopeGroup);

    // 3D Melee Blade / Katana / Chainsword / Warhammer Group (Swings on [V] Melee Attack!)
    meleeViewGroup = new THREE.Group();
    meleeBladeMat = new THREE.MeshStandardMaterial({
      color: 0xc8d2dc,
      roughness: 0.2,
      metalness: 0.9,
      emissive: 0x330500
    });
    const hilt = new THREE.Mesh(new THREE.CylinderGeometry(0.016, 0.016, 0.16, 8), darkPolymerMat);
    const bladeMesh = new THREE.Mesh(new THREE.BoxGeometry(0.014, 0.52, 0.055), meleeBladeMat);
    bladeMesh.position.set(0, 0.32, 0);
    meleeViewGroup.add(hilt, bladeMesh);
    meleeViewGroup.position.set(-0.22, -0.15, -0.38);
    meleeViewGroup.visible = false;
    weaponRig.add(meleeViewGroup);

    // 7. Detachable Curved Magazine (Animates during [R] Reload!)
    magMesh = new THREE.Mesh(new THREE.BoxGeometry(0.042, 0.16, 0.068), darkPolymerMat);
    magMesh.position.set(0, -0.11, -0.08);
    magMesh.rotation.x = 0.18;
    gunGroup.add(magMesh);

    // 8. Pistol Grip & Buttstock
    const grip = new THREE.Mesh(new THREE.BoxGeometry(0.044, 0.12, 0.055), darkPolymerMat);
    grip.position.set(0, -0.09, 0.12);
    grip.rotation.x = -0.28;
    gunGroup.add(grip);

    // 9. 3D Gloved Hands & Ribbed Cuff Forearms (Matching the left hand + sleeve in the screenshot!)
    const leftGlove = new THREE.Mesh(new THREE.BoxGeometry(0.075, 0.068, 0.11), gloveMat);
    leftGlove.position.set(-0.032, -0.048, -0.26);
    leftGlove.rotation.z = 0.25;
    gunGroup.add(leftGlove);

    const leftCuff = new THREE.Mesh(new THREE.CylinderGeometry(0.052, 0.064, 0.34, 12), sleeveMat);
    leftCuff.position.set(-0.11, -0.16, -0.08);
    leftCuff.rotation.x = 1.05;
    leftCuff.rotation.z = -0.38;
    gunGroup.add(leftCuff);

    const rightGlove = new THREE.Mesh(new THREE.BoxGeometry(0.068, 0.068, 0.09), gloveMat);
    rightGlove.position.set(0.01, -0.08, 0.12);
    gunGroup.add(rightGlove);

    // 10. 3D Star Muzzle Flash Mesh
    const flashGeo = new THREE.ConeGeometry(0.09, 0.28, 6);
    const flashMat = new THREE.MeshBasicMaterial({
      color: 0xffcc44,
      transparent: true,
      opacity: 0.9
    });
    muzzleMesh = new THREE.Mesh(flashGeo, flashMat);
    muzzleMesh.rotation.x = -Math.PI / 2;
    muzzleMesh.position.set(0, 0.008, -0.72);
    muzzleMesh.visible = false;
    gunGroup.add(muzzleMesh);
  }

  build3DWeaponViewmodel();

  // =========================================================================
  // 7. CORE GAME STATE, PLAYER PHYSICS & CONTROLS
  // =========================================================================
  let selectedMode = 'campaign';
  let difficultyMult = 1.0;

  const gameState = {
    running: false,
    paused: false,
    shopOpen: false,
    activeShopCat: 'guns',
    mode: 'campaign',
    stratumIndex: 0,
    wave: 1,
    score: 0,
    kills: 0,
    multiplier: 1.0,
    peakMultiplier: 1.0,
    comboTimer: 0,
    elapsedTime: 0,
    blitzTimer: 150,
    overdriveActive: false,
    overdriveTimer: 0,
    vampiricTimer: 0,
    ascensionGauge: 0,
    beaconUnlocked: false,
    muzzleTimer: 0,
    recoilOffset: 0,
    recoilPitch: 0,
    bobPhase: 0,
    isADS: false,
    reloading: false,
    reloadTimer: 0,
    meleeSwingTimer: 0,
    enemies: [],
    projectiles: [],
    pickups: [],
    casings: [],
    particles: [],
    decals: [],
    totalStratumEnemies: 0
  };

  const player = {
    x: 0,
    y: 1.75,
    z: 12,
    vy: 0,
    yaw: 0,
    pitch: 0,
    onGround: true,
    hp: 100,
    maxHp: 100,
    armor: 50,
    maxArmor: 100,
    credits: 500,
    shells: 32,
    maxShells: 64,
    cells: 120,
    maxCells: 240,
    mags: [30, 8, 60, 1, 5],
    weaponIndex: 0,
    meleeIndex: 0,
    meleeCooldown: 0,
    equippedAbility: 'nova',
    abilityCharges: { nova: 1, aegis: 0, meteor: 0 },
    fireCooldown: 0,
    dashCooldown: 0,
    dashTimer: 0,
    dashVx: 0,
    dashVz: 0,
    invulnTimer: 0
  };

  const keys = {};
  let mouseFiring = false;

  // Mode & Difficulty selection UI
  document.querySelectorAll('.mode-card').forEach(card => {
    card.addEventListener('click', () => {
      document.querySelectorAll('.mode-card').forEach(c => c.classList.remove('selected'));
      card.classList.add('selected');
      selectedMode = card.dataset.mode;
    });
  });

  document.querySelectorAll('.diff-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.diff-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      difficultyMult = parseFloat(btn.dataset.diff);
    });
  });

  document.querySelectorAll('.w-slot').forEach(slotBtn => {
    slotBtn.addEventListener('click', () => {
      selectWeapon(parseInt(slotBtn.dataset.slot, 10));
    });
  });

  function selectWeapon(idx) {
    if (idx < 0 || idx >= WEAPONS.length) return;
    const w = WEAPONS[idx];
    if (!w.owned) {
      addKillfeed(`🔒 ${w.name} IS LOCKED — PRESS [B] TO BUY IN ARMORY SHOP!`);
      return;
    }
    player.weaponIndex = idx;
    gameState.reloading = false;
    document.getElementById('reload-indicator').classList.add('hidden');
    if (runeStripMat) runeStripMat.color.setHex(w.color);
    if (barrelTwinGroup) barrelTwinGroup.visible = idx === 1;
    if (bfgCoreMesh) bfgCoreMesh.visible = idx === 3;
    if (sniperScopeGroup) sniperScopeGroup.visible = idx === 4;
    updateHUD();
  }

  window.addEventListener('keydown', e => {
    keys[e.code] = true;

    if (e.code === 'KeyB') {
      e.preventDefault();
      toggleShopModal();
      return;
    }

    if (!gameState.running || gameState.shopOpen) return;

    if (e.code === 'Digit1') selectWeapon(0);
    if (e.code === 'Digit2') selectWeapon(1);
    if (e.code === 'Digit3') selectWeapon(2);
    if (e.code === 'Digit4') selectWeapon(3);
    if (e.code === 'Digit5') selectWeapon(4);
    if (e.code === 'KeyM') sound.toggle();
    if (e.code === 'KeyR') startReload();
    if (e.code === 'KeyV') triggerMeleeAttack();
    if (e.code === 'KeyX') triggerSpecialAbility();

    if (e.code === 'Space' && player.onGround && !gameState.paused) {
      e.preventDefault();
      player.vy = 7.8;
      player.onGround = false;
    }

    if (e.code === 'ShiftLeft' || e.code === 'ShiftRight') {
      e.preventDefault();
      triggerDash();
    }

    if (e.code === 'KeyQ' || e.code === 'KeyE') {
      triggerAscensionOverdrive();
    }

    if (e.code === 'KeyP' || e.code === 'Escape') {
      if (!document.getElementById('state-modal').classList.contains('hidden')) return;
      togglePauseMenu();
    }
  });

  window.addEventListener('keyup', e => {
    keys[e.code] = false;
  });

  // Prevent context menu on Right-Click ADS
  window.addEventListener('contextmenu', e => e.preventDefault());

  container.addEventListener('mousedown', e => {
    if (!gameState.running || gameState.paused) return;
    if (document.pointerLockElement !== renderer.domElement && renderer.domElement.requestPointerLock) {
      renderer.domElement.requestPointerLock();
    }
    if (e.button === 0) {
      mouseFiring = true;
      attemptFire();
    } else if (e.button === 2) {
      gameState.isADS = true;
      document.getElementById('crosshair').style.opacity = '0.2';
    }
  });

  window.addEventListener('mouseup', e => {
    if (e.button === 0) mouseFiring = false;
    if (e.button === 2) {
      gameState.isADS = false;
      document.getElementById('crosshair').style.opacity = '1';
    }
  });

  window.addEventListener('mousemove', e => {
    if (document.pointerLockElement === renderer.domElement && gameState.running && !gameState.paused) {
      const sens = gameState.isADS ? 0.0014 : 0.0023;
      player.yaw -= e.movementX * sens;
      player.pitch = Math.max(-1.25, Math.min(1.25, player.pitch - e.movementY * sens));
    }
  });

  window.addEventListener('wheel', e => {
    if (!gameState.running || gameState.paused) return;
    const dir = e.deltaY > 0 ? 1 : -1;
    selectWeapon((player.weaponIndex + dir + WEAPONS.length) % WEAPONS.length);
  });

  // =========================================================================
  // 8. 3D ARTICULATED DEMON BUILDER & SPAWNER
  // =========================================================================
  function createDemonMesh3D(arch) {
    const group = new THREE.Group();
    const s = arch.scale;

    const armorMat = new THREE.MeshStandardMaterial({
      color: arch.armorColor,
      roughness: 0.45,
      metalness: 0.65
    });
    const skinMat = new THREE.MeshStandardMaterial({
      color: arch.skinColor,
      roughness: 0.7,
      metalness: 0.15
    });
    const glowMat = new THREE.MeshBasicMaterial({ color: arch.glowColor });

    // Legs
    const leftLeg = new THREE.Mesh(new THREE.BoxGeometry(0.28 * s, 0.85 * s, 0.28 * s), armorMat);
    leftLeg.position.set(-0.22 * s, 0.42 * s, 0);
    leftLeg.castShadow = true;
    group.add(leftLeg);

    const rightLeg = new THREE.Mesh(new THREE.BoxGeometry(0.28 * s, 0.85 * s, 0.28 * s), armorMat);
    rightLeg.position.set(0.22 * s, 0.42 * s, 0);
    rightLeg.castShadow = true;
    group.add(rightLeg);

    // Armored Torso & Glowing Core
    const torso = new THREE.Mesh(new THREE.BoxGeometry(0.82 * s, 1.05 * s, 0.48 * s), skinMat);
    torso.position.set(0, 1.32 * s, 0);
    torso.castShadow = true;
    group.add(torso);

    const chestPlate = new THREE.Mesh(new THREE.BoxGeometry(0.88 * s, 0.62 * s, 0.52 * s), armorMat);
    chestPlate.position.set(0, 1.44 * s, 0);
    group.add(chestPlate);

    const core = new THREE.Mesh(new THREE.BoxGeometry(0.28 * s, 0.28 * s, 0.55 * s), glowMat);
    core.position.set(0, 1.42 * s, 0);
    group.add(core);

    // Head & Curved Demon Horns
    const head = new THREE.Mesh(new THREE.BoxGeometry(0.46 * s, 0.46 * s, 0.46 * s), skinMat);
    head.position.set(0, 2.08 * s, 0);
    head.castShadow = true;
    group.add(head);

    const eyeL = new THREE.Mesh(new THREE.BoxGeometry(0.1 * s, 0.06 * s, 0.05 * s), glowMat);
    eyeL.position.set(-0.11 * s, 2.12 * s, 0.24 * s);
    const eyeR = new THREE.Mesh(new THREE.BoxGeometry(0.1 * s, 0.06 * s, 0.05 * s), glowMat);
    eyeR.position.set(0.11 * s, 2.12 * s, 0.24 * s);
    group.add(eyeL, eyeR);

    const hornGeo = new THREE.ConeGeometry(0.09 * s, 0.52 * s, 6);
    const hornL = new THREE.Mesh(hornGeo, armorMat);
    hornL.position.set(-0.26 * s, 2.42 * s, 0);
    hornL.rotation.z = 0.38;
    const hornR = new THREE.Mesh(hornGeo, armorMat);
    hornR.position.set(0.26 * s, 2.42 * s, 0);
    hornR.rotation.z = -0.38;
    group.add(hornL, hornR);

    // Articulated Arms & Hell-Blades
    const leftArm = new THREE.Mesh(new THREE.BoxGeometry(0.24 * s, 0.85 * s, 0.24 * s), skinMat);
    leftArm.position.set(-0.58 * s, 1.35 * s, 0.1 * s);
    const rightArm = new THREE.Mesh(new THREE.BoxGeometry(0.24 * s, 0.85 * s, 0.24 * s), skinMat);
    rightArm.position.set(0.58 * s, 1.35 * s, 0.1 * s);
    group.add(leftArm, rightArm);

    const blade = new THREE.Mesh(new THREE.BoxGeometry(0.08 * s, 0.14 * s, 0.65 * s), glowMat);
    blade.position.set(0, -0.35 * s, 0.28 * s);
    rightArm.add(blade);

    // Floating 3D Health Bar above demon
    const hbBg = new THREE.Mesh(
      new THREE.PlaneGeometry(1.1 * s, 0.12 * s),
      new THREE.MeshBasicMaterial({ color: 0x000000, side: THREE.DoubleSide })
    );
    hbBg.position.set(0, 2.78 * s, 0);
    const hbFill = new THREE.Mesh(
      new THREE.PlaneGeometry(1.06 * s, 0.09 * s),
      new THREE.MeshBasicMaterial({ color: arch.isBoss ? 0xb85cff : 0xff3b22, side: THREE.DoubleSide })
    );
    hbFill.position.set(0, 2.78 * s, 0.01);
    group.add(hbBg, hbFill);

    return {
      group,
      torso,
      head,
      leftLeg,
      rightLeg,
      leftArm,
      rightArm,
      hbFill,
      skinMat,
      origColor: arch.skinColor
    };
  }

  function spawnEnemy(typeKey, x, z) {
    const arch = ENEMY_TYPES[typeKey];
    const scaledHp = Math.round(
      arch.hp * difficultyMult * (gameState.mode === 'endless' ? 1 + (gameState.wave - 1) * 0.15 : 1)
    );
    const rig = createDemonMesh3D(arch);
    rig.group.position.set(x, 0, z);
    scene.add(rig.group);

    gameState.enemies.push({
      ...arch,
      x,
      z,
      hp: scaledHp,
      maxHp: scaledHp,
      cooldown: Math.random() * 1.1,
      hitFlash: 0,
      strafeDir: Math.random() < 0.5 ? 1 : -1,
      summonedMinions: false,
      dead: false,
      walkPhase: Math.random() * 6.28,
      rig
    });
  }

  // =========================================================================
  // 9. MISSION INITIALIZATION & STRATUM LOADER
  // =========================================================================
  function clearDynamicEntities() {
    gameState.enemies.forEach(e => scene.remove(e.rig.group));
    gameState.projectiles.forEach(p => scene.remove(p.mesh));
    gameState.pickups.forEach(p => scene.remove(p.mesh));
    gameState.casings.forEach(c => scene.remove(c.mesh));
    gameState.particles.forEach(pt => scene.remove(pt.mesh));
    gameState.enemies = [];
    gameState.projectiles = [];
    gameState.pickups = [];
    gameState.casings = [];
    gameState.particles = [];
  }

  function startNewGame() {
    sound.init();
    gameState.mode = selectedMode;
    gameState.stratumIndex = 0;
    gameState.wave = 1;
    gameState.score = 0;
    gameState.kills = 0;
    gameState.multiplier = 1.0;
    gameState.peakMultiplier = 1.0;
    gameState.comboTimer = 0;
    gameState.elapsedTime = 0;
    gameState.blitzTimer = 150.0;
    gameState.ascensionGauge = 0;
    gameState.overdriveActive = false;
    gameState.overdriveTimer = 0;
    gameState.reloading = false;

    player.hp = 100;
    player.armor = 50;
    player.credits = Math.max(player.credits, 500);
    player.shells = 32;
    player.cells = 120;
    player.mags = WEAPONS.map(w => w.magSize);
    player.weaponIndex = 0;
    selectWeapon(0);

    loadStratum(0);

    document.getElementById('menu-overlay').classList.add('hidden');
    document.getElementById('state-modal').classList.add('hidden');
    gameState.running = true;
    gameState.paused = false;
    sound.startMusic();
    if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
  }

  function loadStratum(index) {
    clearDynamicEntities();
    const stratum = STRATA[index % STRATA.length];
    gameState.stratumIndex = index;
    updateSkyTexture(index);

    player.x = 0;
    player.y = 1.75;
    player.z = 13;
    player.vy = 0;
    player.yaw = 0;
    player.pitch = 0.04;
    gameState.beaconUnlocked = false;
    if (portalRingMesh) portalRingMesh.material.color.setHex(0x7722ff);

    if (gameState.mode === 'endless') {
      spawnEndlessWave(gameState.wave);
    } else {
      stratum.spawns.forEach(s => spawnEnemy(s.type, s.x, s.z));
      spawnSupplyCaches();
    }

    gameState.totalStratumEnemies = gameState.enemies.length;
    addKillfeed(`DEPLOYED TO ${stratum.name}`);
    updateHUD();
  }

  function spawnSupplyCaches() {
    const spots = [
      { kind: 'shells', x: -7, z: 2 },
      { kind: 'hp', x: 7, z: -2 },
      { kind: 'cells', x: -13, z: -16 },
      { kind: 'armor', x: 13, z: -16 },
      { kind: 'hp', x: 0, z: -20 },
      { kind: 'cells', x: -5, z: -26 }
    ];
    const colors = { hp: 0x28f088, armor: 0x24d8ff, shells: 0xff8c24, cells: 0xb85cff };
    spots.forEach(s => {
      const mesh = new THREE.Mesh(
        new THREE.OctahedronGeometry(0.45, 0),
        new THREE.MeshBasicMaterial({ color: colors[s.kind], wireframe: false })
      );
      mesh.position.set(s.x, 0.75, s.z);
      scene.add(mesh);
      gameState.pickups.push({ kind: s.kind, x: s.x, z: s.z, mesh, collected: false });
    });
  }

  function spawnEndlessWave(waveNum) {
    const spots = [
      [-14, -8], [14, -8], [-16, -20], [16, -20],
      [0, -16], [-9, -26], [9, -26], [-18, -14],
      [18, -14], [0, -28], [-6, -12], [6, -12]
    ];
    const count = Math.min(spots.length, 5 + waveNum * 2);
    for (let i = 0; i < count; i++) {
      const [sx, sz] = spots[i % spots.length];
      const pool = waveNum >= 2 ? ['wretch', 'imp', 'gorger', 'cantor'] : ['wretch', 'imp'];
      const pick = (waveNum % 4 === 0 && i === 0) ? 'boss' : pool[i % pool.length];
      spawnEnemy(pick, sx, sz);
    }
    spawnSupplyCaches();
    gameState.totalStratumEnemies = gameState.enemies.length;
    addKillfeed(`ABYSSAL HORDE WAVE ${waveNum} MANIFESTED!`);
  }

  // =========================================================================
  // 10. RELOAD, PHASE-DASH, OVERDRIVE & 3D BALLISTICS
  // =========================================================================
  function startReload() {
    if (gameState.reloading || !gameState.running || gameState.paused) return;
    const w = WEAPONS[player.weaponIndex];
    if (w.isBFG) return;
    if (player.mags[player.weaponIndex] >= w.magSize) return;

    if (w.ammoType === 'shells' && player.shells <= 0) {
      addKillfeed('NO SLAG SHELLS IN RESERVE!');
      return;
    }
    if (w.ammoType === 'cells' && player.cells <= 0) {
      addKillfeed('NO PLASMA CELLS IN RESERVE!');
      return;
    }

    gameState.reloading = true;
    gameState.reloadTimer = 1.05;
    document.getElementById('reload-indicator').classList.remove('hidden');
    sound.playReload(player.weaponIndex);
  }

  // =========================================================================
  // 10B. 3D MELEE ATTACK ([V]) & SPECIAL ABILITIES ([X])
  // =========================================================================
  function triggerMeleeAttack() {
    if (player.meleeCooldown > 0 || !gameState.running || gameState.paused) return;
    const m = MELEE_WEAPONS[player.meleeIndex];
    player.meleeCooldown = m.cooldown;
    gameState.meleeSwingTimer = 0.34;
    if (meleeBladeMat) meleeBladeMat.color.setHex(m.color);
    sound.playMelee(m.id);

    const forwardX = -Math.sin(player.yaw);
    const forwardZ = -Math.cos(player.yaw);
    let hitCount = 0;

    for (const e of gameState.enemies) {
      if (e.dead) continue;
      const dx = e.x - player.x;
      const dz = e.z - player.z;
      const dist = Math.hypot(dx, dz);
      if (dist > m.range) continue;

      if (!m.isShockwave) {
        // Check forward 120-degree cleave cone
        const dot = (dx * forwardX + dz * forwardZ) / (dist || 1);
        if (dot < 0.25) continue;
      }

      hitCount++;
      const dmg = m.damage * (gameState.overdriveActive ? 2.0 : 1.0);
      damageEnemy(e, dmg, 14, false);
      spawnImpactParticles(new THREE.Vector3(e.x, 1.3, e.z), m.color, 10);

      if (m.lifesteal) {
        player.hp = Math.min(player.maxHp, player.hp + m.lifesteal);
      }
    }

    if (m.isShockwave) {
      flashScreen('fx-overdrive', 0.2);
      spawnImpactParticles(new THREE.Vector3(player.x + forwardX * 2, 0.3, player.z + forwardZ * 2), 0xb85cff, 24);
      addKillfeed(`🔨 WARHAMMER SEISMIC SLAM! (${hitCount} DEMONS CRUSHED)`);
    } else if (hitCount > 0) {
      showHitmarker();
      addKillfeed(`🗡 ${m.name} CLEAVED ${hitCount} HOSTILE(S)!`);
    }
    updateHUD();
  }

  function triggerSpecialAbility() {
    if (!gameState.running || gameState.paused) return;
    const abId = player.equippedAbility;
    const charges = player.abilityCharges[abId] || 0;
    if (charges <= 0) {
      addKillfeed(`NO ${abId.toUpperCase()} CHARGES LEFT — BUY MORE IN [B] SHOP!`);
      return;
    }

    player.abilityCharges[abId]--;
    sound.playOverdrive();

    if (abId === 'nova') {
      // Launch 12 radial Hellfire Missiles in 360 degrees
      flashScreen('fx-damage', 0.22);
      for (let i = 0; i < 12; i++) {
        const ang = (i / 12) * Math.PI * 2;
        const vx = Math.sin(ang) * 24;
        const vz = Math.cos(ang) * 24;
        const orbMesh = new THREE.Mesh(
          new THREE.SphereGeometry(0.32, 12, 12),
          new THREE.MeshBasicMaterial({ color: 0xff5511 })
        );
        orbMesh.position.set(player.x + Math.sin(ang) * 1.1, 1.35, player.z + Math.cos(ang) * 1.1);
        scene.add(orbMesh);
        gameState.projectiles.push({
          mesh: orbMesh,
          vx,
          vy: 0,
          vz,
          damage: 260,
          fromPlayer: true,
          isBFG: true
        });
      }
      addKillfeed('🔥 UNLEASHED HELLFIRE ORBITAL NOVA (12 MISSILES)!');
    } else if (abId === 'aegis') {
      gameState.vampiricTimer = 6.0;
      player.invulnTimer = 6.0;
      player.armor = player.maxArmor;
      flashScreen('fx-pickup', 0.45);
      addKillfeed('🛡 VAMPIRIC BLOOD-AEGIS ACTIVE (6s INVULNERABILITY + LIFESTEAL)!');
    } else if (abId === 'meteor') {
      flashScreen('fx-overdrive', 0.35);
      const targets = gameState.enemies.filter(e => !e.dead);
      targets.forEach(e => {
        spawnImpactParticles(new THREE.Vector3(e.x, 4.5, e.z), 0xffaa22, 18);
        spawnImpactParticles(new THREE.Vector3(e.x, 1.2, e.z), 0xff2200, 18);
        damageEnemy(e, 380, 15, true);
      });
      addKillfeed('☄ BRIMSTONE METEOR AIRSTRIKE OBLITERATED THE PLAZA!');
    }
    updateHUD();
  }

  function finishReload() {
    const idx = player.weaponIndex;
    const w = WEAPONS[idx];
    const needed = w.magSize - player.mags[idx];
    if (w.ammoType === 'infinite') {
      player.mags[idx] = w.magSize;
    } else if (w.ammoType === 'shells') {
      const load = Math.min(needed, player.shells);
      player.shells -= load;
      player.mags[idx] += load;
    } else if (w.ammoType === 'cells') {
      const load = Math.min(needed, player.cells);
      player.cells -= load;
      player.mags[idx] += load;
    }
    gameState.reloading = false;
    document.getElementById('reload-indicator').classList.add('hidden');
    updateHUD();
  }

  function triggerDash() {
    if (player.dashCooldown > 0 || gameState.paused) return;
    const forwardX = -Math.sin(player.yaw);
    const forwardZ = -Math.cos(player.yaw);
    const rightX = Math.cos(player.yaw);
    const rightZ = -Math.sin(player.yaw);

    let dx = 0;
    let dz = 0;
    if (keys['KeyW'] || keys['ArrowUp']) { dx += forwardX; dz += forwardZ; }
    if (keys['KeyS'] || keys['ArrowDown']) { dx -= forwardX; dz -= forwardZ; }
    if (keys['KeyD']) { dx += rightX; dz += rightZ; }
    if (keys['KeyA']) { dx -= rightX; dz -= rightZ; }
    if (dx === 0 && dz === 0) { dx = forwardX; dz = forwardZ; }

    const len = Math.hypot(dx, dz) || 1;
    player.dashVx = (dx / len) * 26;
    player.dashVz = (dz / len) * 26;
    player.dashTimer = 0.2;
    player.invulnTimer = 0.28;
    player.dashCooldown = 1.4;
    sound.playDash();
    flashScreen('fx-dash', 0.2);
  }

  function triggerAscensionOverdrive() {
    if (gameState.ascensionGauge < 100 || gameState.overdriveActive) return;
    gameState.ascensionGauge = 0;
    gameState.overdriveActive = true;
    gameState.overdriveTimer = 8.0;
    player.armor = Math.min(player.maxArmor, player.armor + 25);
    sound.playOverdrive();
    document.getElementById('fx-overdrive').style.opacity = '1';
    addKillfeed('⚡ ASCENSION OVERDRIVE ENGAGED (2× DMG // CHRONO-RIFT)!');
    updateHUD();
  }

  const raycaster = new THREE.Raycaster();

  function attemptFire() {
    if (player.fireCooldown > 0 || gameState.reloading || !gameState.running || gameState.paused) return;
    const idx = player.weaponIndex;
    const weapon = WEAPONS[idx];

    if (weapon.isBFG) {
      if (player.cells < weapon.cost) {
        addKillfeed('NEED 25 PLASMA CELLS FOR B.F.G. SINGULARITY!');
        return;
      }
      player.cells -= weapon.cost;
    } else {
      if (player.mags[idx] <= 0) {
        startReload();
        return;
      }
      player.mags[idx]--;
      if (player.mags[idx] === 0) {
        setTimeout(() => startReload(), 160);
      }
    }

    const rofFactor = gameState.overdriveActive ? 0.52 : 1.0;
    player.fireCooldown = weapon.cooldown * rofFactor;
    gameState.muzzleTimer = 0.065;
    gameState.recoilOffset = weapon.recoilKick;
    gameState.recoilPitch = weapon.recoilKick * 0.45;
    player.pitch = Math.min(1.25, player.pitch + weapon.recoilKick * (gameState.isADS ? 0.14 : 0.26));

    sound.playShoot(weapon.id);
    spawnEjectedCasing();

    const dmgMult = gameState.overdriveActive ? 2.0 : 1.0;

    if (weapon.isBFG) {
      // Launch 3D BFG Singularity Orb
      const dir = new THREE.Vector3(0, 0, -1).applyQuaternion(camera.quaternion);
      const orbMesh = new THREE.Mesh(
        new THREE.SphereGeometry(0.42, 16, 16),
        new THREE.MeshBasicMaterial({ color: 0xb85cff })
      );
      orbMesh.position.copy(camera.position).addScaledVector(dir, 1.2);
      scene.add(orbMesh);
      gameState.projectiles.push({
        mesh: orbMesh,
        vx: dir.x * 26,
        vy: dir.y * 26,
        vz: dir.z * 26,
        damage: weapon.damage * dmgMult,
        fromPlayer: true,
        isBFG: true
      });
    } else {
      let hitEnemyAny = false;
      const spreadAmt = gameState.isADS ? weapon.adsSpread : weapon.spread;

      for (let p = 0; p < weapon.pellets; p++) {
        const ndcX = (Math.random() - 0.5) * spreadAmt;
        const ndcY = (Math.random() - 0.5) * spreadAmt;
        raycaster.setFromCamera({ x: ndcX, y: ndcY }, camera);

        // Check enemies vs world geometry
        const worldHits = raycaster.intersectObjects(shootableMeshes, false);
        const maxDist = worldHits.length > 0 ? worldHits[0].distance : 180;

        let closestEnemy = null;
        let closestDist = maxDist;
        let hitPoint = worldHits.length > 0 ? worldHits[0].point : null;

        for (const e of gameState.enemies) {
          if (e.dead) continue;
          const eHits = raycaster.intersectObjects(e.rig.group.children, false);
          if (eHits.length > 0 && eHits[0].distance < closestDist) {
            closestDist = eHits[0].distance;
            closestEnemy = e;
            hitPoint = eHits[0].point;
          }
        }

        if (closestEnemy) {
          hitEnemyAny = true;
          // Headshot bonus check! (Sniper gets 2.5x headshot multiplier!)
          const isHeadshot = hitPoint && hitPoint.y > 1.82 * closestEnemy.scale;
          const hsMult = isHeadshot ? (weapon.isSniper ? 2.5 : 1.5) : 1.0;
          const finalDmg = weapon.damage * dmgMult * hsMult;
          damageEnemy(closestEnemy, finalDmg, weapon.resonanceGain / weapon.pellets, isHeadshot);
          if (gameState.vampiricTimer > 0) {
            player.hp = Math.min(player.maxHp, player.hp + 10);
          }
          if (hitPoint) spawnImpactParticles(hitPoint, 0xff2211, 7);
        } else if (hitPoint && worldHits.length > 0) {
          spawnImpactParticles(hitPoint, 0xffbb44, 5);
          spawnBulletDecal(hitPoint, worldHits[0].face.normal);
        }
      }

      if (hitEnemyAny) showHitmarker();
    }

    updateHUD();
  }

  function spawnEjectedCasing() {
    const casingGeo = new THREE.CylinderGeometry(0.008, 0.008, 0.032, 6);
    const casingMat = new THREE.MeshBasicMaterial({ color: 0xffbb44 });
    const mesh = new THREE.Mesh(casingGeo, casingMat);

    const worldPos = new THREE.Vector3();
    boltMesh.getWorldPosition(worldPos);
    mesh.position.copy(worldPos);
    scene.add(mesh);

    const right = new THREE.Vector3(1, 0.5, 0.1).applyQuaternion(camera.quaternion);
    gameState.casings.push({
      mesh,
      vx: right.x * (2.5 + Math.random()),
      vy: 2.2 + Math.random(),
      vz: right.z * (2.5 + Math.random()),
      life: 0.75
    });
  }

  function spawnImpactParticles(pos, hexColor, count = 6) {
    for (let i = 0; i < count; i++) {
      const m = new THREE.Mesh(
        new THREE.BoxGeometry(0.07, 0.07, 0.07),
        new THREE.MeshBasicMaterial({ color: hexColor })
      );
      m.position.copy(pos);
      scene.add(m);
      gameState.particles.push({
        mesh: m,
        vx: (Math.random() - 0.5) * 6,
        vy: 1.5 + Math.random() * 4.5,
        vz: (Math.random() - 0.5) * 6,
        life: 0.38 + Math.random() * 0.25
      });
    }
  }

  function spawnBulletDecal(point, normal) {
    if (!normal) return;
    const decalMesh = new THREE.Mesh(
      new THREE.CircleGeometry(0.085, 8),
      new THREE.MeshBasicMaterial({ color: 0x120d0a, side: THREE.DoubleSide })
    );
    decalMesh.position.copy(point).addScaledVector(normal, 0.02);
    decalMesh.lookAt(point.clone().add(normal));
    scene.add(decalMesh);
    gameState.decals.push(decalMesh);
    if (gameState.decals.length > 45) {
      const old = gameState.decals.shift();
      scene.remove(old);
    }
  }

  function damageEnemy(enemy, amount, resonanceBonus = 5, isHeadshot = false) {
    if (enemy.dead) return;
    enemy.hp -= amount;
    enemy.hitFlash = 0.14;
    enemy.rig.skinMat.color.setHex(0xffffff);

    if (!gameState.overdriveActive) {
      gameState.ascensionGauge = Math.min(100, gameState.ascensionGauge + resonanceBonus);
    }

    // Update 3D health bar scale
    const ratio = Math.max(0.01, enemy.hp / enemy.maxHp);
    enemy.rig.hbFill.scale.x = ratio;

    // Boss 50% HP phase summon
    if (enemy.isBoss && !enemy.summonedMinions && enemy.hp <= enemy.maxHp * 0.5 && enemy.hp > 0) {
      enemy.summonedMinions = true;
      spawnEnemy('wretch', -10, -22);
      spawnEnemy('imp', 10, -22);
      gameState.totalStratumEnemies += 2;
      addKillfeed('☠ ARCHON MALAKOR ENRAGED — REINFORCEMENTS SUMMONED!');
    }

    if (enemy.hp <= 0) {
      enemy.hp = 0;
      enemy.dead = true;
      spawnImpactParticles(enemy.rig.group.position.clone().add(new THREE.Vector3(0, 1.2, 0)), 0xff3b22, 16);
      scene.remove(enemy.rig.group);

      gameState.kills++;
      gameState.comboTimer = 4.5;
      gameState.multiplier = Math.min(5.0, +(gameState.multiplier + 0.5).toFixed(1));
      if (gameState.multiplier > gameState.peakMultiplier) {
        gameState.peakMultiplier = gameState.multiplier;
      }

      const pts = Math.round(enemy.score * gameState.multiplier * (isHeadshot ? 1.25 : 1));
      gameState.score += pts;

      // Award Soul Credits ($SC) for the Armory Shop!
      const creditBounty = Math.round((enemy.score * 0.38) * (isHeadshot ? 1.35 : 1.0));
      player.credits += creditBounty;

      if (!gameState.overdriveActive) {
        gameState.ascensionGauge = Math.min(100, gameState.ascensionGauge + 14);
      }

      if (gameState.mode === 'blitz') {
        gameState.blitzTimer += 2.5;
        addKillfeed(`${isHeadshot ? 'HEADSHOT! ' : ''}${enemy.name} (+$${creditBounty} // +2.5s)`);
      } else {
        addKillfeed(`${isHeadshot ? 'HEADSHOT! ' : ''}${enemy.name} (+${pts} PTS // +$${creditBounty})`);
      }

      sound.playHit(true);
      checkStratumClear();
    } else {
      sound.playHit(false);
    }
  }

  function checkStratumClear() {
    const alive = gameState.enemies.filter(e => !e.dead).length;
    if (alive === 0) {
      if (gameState.mode === 'endless') {
        gameState.wave++;
        setTimeout(() => {
          if (gameState.running) spawnEndlessWave(gameState.wave);
        }, 1300);
      } else {
        gameState.beaconUnlocked = true;
        if (portalRingMesh) portalRingMesh.material.color.setHex(0xffd15c);
        addKillfeed('★ CITADEL PORTAL UNSEALED! ENTER THE RIFT UNDER THE COLONNADE!');
      }
    }
    updateHUD();
  }

  // =========================================================================
  // 11. 3D SIMULATION LOOP (MOVEMENT, COLLISIONS, AI, SMOKE & SPARKS)
  // =========================================================================
  function collidesWithWorld(nx, nz, radius = 0.65) {
    if (nx < -23.2 || nx > 23.2 || nz < -33.2 || nz > 18.2) return true;
    for (const c of colliders) {
      if (
        nx + radius > c.minX &&
        nx - radius < c.maxX &&
        nz + radius > c.minZ &&
        nz - radius < c.maxZ &&
        player.y < c.maxY + 1.2
      ) {
        return true;
      }
    }
    return false;
  }

  function moveWithSliding(entity, dx, dz, radius = 0.65) {
    if (!collidesWithWorld(entity.x + dx, entity.z, radius)) {
      entity.x += dx;
    }
    if (!collidesWithWorld(entity.x, entity.z + dz, radius)) {
      entity.z += dz;
    }
  }

  function updateSimulation(dt) {
    if (!gameState.running || gameState.paused) return;

    gameState.elapsedTime += dt;
    if (gameState.mode === 'blitz') {
      gameState.blitzTimer -= dt;
      if (gameState.blitzTimer <= 0) {
        gameState.blitzTimer = 0;
        triggerGameOver('CHRONO-COLLAPSE: DOOMSDAY CLOCK EXPIRED');
        return;
      }
    }

    if (player.fireCooldown > 0) player.fireCooldown = Math.max(0, player.fireCooldown - dt);
    if (player.meleeCooldown > 0) player.meleeCooldown = Math.max(0, player.meleeCooldown - dt);
    if (player.dashCooldown > 0) player.dashCooldown = Math.max(0, player.dashCooldown - dt);
    if (player.invulnTimer > 0) player.invulnTimer = Math.max(0, player.invulnTimer - dt);
    if (gameState.vampiricTimer > 0) gameState.vampiricTimer = Math.max(0, gameState.vampiricTimer - dt);
    if (gameState.meleeSwingTimer > 0) gameState.meleeSwingTimer = Math.max(0, gameState.meleeSwingTimer - dt);

    // Reload timer & magazine animation
    if (gameState.reloading) {
      gameState.reloadTimer -= dt;
      if (gameState.reloadTimer <= 0) {
        finishReload();
      }
    }

    // Muzzle flash & recoil recovery
    if (gameState.muzzleTimer > 0) {
      gameState.muzzleTimer -= dt;
      muzzleMesh.visible = true;
      muzzleMesh.rotation.z = Math.random() * Math.PI;
      muzzleLight.intensity = 2.8;
    } else {
      muzzleMesh.visible = false;
      muzzleLight.intensity = 0;
    }
    gameState.recoilOffset = THREE.MathUtils.lerp(gameState.recoilOffset, 0, dt * 14);
    gameState.recoilPitch = THREE.MathUtils.lerp(gameState.recoilPitch, 0, dt * 14);

    // Automatic firing
    if ((mouseFiring || keys['KeyF']) && !gameState.reloading) {
      if (WEAPONS[player.weaponIndex].auto || player.fireCooldown === 0) {
        attemptFire();
      }
    }

    // Overdrive timer
    if (gameState.overdriveActive) {
      gameState.overdriveTimer -= dt;
      if (gameState.overdriveTimer <= 0) {
        gameState.overdriveActive = false;
        document.getElementById('fx-overdrive').style.opacity = '0';
        addKillfeed('ASCENSION OVERDRIVE DEPLETED');
      }
    }

    // Combo multiplier decay
    if (gameState.comboTimer > 0) {
      gameState.comboTimer -= dt;
      if (gameState.comboTimer <= 0) {
        gameState.multiplier = 1.0;
      }
    }

    // Keyboard rotation fallback
    if (keys['ArrowLeft']) player.yaw += 2.2 * dt;
    if (keys['ArrowRight']) player.yaw -= 2.2 * dt;

    // Player horizontal movement
    if (player.dashTimer > 0) {
      player.dashTimer -= dt;
      moveWithSliding(player, player.dashVx * dt, player.dashVz * dt, 0.65);
    } else {
      const baseSpeed = (gameState.overdriveActive ? 13.5 : 10.2) * (gameState.isADS ? 0.68 : 1.0);
      const forwardX = -Math.sin(player.yaw);
      const forwardZ = -Math.cos(player.yaw);
      const rightX = Math.cos(player.yaw);
      const rightZ = -Math.sin(player.yaw);

      let moveX = 0;
      let moveZ = 0;
      if (keys['KeyW'] || keys['ArrowUp']) { moveX += forwardX; moveZ += forwardZ; }
      if (keys['KeyS'] || keys['ArrowDown']) { moveX -= forwardX; moveZ -= forwardZ; }
      if (keys['KeyD']) { moveX += rightX; moveZ += rightZ; }
      if (keys['KeyA']) { moveX -= rightX; moveZ -= rightZ; }

      const len = Math.hypot(moveX, moveZ);
      if (len > 0) {
        moveX = (moveX / len) * baseSpeed * dt;
        moveZ = (moveZ / len) * baseSpeed * dt;
        moveWithSliding(player, moveX, moveZ, 0.65);
        gameState.bobPhase += dt * (gameState.isADS ? 6.5 : 11.5);
      }
    }

    // Player vertical jump & gravity
    player.y += player.vy * dt;
    player.vy -= 19.5 * dt;
    if (player.y <= 1.75) {
      player.y = 1.75;
      player.vy = 0;
      player.onGround = true;
    }

    // Update Camera Transform & Smooth ADS FOV
    camera.position.set(player.x, player.y + Math.sin(gameState.bobPhase) * (gameState.isADS ? 0.015 : 0.045), player.z);
    camera.rotation.set(player.pitch, player.yaw, 0, 'YXZ');

    const isSniperEquipped = WEAPONS[player.weaponIndex] && WEAPONS[player.weaponIndex].isSniper;
    const targetFov = gameState.isADS ? (isSniperEquipped ? 26 : 46) : 70;
    if (Math.abs(camera.fov - targetFov) > 0.1) {
      camera.fov = THREE.MathUtils.lerp(camera.fov, targetFov, dt * 14);
      camera.updateProjectionMatrix();
    }

    // Update 3D Weapon Viewmodel (Hip-fire <-> ADS interpolation + 4-Stage Reload Animation + Recoil)
    const targetPos = gameState.isADS ? ADS_POS : HIP_POS;
    const targetRot = gameState.isADS ? ADS_ROT : HIP_ROT;
    weaponRig.position.lerp(targetPos, dt * 15);

    let reloadRoll = 0;
    let reloadPitchAdd = 0;
    let reloadDropY = 0;
    let boltRackZ = 0;

    if (gameState.reloading) {
      const prog = 1 - gameState.reloadTimer / 1.05; // 0.0 -> 1.0
      if (prog < 0.35) {
        // Stage 1: Tilt rifle & drop empty magazine out
        reloadRoll = 0.42;
        reloadPitchAdd = 0.18;
      } else if (prog < 0.68) {
        // Stage 2: Slam fresh magazine into magwell (upward jolt at 0.45)
        reloadRoll = 0.38;
        reloadPitchAdd = prog > 0.42 && prog < 0.55 ? -0.12 : 0.12;
        reloadDropY = prog > 0.42 && prog < 0.52 ? 0.035 : -0.02;
      } else if (prog < 0.9) {
        // Stage 3: Pull charging handle / bolt carrier sharply back
        reloadRoll = -0.22;
        reloadPitchAdd = 0.08;
        boltRackZ = 0.085;
      } else {
        // Stage 4: Bolt slams forward
        reloadRoll = 0;
        boltRackZ = 0;
      }
    }

    weaponRig.rotation.x = THREE.MathUtils.lerp(
      weaponRig.rotation.x,
      targetRot.x + gameState.recoilPitch + reloadPitchAdd,
      dt * 15
    );
    weaponRig.rotation.y = THREE.MathUtils.lerp(weaponRig.rotation.y, targetRot.y, dt * 15);
    weaponRig.rotation.z = THREE.MathUtils.lerp(weaponRig.rotation.z, targetRot.z + reloadRoll, dt * 14);

    const swayAmp = gameState.isADS ? 0.002 : 0.011;
    gunGroup.position.set(
      Math.cos(gameState.bobPhase * 0.5) * swayAmp,
      Math.abs(Math.sin(gameState.bobPhase)) * swayAmp + reloadDropY,
      gameState.recoilOffset
    );
    if (boltMesh) {
      boltMesh.position.z = 0.02 + gameState.recoilOffset * 1.4 + boltRackZ;
    }
    if (magMesh) {
      if (gameState.reloading) {
        const prog = 1 - gameState.reloadTimer / 1.05;
        if (prog < 0.36) {
          magMesh.position.y = -0.11 - (prog / 0.36) * 0.24;
        } else if (prog < 0.48) {
          magMesh.position.y = -0.35 + ((prog - 0.36) / 0.12) * 0.24;
        } else {
          magMesh.position.y = -0.11;
        }
      } else {
        magMesh.position.y = -0.11;
      }
    }

    // 3D Melee Blade Slash Animation ([V])
    if (meleeViewGroup) {
      if (gameState.meleeSwingTimer > 0) {
        meleeViewGroup.visible = true;
        const p = 1 - gameState.meleeSwingTimer / 0.34;
        meleeViewGroup.position.set(-0.35 + p * 0.65, -0.08, -0.44);
        meleeViewGroup.rotation.set(-0.6, 0.3, -1.2 + p * 2.4);
      } else {
        meleeViewGroup.visible = false;
      }
    }

    // Rotate Citadel Portal & Check if Player Entered
    if (portalGroup) {
      portalRingMesh.rotation.z += dt * (gameState.beaconUnlocked ? 2.5 : 0.6);
      if (gameState.beaconUnlocked && Math.hypot(player.x - 0, player.z - -30.5) < 3.4) {
        handleBeaconEntered();
        return;
      }
    }

    // Pickups rotation & collection
    for (const p of gameState.pickups) {
      if (p.collected) continue;
      p.mesh.rotation.y += dt * 2.4;
      if (Math.hypot(player.x - p.x, player.z - p.z) < 1.65) {
        collectPickup(p);
      }
    }

    // Update 3D Articulated Enemies
    const enemyTimeScale = gameState.overdriveActive ? 0.55 : 1.0;
    const edt = dt * enemyTimeScale;

    for (const e of gameState.enemies) {
      if (e.dead) continue;
      if (e.hitFlash > 0) {
        e.hitFlash -= dt;
        if (e.hitFlash <= 0) e.rig.skinMat.color.setHex(e.origColor);
      }
      if (e.cooldown > 0) e.cooldown -= edt;

      const dx = player.x - e.x;
      const dz = player.z - e.z;
      const dist = Math.hypot(dx, dz) || 1;
      const dirX = dx / dist;
      const dirZ = dz / dist;

      // Face player
      e.rig.group.rotation.y = Math.atan2(dx, dz);

      // Animate legs & arms stride
      e.walkPhase += edt * 8.5;
      const swing = Math.sin(e.walkPhase) * 0.45;
      e.rig.leftLeg.rotation.x = swing;
      e.rig.rightLeg.rotation.x = -swing;
      e.rig.leftArm.rotation.x = -swing * 0.7;
      e.rig.rightArm.rotation.x = swing * 0.7;

      if (!e.ranged) {
        if (dist > 1.8) {
          moveWithSliding(e, dirX * e.speed * edt, dirZ * e.speed * edt, 0.7);
        }
        if (dist <= e.attackRange && e.cooldown <= 0) {
          e.cooldown = e.attackCooldown;
          applyDamageToPlayer(e.damage, e.name);
        }
      } else {
        if (dist > 14) {
          moveWithSliding(e, dirX * e.speed * edt, dirZ * e.speed * edt, 0.75);
        } else {
          const perpX = -dirZ * e.strafeDir;
          const perpZ = dirX * e.strafeDir;
          moveWithSliding(e, perpX * e.speed * 0.75 * edt, perpZ * e.speed * 0.75 * edt, 0.75);
          if (Math.random() < 0.02) e.strafeDir *= -1;
        }

        if (dist <= e.attackRange && e.cooldown <= 0) {
          e.cooldown = e.attackCooldown;
          const baseAng = Math.atan2(dx, dz);
          if (e.isBoss) {
            [-0.18, 0, 0.18].forEach(off => spawnEnemyProjectile3D(e, baseAng + off));
          } else {
            spawnEnemyProjectile3D(e, baseAng);
          }
          if (e.canBlink && Math.random() < 0.5) {
            moveWithSliding(e, -dirZ * e.strafeDir * 3.2, dirX * e.strafeDir * 3.2, 0.75);
          }
        }
      }

      e.rig.group.position.set(e.x, 0, e.z);
    }

    // Update 3D Projectiles
    for (let i = gameState.projectiles.length - 1; i >= 0; i--) {
      const pr = gameState.projectiles[i];
      const pScale = pr.fromPlayer ? 1.0 : enemyTimeScale;
      pr.mesh.position.x += pr.vx * dt * pScale;
      pr.mesh.position.y += pr.vy * dt * pScale;
      pr.mesh.position.z += pr.vz * dt * pScale;

      if (collidesWithWorld(pr.mesh.position.x, pr.mesh.position.z, 0.25) || pr.mesh.position.y <= 0.1) {
        if (pr.isBFG) detonateBFG(pr.mesh.position, pr.damage);
        scene.remove(pr.mesh);
        gameState.projectiles.splice(i, 1);
        continue;
      }

      if (pr.fromPlayer) {
        let hit = false;
        for (const e of gameState.enemies) {
          if (e.dead) continue;
          if (Math.hypot(e.x - pr.mesh.position.x, e.z - pr.mesh.position.z) < 1.35 * e.scale) {
            if (pr.isBFG) detonateBFG(pr.mesh.position, pr.damage);
            else damageEnemy(e, pr.damage, 8, false);
            hit = true;
            break;
          }
        }
        if (hit) {
          scene.remove(pr.mesh);
          gameState.projectiles.splice(i, 1);
        }
      } else {
        if (Math.hypot(player.x - pr.mesh.position.x, player.z - pr.mesh.position.z) < 0.95) {
          applyDamageToPlayer(pr.damage, pr.sourceName);
          scene.remove(pr.mesh);
          gameState.projectiles.splice(i, 1);
        }
      }
    }

    // Update Ejected Shell Casings
    for (let i = gameState.casings.length - 1; i >= 0; i--) {
      const c = gameState.casings[i];
      c.life -= dt;
      c.mesh.position.x += c.vx * dt;
      c.mesh.position.y += c.vy * dt;
      c.mesh.position.z += c.vz * dt;
      c.vy -= 14 * dt;
      c.mesh.rotation.x += 12 * dt;
      c.mesh.rotation.z += 9 * dt;
      if (c.life <= 0 || c.mesh.position.y < 0.05) {
        scene.remove(c.mesh);
        gameState.casings.splice(i, 1);
      }
    }

    // Update Impact Particles
    for (let i = gameState.particles.length - 1; i >= 0; i--) {
      const pt = gameState.particles[i];
      pt.life -= dt;
      pt.mesh.position.x += pt.vx * dt;
      pt.mesh.position.y += pt.vy * dt;
      pt.mesh.position.z += pt.vz * dt;
      pt.vy -= 12 * dt;
      if (pt.life <= 0) {
        scene.remove(pt.mesh);
        gameState.particles.splice(i, 1);
      }
    }

    updateHUD();
  }

  function updateAtmosphereFX(dt) {
    // Rising volumetric smoke plumes above burning hulls
    for (const sm of smokePlumes) {
      sm.sprite.position.y += sm.speed * dt;
      sm.sprite.position.x = sm.baseX + Math.sin(performance.now() * 0.0015 + sm.phase) * 0.8;
      if (sm.sprite.position.y > sm.baseY + 8.5) {
        sm.sprite.position.y = sm.baseY;
      }
    }

    // Sparking broken streetlamp in courtyard (like the screenshot!)
    for (const sp of sparkEmitters) {
      sp.light.intensity = 1.2 + Math.random() * 1.1;
      if (Math.random() < 0.25 && gameState.particles.length < 60) {
        spawnImpactParticles(sp.pos, 0xffdd55, 1);
      }
    }
  }

  function spawnEnemyProjectile3D(enemy, angle) {
    const startY = 1.45 * enemy.scale;
    const dx = player.x - enemy.x;
    const dy = (player.y - 0.2) - startY;
    const dz = player.z - enemy.z;
    const dist = Math.hypot(dx, dy, dz) || 1;

    const mesh = new THREE.Mesh(
      new THREE.SphereGeometry(enemy.isBoss ? 0.34 : 0.22, 10, 10),
      new THREE.MeshBasicMaterial({ color: enemy.projColor })
    );
    mesh.position.set(enemy.x + Math.sin(angle) * 0.8, startY, enemy.z + Math.cos(angle) * 0.8);
    scene.add(mesh);

    gameState.projectiles.push({
      mesh,
      vx: Math.sin(angle) * enemy.projSpeed,
      vy: (dy / dist) * enemy.projSpeed,
      vz: Math.cos(angle) * enemy.projSpeed,
      damage: Math.round(enemy.damage * difficultyMult),
      fromPlayer: false,
      sourceName: enemy.name
    });
  }

  function detonateBFG(pos, baseDmg) {
    sound.playOverdrive();
    flashScreen('fx-overdrive', 0.28);
    spawnImpactParticles(pos, 0xb85cff, 28);
    for (const e of gameState.enemies) {
      if (e.dead) continue;
      const d = Math.hypot(e.x - pos.x, e.z - pos.z);
      if (d < 16) {
        const falloff = Math.max(0.35, 1 - d / 16);
        damageEnemy(e, Math.round(baseDmg * falloff), 12, false);
      }
    }
  }

  function applyDamageToPlayer(rawDmg, attackerName) {
    if (player.invulnTimer > 0) return;
    let hpLoss = rawDmg;
    if (player.armor > 0) {
      const absorbed = Math.min(player.armor, Math.ceil(rawDmg * 0.65));
      player.armor -= absorbed;
      hpLoss = Math.max(1, rawDmg - absorbed);
    }
    player.hp = Math.max(0, player.hp - hpLoss);
    sound.playPlayerDamage();
    flashScreen('fx-damage', 0.28);

    if (player.hp <= 0) {
      triggerGameOver(`VESSEL SHATTERED BY ${attackerName}`);
    }
  }

  function collectPickup(p) {
    p.collected = true;
    scene.remove(p.mesh);
    sound.playPickup();
    flashScreen('fx-pickup', 0.18);

    if (p.kind === 'hp') {
      player.hp = Math.min(player.maxHp, player.hp + 30);
      addKillfeed('HARVESTED VITALITY ICHOR (+30 HP)');
    } else if (p.kind === 'armor') {
      player.armor = Math.min(player.maxArmor, player.armor + 35);
      addKillfeed('EQUIPPED AEGIS SHARD (+35 ARMOR)');
    } else if (p.kind === 'shells') {
      player.shells = Math.min(player.maxShells, player.shells + 16);
      addKillfeed('COLLECTED SLAG SHELLS (+16)');
    } else if (p.kind === 'cells') {
      player.cells = Math.min(player.maxCells, player.cells + 45);
      addKillfeed('COLLECTED PLASMA CELLS (+45)');
    }
  }

  // =========================================================================
  // 12. 2D TACTICAL RADAR & HUD TELEMETRY
  // =========================================================================
  function renderMinimap() {
    const mw = miniCanvas.width;
    const mh = miniCanvas.height;
    miniCtx.fillStyle = '#07050a';
    miniCtx.fillRect(0, 0, mw, mh);

    const mapWorldToRadar = (wx, wz) => ({
      rx: ((wx + 28) / 56) * mw,
      ry: ((wz + 38) / 58) * mh
    });

    // Citadel & Wreckage Colliders
    miniCtx.fillStyle = '#2d2438';
    for (const c of colliders) {
      const p1 = mapWorldToRadar(c.minX, c.minZ);
      const p2 = mapWorldToRadar(c.maxX, c.maxZ);
      miniCtx.fillRect(p1.rx, p1.ry, Math.max(2, p2.rx - p1.rx), Math.max(2, p2.ry - p1.ry));
    }

    // Citadel Ascension Portal
    const portPos = mapWorldToRadar(0, -31.2);
    miniCtx.fillStyle = gameState.beaconUnlocked ? '#ffd15c' : '#8c3bff';
    miniCtx.beginPath();
    miniCtx.arc(portPos.rx, portPos.ry, 5, 0, Math.PI * 2);
    miniCtx.fill();

    // Pickups
    for (const p of gameState.pickups) {
      if (p.collected) continue;
      const rp = mapWorldToRadar(p.x, p.z);
      miniCtx.fillStyle = '#24d8ff';
      miniCtx.fillRect(rp.rx - 2, rp.ry - 2, 4, 4);
    }

    // Enemies
    for (const e of gameState.enemies) {
      if (e.dead) continue;
      const ep = mapWorldToRadar(e.x, e.z);
      miniCtx.fillStyle = e.isBoss ? '#ffd15c' : '#ff3b22';
      miniCtx.beginPath();
      miniCtx.arc(ep.rx, ep.ry, e.isBoss ? 4.5 : 3, 0, Math.PI * 2);
      miniCtx.fill();
    }

    // Player & Look Direction
    const pp = mapWorldToRadar(player.x, player.z);
    miniCtx.strokeStyle = 'rgba(40, 240, 136, 0.6)';
    miniCtx.beginPath();
    miniCtx.moveTo(pp.rx, pp.ry);
    miniCtx.lineTo(pp.rx - Math.sin(player.yaw) * 18, pp.ry - Math.cos(player.yaw) * 18);
    miniCtx.stroke();

    miniCtx.fillStyle = '#28f088';
    miniCtx.beginPath();
    miniCtx.arc(pp.rx, pp.ry, 3.5, 0, Math.PI * 2);
    miniCtx.fill();
  }

  function updateHUD() {
    const stratum = STRATA[gameState.stratumIndex % STRATA.length];
    document.getElementById('hud-mode-badge').textContent = gameState.mode.toUpperCase();
    document.getElementById('hud-stratum-name').textContent =
      gameState.mode === 'endless' ? `CITADEL HORDE // WAVE ${gameState.wave}` : stratum.name;

    const alive = gameState.enemies.filter(e => !e.dead).length;
    const total = Math.max(1, gameState.totalStratumEnemies);
    document.getElementById('hud-enemies-count').textContent = `${alive} / ${total}`;
    document.getElementById('hud-quota-bar').style.width = `${(alive / total) * 100}%`;
    document.getElementById('hud-objective-text').textContent =
      gameState.beaconUnlocked ? 'ENTER CITADEL PORTAL AT COLONNADE' : 'PURGE DEMONIC GARRISON';

    document.getElementById('interaction-prompt').classList.toggle('hidden', !gameState.beaconUnlocked);

    if (gameState.mode === 'blitz') {
      document.getElementById('hud-clock-label').textContent = 'DOOMSDAY CLOCK';
      document.getElementById('hud-timer').textContent = formatTime(gameState.blitzTimer);
    } else {
      document.getElementById('hud-clock-label').textContent = 'ASCENSION TIME';
      document.getElementById('hud-timer').textContent = formatTime(gameState.elapsedTime);
    }

    const boss = gameState.enemies.find(e => e.isBoss && !e.dead);
    const bossContainer = document.getElementById('boss-bar-container');
    if (boss) {
      bossContainer.classList.remove('hidden');
      document.getElementById('boss-hp-text').textContent = `${Math.ceil(boss.hp)} / ${boss.maxHp}`;
      document.getElementById('boss-hp-bar').style.width = `${(boss.hp / boss.maxHp) * 100}%`;
    } else {
      bossContainer.classList.add('hidden');
    }

    document.getElementById('hud-score').textContent = String(gameState.score).padStart(6, '0');
    const credEl = document.getElementById('hud-credits');
    if (credEl) credEl.textContent = `🩸 $${player.credits}`;
    document.getElementById('hud-multiplier').textContent = `${gameState.multiplier.toFixed(1)}×`;
    document.getElementById('hud-mult-bar').style.width = `${(gameState.comboTimer / 4.5) * 100}%`;

    const meleeEl = document.getElementById('hud-melee-equipped');
    if (meleeEl) {
      meleeEl.textContent = `[V] MELEE: ${MELEE_WEAPONS[player.meleeIndex].name}`;
    }
    const abEl = document.getElementById('hud-ability-equipped');
    if (abEl) {
      const abObj = SPECIAL_ABILITIES.find(a => a.id === player.equippedAbility);
      const chg = player.abilityCharges[player.equippedAbility] || 0;
      abEl.textContent = `[X] ${abObj ? abObj.name.split(' ')[1] : 'SPECIAL'} (${chg})`;
    }

    document.getElementById('hud-hp').textContent = player.hp;
    document.getElementById('hud-hp-bar').style.width = `${player.hp}%`;
    document.getElementById('hud-armor').textContent = player.armor;
    document.getElementById('hud-armor-bar').style.width = `${player.armor}%`;

    const dashPct = player.dashCooldown <= 0 ? 100 : Math.round((1 - player.dashCooldown / 1.4) * 100);
    document.getElementById('hud-dash-bar').style.width = `${dashPct}%`;
    document.getElementById('hud-dash-state').textContent = player.dashCooldown <= 0 ? 'READY' : 'RECHARGING';

    const coreEl = document.querySelector('.hud-ascension-core');
    if (gameState.overdriveActive) {
      const pct = Math.round((gameState.overdriveTimer / 8.0) * 100);
      document.getElementById('hud-ascension-pct').textContent = 'OVERDRIVE!';
      document.getElementById('hud-ascension-bar').style.width = `${pct}%`;
      document.getElementById('hud-ascension-status').textContent = 'CHRONO-OVERDRIVE ACTIVE — 2× DAMAGE';
      coreEl.classList.add('overdrive-ready');
    } else {
      const pct = Math.round(gameState.ascensionGauge);
      document.getElementById('hud-ascension-pct').textContent = `${pct}%`;
      document.getElementById('hud-ascension-bar').style.width = `${pct}%`;
      if (pct >= 100) {
        document.getElementById('hud-ascension-status').textContent = 'PRESS [Q] OR [E] TO UNLEASH OVERDRIVE!';
        coreEl.classList.add('overdrive-ready');
      } else {
        document.getElementById('hud-ascension-status').textContent = 'HARVEST FALLEN FIENDS TO CHARGE OVERDRIVE';
        coreEl.classList.remove('overdrive-ready');
      }
    }

    const idx = player.weaponIndex;
    const w = WEAPONS[idx];
    document.getElementById('hud-weapon-name').textContent = w.name;
    document.getElementById('hud-ammo-type').textContent = w.ammoLabel;
    if (w.isBFG) {
      document.getElementById('hud-ammo-current').textContent = `${player.cells}`;
    } else if (w.ammoType === 'infinite') {
      document.getElementById('hud-ammo-current').textContent = `${player.mags[idx]} / ∞`;
    } else if (w.ammoType === 'shells') {
      document.getElementById('hud-ammo-current').textContent = `${player.mags[idx]} / ${player.shells}`;
    } else {
      document.getElementById('hud-ammo-current').textContent = `${player.mags[idx]} / ${player.cells}`;
    }

    document.getElementById('slot-ammo-0').textContent = `${player.mags[0]}/∞`;
    document.getElementById('slot-ammo-1').textContent = `${player.mags[1]}/${player.shells}`;
    document.getElementById('slot-ammo-2').textContent = `${player.mags[2]}/${player.cells}`;
    document.getElementById('slot-ammo-3').textContent = `${player.cells}`;
    const slot4El = document.getElementById('slot-ammo-4');
    if (slot4El) {
      slot4El.textContent = WEAPONS[4].owned ? `${player.mags[4]}/${player.shells}` : 'LOCKED';
    }

    document.querySelectorAll('.w-slot').forEach((btn, sIdx) => {
      btn.classList.toggle('active', sIdx === idx);
    });
  }

  // =========================================================================
  // 12B. BLACK-MARKET ARMORY & LOADOUT SHOP CONTROLLER ([B] KEY)
  // =========================================================================
  function openShopModal() {
    gameState.shopOpen = true;
    gameState.isADS = false;
    if (gameState.running) gameState.paused = true;
    if (document.exitPointerLock) document.exitPointerLock();
    document.getElementById('shop-modal').classList.remove('hidden');
    renderShopItems(gameState.activeShopCat);
  }

  function closeShopModal() {
    gameState.shopOpen = false;
    document.getElementById('shop-modal').classList.add('hidden');
    if (gameState.running && document.getElementById('state-modal').classList.contains('hidden')) {
      gameState.paused = false;
      if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
    }
    updateHUD();
  }

  function toggleShopModal() {
    if (gameState.shopOpen) closeShopModal();
    else openShopModal();
  }

  document.getElementById('tab-btn-shop').addEventListener('click', () => {
    switchMainView('view-game');
    openShopModal();
  });
  document.getElementById('btn-open-shop-from-menu').addEventListener('click', openShopModal);
  document.getElementById('hud-btn-open-shop').addEventListener('click', openShopModal);
  document.getElementById('btn-close-shop').addEventListener('click', closeShopModal);

  document.querySelectorAll('.shop-cat-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.shop-cat-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      gameState.activeShopCat = btn.dataset.cat;
      renderShopItems(btn.dataset.cat);
    });
  });

  function renderShopItems(category) {
    const walletEl = document.getElementById('shop-credits-amount');
    if (walletEl) walletEl.textContent = `🩸 $${player.credits}`;
    const grid = document.getElementById('shop-items-grid');
    if (!grid) return;
    grid.innerHTML = '';

    if (category === 'guns') {
      WEAPONS.forEach((w, idx) => {
        const isEquipped = player.weaponIndex === idx && w.owned;
        const card = document.createElement('div');
        card.className = `shop-item-card ${isEquipped ? 'equipped-card' : ''}`;

        const nextCost = w.owned ? Math.round(w.upgradeCost * w.level) : w.buyCost;
        const canAfford = player.credits >= nextCost;
        const maxed = w.level >= 5;

        card.innerHTML = `
          <div>
            <div class="shop-card-top">
              <span class="shop-tier-tag">SLOT [${idx + 1}] // FIREARM</span>
              <span class="shop-level-badge">${w.owned ? `MK-${w.level}` : 'LOCKED'}</span>
            </div>
            <h4>${w.name}</h4>
            <p>${w.desc}</p>
            <div class="shop-stat-pills">
              <span>DMG: ${w.damage}${w.pellets > 1 ? ` × ${w.pellets}` : ''}</span>
              <span>MAG: ${w.magSize}</span>
              <span>${w.owned ? `UPGRADE: +22% DMG` : 'NEW WEAPON'}</span>
            </div>
          </div>
          <div class="shop-card-actions">
            ${
              !w.owned
                ? `<button class="btn-shop-buy" data-action="buy-gun" data-idx="${idx}" ${!canAfford ? 'disabled' : ''}>
                     UNLOCK GUN ($${nextCost})
                   </button>`
                : `<button class="btn-shop-buy" data-action="upgrade-gun" data-idx="${idx}" ${!canAfford || maxed ? 'disabled' : ''}>
                     ${maxed ? 'MAX LEVEL (MK-5)' : `UPGRADE MK-${w.level + 1} ($${nextCost})`}
                   </button>
                   <button class="btn-shop-equip" data-action="equip-gun" data-idx="${idx}">
                     ${isEquipped ? '✓ EQUIPPED' : 'EQUIP'}
                   </button>`
            }
          </div>
        `;
        grid.appendChild(card);
      });
    } else if (category === 'melee') {
      MELEE_WEAPONS.forEach((m, idx) => {
        const isEquipped = player.meleeIndex === idx;
        const canAfford = player.credits >= m.cost;
        const card = document.createElement('div');
        card.className = `shop-item-card ${isEquipped ? 'equipped-card' : ''}`;

        card.innerHTML = `
          <div>
            <div class="shop-card-top">
              <span class="shop-tier-tag">QUICK MELEE [V]</span>
              <span class="shop-level-badge">${m.owned ? 'OWNED' : `$${m.cost}`}</span>
            </div>
            <h4>${m.name}</h4>
            <p>${m.desc}</p>
            <div class="shop-stat-pills">
              <span>DMG: ${m.damage}</span>
              <span>REACH: ${m.range}m</span>
              <span>${m.lifesteal ? `+${m.lifesteal} HP LEECH` : m.isShockwave ? '360° SHOCKWAVE' : 'FAST SLASH'}</span>
            </div>
          </div>
          <div class="shop-card-actions">
            ${
              !m.owned
                ? `<button class="btn-shop-buy" data-action="buy-melee" data-idx="${idx}" ${!canAfford ? 'disabled' : ''}>
                     BUY MELEE ($${m.cost})
                   </button>`
                : `<button class="btn-shop-equip" data-action="equip-melee" data-idx="${idx}">
                     ${isEquipped ? '✓ EQUIPPED ON [V]' : 'EQUIP ON [V]'}
                   </button>`
            }
          </div>
        `;
        grid.appendChild(card);
      });
    } else if (category === 'abilities') {
      SPECIAL_ABILITIES.forEach(ab => {
        const isEquipped = !ab.isInstant && player.equippedAbility === ab.id;
        const canAfford = player.credits >= ab.cost;
        const charges = player.abilityCharges[ab.id] || 0;
        const card = document.createElement('div');
        card.className = `shop-item-card ${isEquipped ? 'equipped-card' : ''}`;

        card.innerHTML = `
          <div>
            <div class="shop-card-top">
              <span class="shop-tier-tag">${ab.tag}</span>
              <span class="shop-level-badge">${ab.isInstant ? 'INSTANT' : `CHARGES: ${charges}`}</span>
            </div>
            <h4>${ab.name}</h4>
            <p>${ab.desc}</p>
            <div class="shop-stat-pills">
              <span>COST: $${ab.cost}</span>
              <span>${ab.isInstant ? 'FULL HP/ARMOR/AMMO' : `+${ab.chargesPerBuy} CHARGES / BUY`}</span>
            </div>
          </div>
          <div class="shop-card-actions">
            <button class="btn-shop-buy" data-action="buy-ability" data-id="${ab.id}" ${!canAfford ? 'disabled' : ''}>
              ${ab.isInstant ? `BUY RESUPPLY ($${ab.cost})` : `BUY +${ab.chargesPerBuy} CHARGES ($${ab.cost})`}
            </button>
            ${
              !ab.isInstant
                ? `<button class="btn-shop-equip" data-action="equip-ability" data-id="${ab.id}">
                     ${isEquipped ? '✓ ACTIVE ON [X]' : 'EQUIP ON [X]'}
                   </button>`
                : ''
            }
          </div>
        `;
        grid.appendChild(card);
      });
    }

    // Bind shop card buttons
    grid.querySelectorAll('button[data-action]').forEach(btn => {
      btn.addEventListener('click', () => {
        const act = btn.dataset.action;
        if (act === 'buy-gun') {
          const idx = parseInt(btn.dataset.idx, 10);
          const w = WEAPONS[idx];
          if (player.credits >= w.buyCost && !w.owned) {
            player.credits -= w.buyCost;
            w.owned = true;
            player.shells = Math.min(player.maxShells, player.shells + 20);
            player.mags[idx] = w.magSize;
            selectWeapon(idx);
            sound.playPickup();
            addKillfeed(`🛒 UNLOCKED ${w.name}!`);
          }
        } else if (act === 'upgrade-gun') {
          const idx = parseInt(btn.dataset.idx, 10);
          const w = WEAPONS[idx];
          const cost = Math.round(w.upgradeCost * w.level);
          if (player.credits >= cost && w.level < 5) {
            player.credits -= cost;
            w.level++;
            w.damage = Math.round(w.baseDamage * (1 + (w.level - 1) * 0.22));
            if (!w.isBFG) w.magSize += 4;
            player.mags[idx] = w.magSize;
            player.shells = Math.min(player.maxShells, player.shells + 16);
            player.cells = Math.min(player.maxCells, player.cells + 50);
            sound.playReload(idx);
            addKillfeed(`🔧 UPGRADED ${w.name} TO MK-${w.level} (${w.damage} DMG)!`);
          }
        } else if (act === 'equip-gun') {
          selectWeapon(parseInt(btn.dataset.idx, 10));
          sound.playPickup();
        } else if (act === 'buy-melee') {
          const idx = parseInt(btn.dataset.idx, 10);
          const m = MELEE_WEAPONS[idx];
          if (player.credits >= m.cost && !m.owned) {
            player.credits -= m.cost;
            m.owned = true;
            player.meleeIndex = idx;
            sound.playMelee(idx);
            addKillfeed(`🗡 PURCHASED & EQUIPPED ${m.name} ON [V]!`);
          }
        } else if (act === 'equip-melee') {
          player.meleeIndex = parseInt(btn.dataset.idx, 10);
          sound.playMelee(player.meleeIndex);
        } else if (act === 'buy-ability') {
          const id = btn.dataset.id;
          const ab = SPECIAL_ABILITIES.find(a => a.id === id);
          if (ab && player.credits >= ab.cost) {
            player.credits -= ab.cost;
            if (ab.isInstant) {
              player.hp = player.maxHp;
              player.armor = player.maxArmor;
              player.shells = player.maxShells;
              player.cells = player.maxCells;
              player.mags = WEAPONS.map(w => w.magSize);
              sound.playReload(player.weaponIndex);
              addKillfeed('📦 FULL TACTICAL RESUPPLY APPLIED (100 HP / 100 ARMOR / MAX AMMO)!');
            } else {
              player.abilityCharges[id] = (player.abilityCharges[id] || 0) + ab.chargesPerBuy;
              player.equippedAbility = id;
              sound.playOverdrive();
              addKillfeed(`⚡ PURCHASED +${ab.chargesPerBuy} ${ab.name} CHARGES!`);
            }
          }
        } else if (act === 'equip-ability') {
          player.equippedAbility = btn.dataset.id;
          sound.playPickup();
        }
        renderShopItems(gameState.activeShopCat);
        updateHUD();
      });
    });
  }

  function addKillfeed(msg) {
    const feed = document.getElementById('killfeed');
    const item = document.createElement('div');
    item.className = 'kf-item';
    item.textContent = msg;
    feed.prepend(item);
    while (feed.children.length > 3) {
      feed.removeChild(feed.lastChild);
    }
  }

  function showHitmarker() {
    const hm = document.getElementById('hitmarker');
    hm.classList.remove('hidden');
    clearTimeout(hm._timer);
    hm._timer = setTimeout(() => hm.classList.add('hidden'), 110);
  }

  function flashScreen(id, durationSec) {
    const el = document.getElementById(id);
    if (!el) return;
    el.style.opacity = '1';
    setTimeout(() => {
      el.style.opacity = '0';
    }, durationSec * 1000);
  }

  function handleBeaconEntered() {
    gameState.paused = true;
    gameState.isADS = false;
    if (document.exitPointerLock) document.exitPointerLock();

    if (gameState.stratumIndex < STRATA.length - 1) {
      showStateModal({
        badge: `STRATUM ${gameState.stratumIndex + 1} COMPLETE`,
        title: 'CITADEL PORTAL BREACHED',
        subtitle: `Tectonic seals broken. Prepare to ascend to ${STRATA[gameState.stratumIndex + 1].name}.`,
        primaryLabel: 'ASCEND TO NEXT STRATUM ▶',
        onPrimary: () => {
          document.getElementById('state-modal').classList.add('hidden');
          player.hp = Math.min(player.maxHp, player.hp + 30);
          loadStratum(gameState.stratumIndex + 1);
          gameState.paused = false;
          if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
        }
      });
    } else {
      gameState.score += 2500;
      showStateModal({
        badge: 'TOTAL ASCENSION ACHIEVED // VICTORY',
        title: 'THE EMPYREAN BREACH IS SEALED',
        subtitle: 'Archon Malakor has fallen. You have conquered all three strata of the Demonic Citadel!',
        primaryLabel: 'PLAY AGAIN',
        onPrimary: () => startNewGame()
      });
    }
  }

  function triggerGameOver(reason) {
    gameState.paused = true;
    gameState.isADS = false;
    sound.stopMusic();
    if (document.exitPointerLock) document.exitPointerLock();

    showStateModal({
      badge: 'ASCENSION FAILED // DEFEAT',
      title: 'YOUR VESSEL HAS FALLEN',
      subtitle: reason,
      primaryLabel: 'RETRY STRATUM',
      onPrimary: () => {
        document.getElementById('state-modal').classList.add('hidden');
        player.hp = 100;
        player.armor = 50;
        player.shells = Math.max(24, player.shells);
        player.cells = Math.max(80, player.cells);
        player.mags = [30, 8, 60, 1];
        loadStratum(gameState.stratumIndex);
        gameState.paused = false;
        sound.startMusic();
        if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
      }
    });
  }

  function pauseGame() {
    if (!gameState.running || gameState.paused) return;
    gameState.paused = true;
    gameState.isADS = false;
    if (document.exitPointerLock) document.exitPointerLock();
    showStateModal({
      badge: 'SIMULATION PAUSED',
      title: 'TACTICAL COMMAND STANDBY',
      subtitle: 'Inspect the 6-Criteria Game Design Rubric above or resume your battle.',
      primaryLabel: 'RESUME ASCENSION ▶',
      onPrimary: () => {
        document.getElementById('state-modal').classList.add('hidden');
        gameState.paused = false;
        if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
      }
    });
  }

  function togglePauseMenu() {
    if (gameState.paused) {
      document.getElementById('state-modal').classList.add('hidden');
      gameState.paused = false;
      if (renderer.domElement.requestPointerLock) renderer.domElement.requestPointerLock();
    } else {
      pauseGame();
    }
  }

  function showStateModal({ badge, title, subtitle, primaryLabel, onPrimary }) {
    document.getElementById('modal-badge').textContent = badge;
    document.getElementById('modal-title').textContent = title;
    document.getElementById('modal-subtitle').textContent = subtitle;
    document.getElementById('stat-score').textContent = gameState.score;
    document.getElementById('stat-kills').textContent = gameState.kills;
    document.getElementById('stat-mult').textContent = `${gameState.peakMultiplier.toFixed(1)}×`;

    const rank = gameState.score >= 12000 ? 'S+ SOVEREIGN' :
                 gameState.score >= 7000 ? 'S WARDEN' :
                 gameState.score >= 3500 ? 'A ASCENDANT' : 'B INITIATE';
    document.getElementById('stat-rank').textContent = rank;

    const primaryBtn = document.getElementById('btn-modal-primary');
    primaryBtn.textContent = primaryLabel;
    primaryBtn.onclick = onPrimary;

    document.getElementById('state-modal').classList.remove('hidden');
  }

  document.getElementById('btn-start-game').addEventListener('click', startNewGame);
  document.getElementById('btn-modal-restart').addEventListener('click', startNewGame);
  document.getElementById('btn-modal-menu').addEventListener('click', () => {
    gameState.running = false;
    gameState.paused = false;
    sound.stopMusic();
    document.getElementById('state-modal').classList.add('hidden');
    document.getElementById('menu-overlay').classList.remove('hidden');
  });

  function formatTime(sec) {
    const m = Math.floor(sec / 60);
    const s = (sec % 60).toFixed(1).padStart(4, '0');
    return `${String(m).padStart(2, '0')}:${s}`;
  }

  // =========================================================================
  // 13. "BLACK"-INSPIRED SHATTERED GLASS & DIAGONAL RIFLE LANDING RENDERER
  // =========================================================================
  const heroCanvas = document.getElementById('landing-hero-canvas');
  const heroCtx = heroCanvas ? heroCanvas.getContext('2d') : null;
  const titleStage = document.getElementById('black-title-stage');

  const landingFX = {
    mouseX: 0,
    mouseY: 0,
    targetX: 0,
    targetY: 0,
    shockwave: 0,
    shards: [],
    embers: [],
    streaks: [],
    smokeWisps: []
  };

  function initBlackLandingFX() {
    if (!heroCanvas) return;

    // 1. Generate 145 Razor-Sharp Shattered Glass Shards radiating from the blast core
    landingFX.shards = [];
    for (let i = 0; i < 145; i++) {
      const angle = Math.random() * Math.PI * 2;
      const dist = 20 + Math.pow(Math.random(), 0.75) * 680;
      const numVerts = 3 + Math.floor(Math.random() * 3);
      const size = 6 + Math.random() * 34;
      const verts = [];
      for (let v = 0; v < numVerts; v++) {
        const va = (v / numVerts) * Math.PI * 2 + (Math.random() - 0.5) * 0.5;
        const vr = size * (0.45 + Math.random() * 0.65);
        verts.push({ x: Math.cos(va) * vr, y: Math.sin(va) * vr });
      }
      landingFX.shards.push({
        angle,
        dist,
        speed: 12 + Math.random() * 45,
        rot: Math.random() * Math.PI * 2,
        rotSpeed: (Math.random() - 0.5) * 1.4,
        depth: 0.3 + Math.random() * 1.2,
        alpha: 0.25 + Math.random() * 0.6,
        glint: Math.random() > 0.65,
        verts
      });
    }

    // 2. Generate 95 Golden-Amber Blast Embers at the shatter origin
    landingFX.embers = [];
    for (let i = 0; i < 95; i++) {
      const angle = Math.random() * Math.PI * 2;
      const dist = Math.pow(Math.random(), 1.3) * 290;
      landingFX.embers.push({
        angle,
        dist,
        size: 1.5 + Math.random() * 4.5,
        speed: 18 + Math.random() * 55,
        color: Math.random() > 0.35 ? '#ffaa2b' : '#fff0b8'
      });
    }

    // 3. Generate Radial Ballistic Trajectory Streaks (Matching BLACK key art)
    landingFX.streaks = [];
    for (let i = 0; i < 42; i++) {
      landingFX.streaks.push({
        angle: -Math.PI * 0.85 + Math.random() * Math.PI * 1.85,
        innerR: 35 + Math.random() * 90,
        outerR: 320 + Math.random() * 820,
        width: 0.6 + Math.random() * 2.2,
        gold: Math.random() > 0.65,
        alpha: 0.14 + Math.random() * 0.42
      });
    }

    // 4. Generate Curling Gunsmoke Ribbons along the diagonal barrel
    landingFX.smokeWisps = [];
    for (let i = 0; i < 9; i++) {
      landingFX.smokeWisps.push({
        offsetT: i / 9,
        amp: 18 + Math.random() * 32,
        freq: 0.8 + Math.random() * 1.2,
        thickness: 14 + Math.random() * 26,
        alpha: 0.08 + Math.random() * 0.14
      });
    }

    window.addEventListener('mousemove', e => {
      if (gameState.running && !gameState.paused) return;
      landingFX.targetX = (e.clientX / window.innerWidth - 0.5) * 2;
      landingFX.targetY = (e.clientY / window.innerHeight - 0.5) * 2;
    });

    document.getElementById('menu-overlay').addEventListener('mousedown', () => {
      landingFX.shockwave = 1.0;
      sound.playShoot(0);
    });
  }

  function renderBlackLandingHero(now, dt) {
    if (!heroCtx || (gameState.running && !gameState.paused)) return;

    const W = heroCanvas.width;
    const H = heroCanvas.height;

    landingFX.mouseX += (landingFX.targetX - landingFX.mouseX) * Math.min(1, dt * 6);
    landingFX.mouseY += (landingFX.targetY - landingFX.mouseY) * Math.min(1, dt * 6);
    if (landingFX.shockwave > 0) {
      landingFX.shockwave = Math.max(0, landingFX.shockwave - dt * 3.2);
    }

    // Subtle parallax on the brushed-brass title stage
    if (titleStage) {
      const tx = landingFX.mouseX * -14;
      const ty = landingFX.mouseY * -8;
      titleStage.style.transform = `translate(${tx}px, ${ty}px)`;
    }

    // Blast origin in lower-left (behind "DO" of DOOM, matching BLACK cover art)
    const originX = W * 0.27 + landingFX.mouseX * 18;
    const originY = H * 0.62 + landingFX.mouseY * 14;

    // 1. Pitch-Black Studio Void Background
    heroCtx.fillStyle = '#050504';
    heroCtx.fillRect(0, 0, W, H);

    // 2. Fiery Golden-Bronze Shatter Core Glow at Lower-Left Origin
    const coreRadius = 340 + landingFX.shockwave * 140;
    const coreGrad = heroCtx.createRadialGradient(originX, originY, 8, originX, originY, coreRadius);
    coreGrad.addColorStop(0, 'rgba(255, 230, 155, 0.92)');
    coreGrad.addColorStop(0.22, 'rgba(235, 145, 38, 0.62)');
    coreGrad.addColorStop(0.55, 'rgba(85, 55, 22, 0.22)');
    coreGrad.addColorStop(1, 'rgba(5, 5, 4, 0)');
    heroCtx.fillStyle = coreGrad;
    heroCtx.fillRect(0, 0, W, H);

    // 3. Radial Ballistic Trajectory Streaks
    heroCtx.save();
    heroCtx.translate(originX, originY);
    for (const st of landingFX.streaks) {
      heroCtx.strokeStyle = st.gold
        ? `rgba(218, 168, 75, ${st.alpha})`
        : `rgba(195, 210, 225, ${st.alpha})`;
      heroCtx.lineWidth = st.width;
      heroCtx.beginPath();
      heroCtx.moveTo(Math.cos(st.angle) * st.innerR, Math.sin(st.angle) * st.innerR);
      heroCtx.lineTo(Math.cos(st.angle) * st.outerR, Math.sin(st.angle) * st.outerR);
      heroCtx.stroke();
    }
    heroCtx.restore();

    // 4. DIAGONAL METALLIC ASSAULT RIFLE BARREL, A-FRAME FRONT SIGHT & FLASH HIDER (Matching BLACK Cover!)
    heroCtx.save();
    const kickOffset = landingFX.shockwave * -22;
    heroCtx.translate(originX + kickOffset, originY - kickOffset * 0.55);
    // Diagonal angle pointing from bottom-left to upper-right (-28.5 degrees)
    heroCtx.rotate(-0.498 + landingFX.mouseY * 0.025);

    // Metallic cylinder shader helper
    const makeSteelGrad = (yTop, yBot) => {
      const g = heroCtx.createLinearGradient(0, yTop, 0, yBot);
      g.addColorStop(0, '#4a525a');
      g.addColorStop(0.22, '#e2ebf2');
      g.addColorStop(0.45, '#8c97a3');
      g.addColorStop(0.76, '#1a2026');
      g.addColorStop(1, '#58636e');
      return g;
    };

    // 4A. Detached / Exploding Carry-Handle Rear Sight Assembly on Left (Like BLACK cover!)
    heroCtx.save();
    heroCtx.translate(-185 - landingFX.mouseX * 15, -48 - landingFX.mouseY * 12);
    heroCtx.rotate(-0.18);
    heroCtx.fillStyle = makeSteelGrad(-32, 32);
    heroCtx.beginPath();
    heroCtx.moveTo(-75, 22);
    heroCtx.lineTo(75, 22);
    heroCtx.lineTo(62, -24);
    heroCtx.lineTo(-58, -24);
    heroCtx.closePath();
    heroCtx.fill();
    heroCtx.strokeStyle = '#d8e3ec';
    heroCtx.lineWidth = 1.5;
    heroCtx.stroke();
    // Carry handle thumb-nuts
    [-42, 38].forEach(nx => {
      heroCtx.fillStyle = '#c5ced6';
      heroCtx.beginPath();
      heroCtx.arc(nx, 10, 9, 0, Math.PI * 2);
      heroCtx.fill();
      heroCtx.strokeStyle = '#111';
      heroCtx.stroke();
    });
    heroCtx.restore();

    // 4B. Upper Receiver, Delta Ring & Handguard Cap emerging from the shatter cloud
    heroCtx.fillStyle = makeSteelGrad(-44, 44);
    heroCtx.fillRect(-40, -40, 175, 80);

    // Tapered Delta Ring
    heroCtx.beginPath();
    heroCtx.moveTo(135, -42);
    heroCtx.lineTo(178, -32);
    heroCtx.lineTo(178, 32);
    heroCtx.lineTo(135, 42);
    heroCtx.closePath();
    heroCtx.fill();

    // Ribbed Handguard Barrel Shroud
    heroCtx.fillStyle = makeSteelGrad(-30, 30);
    heroCtx.fillRect(178, -28, 170, 56);
    heroCtx.fillStyle = '#14181d';
    for (let rx = 190; rx < 340; rx += 18) {
      heroCtx.fillRect(rx, -26, 8, 52);
    }

    // 4C. Heavy Steel Rifle Barrel extending to the upper-right
    heroCtx.fillStyle = makeSteelGrad(-19, 19);
    heroCtx.fillRect(348, -18, 415, 36);

    // Step-down M4 profile cut on the barrel
    heroCtx.fillStyle = makeSteelGrad(-15, 15);
    heroCtx.fillRect(545, -14, 95, 28);

    // Black carbon/gunpowder scorch marks across the steel barrel
    heroCtx.fillStyle = 'rgba(12, 14, 16, 0.65)';
    heroCtx.beginPath();
    heroCtx.moveTo(580, -14);
    heroCtx.lineTo(625, -14);
    heroCtx.lineTo(595, 14);
    heroCtx.lineTo(560, 14);
    heroCtx.fill();

    // 4D. ICONIC A-FRAME FRONT SIGHT TOWER (M16/M4 Triangular Sight Post in Center-Top of Reference Image!)
    heroCtx.fillStyle = makeSteelGrad(-175, 45);
    heroCtx.beginPath();
    heroCtx.moveTo(355, -18);   // Base rear
    heroCtx.lineTo(395, -168);  // Rear vertical sight tower
    heroCtx.lineTo(445, -168);  // Top sight ears
    heroCtx.lineTo(525, -18);   // Slanted front A-frame strut
    heroCtx.lineTo(525, 32);    // Lower bayonet lug collar
    heroCtx.lineTo(355, 32);
    heroCtx.closePath();
    heroCtx.fill();
    heroCtx.strokeStyle = 'rgba(235, 245, 255, 0.65)';
    heroCtx.lineWidth = 2;
    heroCtx.stroke();

    // Inner triangular cutout inside the A-frame sight tower
    heroCtx.fillStyle = '#050504';
    heroCtx.beginPath();
    heroCtx.moveTo(405, -24);
    heroCtx.lineTo(422, -125);
    heroCtx.lineTo(482, -24);
    heroCtx.closePath();
    heroCtx.fill();
    heroCtx.strokeStyle = '#9aa6b2';
    heroCtx.lineWidth = 1.5;
    heroCtx.stroke();

    // Protective Front Sight Ears & Sight Post Pin at top of A-frame
    heroCtx.fillStyle = '#d4dee8';
    heroCtx.fillRect(402, -186, 12, 20);
    heroCtx.fillRect(430, -186, 12, 20);
    heroCtx.fillStyle = '#ff5522';
    heroCtx.fillRect(419, -182, 6, 16);

    // 4E. A2 BIRDCAGE FLASH HIDER / MUZZLE COMPENSATOR AT UPPER-RIGHT TIP (Matching Reference Image!)
    heroCtx.fillStyle = makeSteelGrad(-25, 25);
    heroCtx.beginPath();
    heroCtx.roundRect(745, -24, 155, 48, 6);
    heroCtx.fill();
    heroCtx.strokeStyle = '#dce6f0';
    heroCtx.lineWidth = 1.5;
    heroCtx.stroke();

    // Circular machined wrench grooves on the flash hider
    heroCtx.fillStyle = '#181d22';
    heroCtx.fillRect(768, -24, 7, 48);
    heroCtx.fillRect(784, -24, 7, 48);

    // Dark Oblong Muzzle Compensator Vented Port Slot (Exact match to reference photo!)
    heroCtx.fillStyle = '#07090b';
    heroCtx.beginPath();
    heroCtx.roundRect(808, -11, 56, 20, 8);
    heroCtx.fill();
    heroCtx.strokeStyle = '#9aa6b2';
    heroCtx.lineWidth = 1.5;
    heroCtx.stroke();

    // Foreshortened Elliptical Hollow Muzzle Bore at the very tip (x = 900)
    heroCtx.fillStyle = '#060709';
    heroCtx.beginPath();
    heroCtx.ellipse(898, 0, 12, 23, 0, 0, Math.PI * 2);
    heroCtx.fill();
    heroCtx.strokeStyle = '#d8e4ee';
    heroCtx.lineWidth = 3;
    heroCtx.stroke();

    heroCtx.restore();

    // 5. CURLING GUNSMOKE RIBBONS DRIFTING ACROSS THE BARREL (Like Reference Image!)
    heroCtx.save();
    for (const sm of landingFX.smokeWisps) {
      const t = (sm.offsetT + now * 0.00008) % 1;
      const sx = originX + t * (W * 0.65);
      const sy = originY - t * (H * 0.58) + Math.sin(now * 0.0015 * sm.freq + sm.offsetT * 12) * sm.amp;

      const sGrad = heroCtx.createRadialGradient(sx, sy, 4, sx, sy, sm.thickness * 3.2);
      sGrad.addColorStop(0, `rgba(215, 222, 230, ${sm.alpha})`);
      sGrad.addColorStop(0.5, `rgba(140, 150, 160, ${sm.alpha * 0.5})`);
      sGrad.addColorStop(1, 'rgba(10, 10, 12, 0)');
      heroCtx.fillStyle = sGrad;
      heroCtx.beginPath();
      heroCtx.ellipse(sx, sy, sm.thickness * 4.2, sm.thickness * 1.4, -0.48, 0, Math.PI * 2);
      heroCtx.fill();
    }
    heroCtx.restore();

    // 6. GOLDEN MOLTEN BLAST SPARKS AT ORIGIN
    heroCtx.save();
    for (const em of landingFX.embers) {
      const r = (em.dist + now * 0.001 * em.speed) % 310;
      const ex = originX + Math.cos(em.angle) * r;
      const ey = originY + Math.sin(em.angle) * r;
      const fade = 1 - r / 310;
      heroCtx.fillStyle = em.color;
      heroCtx.globalAlpha = fade * 0.9;
      heroCtx.fillRect(ex, ey, em.size, em.size);
    }
    heroCtx.restore();

    // 7. 145 RAZOR-SHARP SHATTERED GLASS SHARDS WITH SPECULAR WHITE RIMS
    heroCtx.save();
    for (const sh of landingFX.shards) {
      const r = (sh.dist + now * 0.001 * sh.speed + landingFX.shockwave * 55 * sh.depth) % 720;
      const px = originX + Math.cos(sh.angle) * r + landingFX.mouseX * 28 * sh.depth;
      const py = originY + Math.sin(sh.angle) * r + landingFX.mouseY * 22 * sh.depth;
      const rot = sh.rot + now * 0.001 * sh.rotSpeed;
      const fade = Math.min(1, (1 - r / 720) * 1.35);

      heroCtx.save();
      heroCtx.translate(px, py);
      heroCtx.rotate(rot);

      heroCtx.beginPath();
      sh.verts.forEach((v, idx) => {
        if (idx === 0) heroCtx.moveTo(v.x, v.y);
        else heroCtx.lineTo(v.x, v.y);
      });
      heroCtx.closePath();

      // Translucent shattered glass body
      heroCtx.fillStyle = sh.glint
        ? `rgba(225, 240, 255, ${sh.alpha * fade * 0.55})`
        : `rgba(110, 130, 148, ${sh.alpha * fade * 0.35})`;
      heroCtx.fill();

      // Bright razor-sharp glass edge highlight
      heroCtx.strokeStyle = `rgba(245, 250, 255, ${Math.min(1, sh.alpha * fade * 1.4)})`;
      heroCtx.lineWidth = sh.glint ? 1.8 : 1.0;
      heroCtx.stroke();

      heroCtx.restore();
    }
    heroCtx.restore();
  }

  initBlackLandingFX();

  // =========================================================================
  // 14. MAIN RENDER & ANIMATION LOOP
  // =========================================================================
  let lastFrameTime = performance.now();
  function mainLoop(now) {
    const dt = Math.min(0.05, (now - lastFrameTime) / 1000);
    lastFrameTime = now;

    updateAtmosphereFX(dt);

    if (gameState.running && !gameState.paused) {
      updateSimulation(dt);
    } else if (!gameState.running) {
      renderBlackLandingHero(now, dt);
      camera.position.set(1.5 + Math.sin(now * 0.0004) * 0.6, 1.85, 11.5);
      camera.rotation.set(0.05, 0.16 + Math.cos(now * 0.0003) * 0.04, 0, 'YXZ');
    }

    renderer.render(scene, camera);
    renderMinimap();
    requestAnimationFrame(mainLoop);
  }

  loadStratum(0);
  requestAnimationFrame(mainLoop);
})();
