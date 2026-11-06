/**
 * DOOM: HELL'S ASCENSION — 3D Raycasting FPS Engine & Game Design Simulation
 * Custom 60FPS Raycaster, Procedural Texture/Sprite Synthesizer, WebAudio Industrial Synth
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

  // Criteria filter buttons
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

  // Scanlines toggle
  const gameWrapper = document.getElementById('game-wrapper');
  document.getElementById('btn-toggle-crt').addEventListener('click', () => {
    gameWrapper.classList.toggle('scanlines-on');
  });

  // =========================================================================
  // 2. WEBAUDIO PROCEDURAL SOUND & INDUSTRIAL SYNTH ENGINE
  // =========================================================================
  class SoundEngine {
    constructor() {
      this.ctx = null;
      this.enabled = true;
      this.musicTimer = null;
      this.step = 0;
    }

    init() {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) this.ctx = new AudioCtx();
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

    playShoot(weaponId) {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      const now = this.ctx.currentTime;

      if (weaponId === 0) {
        // Cinder-Lance: crisp laser-ash zap
        this.tone(420, 95, 0.14, 'sawtooth', 0.22);
        this.noise(0.08, 0.14);
      } else if (weaponId === 1) {
        // Hellfire Scatter-Maw: heavy pneumatic shotgun boom
        this.tone(150, 38, 0.28, 'sawtooth', 0.35);
        this.noise(0.24, 0.38);
      } else if (weaponId === 2) {
        // Soul-Tether Pulse Rifle: rapid plasma bolt
        this.tone(620, 180, 0.09, 'square', 0.18);
      } else if (weaponId === 3) {
        // B.F.G. Singularity: deep charging sub-bass + shockwave
        this.tone(90, 28, 0.65, 'sawtooth', 0.42);
        this.tone(540, 110, 0.55, 'sine', 0.3);
        this.noise(0.5, 0.4);
      }
    }

    playHit(isKill = false) {
      if (!this.enabled || !this.ctx) return;
      if (isKill) {
        this.tone(240, 70, 0.22, 'sawtooth', 0.24);
        this.tone(580, 880, 0.12, 'triangle', 0.18);
      } else {
        this.tone(780, 420, 0.05, 'triangle', 0.15);
      }
    }

    playPlayerDamage() {
      if (!this.enabled || !this.ctx) return;
      this.tone(130, 45, 0.2, 'sawtooth', 0.3);
      this.noise(0.14, 0.22);
    }

    playPickup() {
      if (!this.enabled || !this.ctx) return;
      this.tone(440, 880, 0.14, 'sine', 0.2);
    }

    playDash() {
      if (!this.enabled || !this.ctx) return;
      this.tone(280, 620, 0.12, 'sine', 0.18);
      this.noise(0.1, 0.12);
    }

    playOverdrive() {
      if (!this.enabled || !this.ctx) return;
      this.tone(220, 880, 0.45, 'sawtooth', 0.3);
      this.tone(330, 660, 0.45, 'triangle', 0.25);
    }

    tone(startFreq, endFreq, duration, type = 'sawtooth', gainVal = 0.2) {
      if (!this.ctx) return;
      const now = this.ctx.currentTime;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(startFreq, now);
      osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), now + duration);
      gain.gain.setValueAtTime(gainVal, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + duration);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start(now);
      osc.stop(now + duration);
    }

    noise(duration, gainVal = 0.2) {
      if (!this.ctx) return;
      const bufferSize = this.ctx.sampleRate * duration;
      const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i++) {
        data[i] = Math.random() * 2 - 1;
      }
      const src = this.ctx.createBufferSource();
      src.buffer = buffer;
      const filter = this.ctx.createBiquadFilter();
      filter.type = 'lowpass';
      filter.frequency.value = 1100;
      const gain = this.ctx.createGain();
      const now = this.ctx.currentTime;
      gain.gain.setValueAtTime(gainVal, now);
      gain.gain.exponentialRampToValueAtTime(0.01, now + duration);
      src.connect(filter);
      filter.connect(gain);
      gain.connect(this.ctx.destination);
      src.start(now);
    }

    startMusic() {
      if (!this.enabled) return;
      this.init();
      this.stopMusic();
      const riff = [55, 55, 65.4, 55, 73.4, 55, 61.7, 58.3]; // Dark D-minor/Phrygian industrial bass
      this.musicTimer = setInterval(() => {
        if (!this.enabled || !gameState.running || gameState.paused) return;
        const freq = riff[this.step % riff.length];
        this.tone(freq, freq * 0.98, 0.16, 'sawtooth', 0.075);
        if (this.step % 4 === 0) {
          this.tone(110, 32, 0.11, 'sine', 0.11); // Kick thump
        }
        if (this.step % 4 === 2) {
          this.noise(0.06, 0.05); // Industrial snare hiss
        }
        this.step++;
      }, 185);
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
  // 3. PROCEDURAL WALL TEXTURES & SPRITE ART GENERATOR
  // =========================================================================
  const TEX_SIZE = 64;
  const wallTextures = {};

  function generateWallTextures() {
    // 1: Obsidian Brick with Crimson Veins (Nadir Vault)
    wallTextures[1] = createTextureCanvas((ctx, w, h) => {
      ctx.fillStyle = '#181421';
      ctx.fillRect(0, 0, w, h);
      ctx.strokeStyle = '#09070d';
      ctx.lineWidth = 2;
      for (let y = 0; y < h; y += 16) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(w, y);
        ctx.stroke();
        const offset = (y / 16) % 2 === 0 ? 0 : 16;
        for (let x = offset; x < w; x += 32) {
          ctx.beginPath();
          ctx.moveTo(x, y);
          ctx.lineTo(x, y + 16);
          ctx.stroke();
        }
      }
      // Glowing crimson rune strip
      ctx.fillStyle = '#ff3b22';
      ctx.fillRect(0, 28, w, 3);
      ctx.fillStyle = '#ff9233';
      for (let x = 6; x < w; x += 16) {
        ctx.fillRect(x, 26, 4, 7);
      }
    });

    // 2: Crucible Brass & Molten Industrial Slag
    wallTextures[2] = createTextureCanvas((ctx, w, h) => {
      ctx.fillStyle = '#261a14';
      ctx.fillRect(0, 0, w, h);
      ctx.fillStyle = '#3b281e';
      ctx.fillRect(4, 4, w - 8, h - 8);
      // Molten core grate
      ctx.fillStyle = '#ff6b1a';
      ctx.fillRect(10, 18, w - 20, 28);
      ctx.fillStyle = '#1a110d';
      for (let x = 14; x < w - 14; x += 8) {
        ctx.fillRect(x, 18, 4, 28);
      }
      // Rivets
      ctx.fillStyle = '#ffd15c';
      [[6, 6], [w - 10, 6], [6, h - 10], [w - 10, h - 10]].forEach(([rx, ry]) => {
        ctx.fillRect(rx, ry, 4, 4);
      });
    });

    // 3: Empyrean Blood-Cathedral Pillar Stone
    wallTextures[3] = createTextureCanvas((ctx, w, h) => {
      ctx.fillStyle = '#230d1a';
      ctx.fillRect(0, 0, w, h);
      ctx.fillStyle = '#38142b';
      ctx.fillRect(6, 0, w - 12, h);
      ctx.strokeStyle = '#b85cff';
      ctx.lineWidth = 2;
      ctx.strokeRect(12, 8, w - 24, h - 16);
      ctx.fillStyle = '#ffd15c';
      ctx.fillRect(w / 2 - 3, 16, 6, h - 32);
    });

    // 4: Ascension Rift Beacon Gate
    wallTextures[4] = createTextureCanvas((ctx, w, h) => {
      const grad = ctx.createLinearGradient(0, 0, 0, h);
      grad.addColorStop(0, '#ff3b22');
      grad.addColorStop(0.5, '#ffd15c');
      grad.addColorStop(1, '#b85cff');
      ctx.fillStyle = grad;
      ctx.fillRect(0, 0, w, h);
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 3;
      ctx.strokeRect(6, 6, w - 12, h - 12);
      ctx.fillStyle = '#12081c';
      ctx.fillRect(18, 18, w - 36, h - 36);
      ctx.fillStyle = '#ffd15c';
      ctx.fillRect(w / 2 - 4, h / 2 - 4, 8, 8);
    });
  }

  function createTextureCanvas(drawFn) {
    const c = document.createElement('canvas');
    c.width = TEX_SIZE;
    c.height = TEX_SIZE;
    const ctx = c.getContext('2d');
    drawFn(ctx, TEX_SIZE, TEX_SIZE);
    return c;
  }

  generateWallTextures();

  // =========================================================================
  // 4. WEAPON DEFINITIONS & ENEMY ARCHETYPES
  // =========================================================================
  const WEAPONS = [
    {
      id: 0,
      name: 'CINDER-LANCE',
      ammoType: 'infinite',
      ammoLabel: 'THERMAL CELL',
      cost: 0,
      damage: 28,
      cooldown: 0.24,
      pellets: 1,
      spread: 0.015,
      color: '#ff9233',
      auto: false,
      resonanceGain: 7
    },
    {
      id: 1,
      name: 'HELLFIRE SCATTER-MAW',
      ammoType: 'shells',
      ammoLabel: 'SLAG SHELLS',
      cost: 1,
      damage: 19,
      cooldown: 0.58,
      pellets: 7,
      spread: 0.14,
      color: '#ff3b22',
      auto: false,
      resonanceGain: 12
    },
    {
      id: 2,
      name: 'SOUL-TETHER PULSE RIFLE',
      ammoType: 'cells',
      ammoLabel: 'PLASMA CELLS',
      cost: 1,
      damage: 23,
      cooldown: 0.11,
      pellets: 1,
      spread: 0.035,
      color: '#24d8ff',
      auto: true,
      resonanceGain: 6
    },
    {
      id: 3,
      name: 'B.F.G. SINGULARITY',
      ammoType: 'cells',
      ammoLabel: 'PLASMA CELLS (25)',
      cost: 25,
      damage: 360,
      cooldown: 1.05,
      pellets: 1,
      spread: 0,
      color: '#b85cff',
      auto: false,
      isBFG: true,
      resonanceGain: 25
    }
  ];

  const ENEMY_TYPES = {
    wretch: {
      type: 'wretch',
      name: 'ASH-WRETCH',
      hp: 65,
      speed: 2.35,
      damage: 12,
      attackRange: 1.35,
      attackCooldown: 0.95,
      ranged: false,
      score: 150,
      scale: 0.82,
      primaryColor: '#ff5232',
      secondaryColor: '#3a1712',
      eyeColor: '#ffd15c'
    },
    imp: {
      type: 'imp',
      name: 'CINDER-IMP',
      hp: 95,
      speed: 1.75,
      damage: 14,
      attackRange: 8.5,
      attackCooldown: 1.65,
      ranged: true,
      projColor: '#ff7b1c',
      projSpeed: 6.2,
      score: 250,
      scale: 0.9,
      primaryColor: '#d9381e',
      secondaryColor: '#5c1916',
      eyeColor: '#24d8ff'
    },
    gorger: {
      type: 'gorger',
      name: 'IRON-GORGER',
      hp: 240,
      speed: 1.2,
      damage: 22,
      attackRange: 7.5,
      attackCooldown: 2.0,
      ranged: true,
      projColor: '#ff3b22',
      projSpeed: 5.2,
      score: 450,
      scale: 1.12,
      primaryColor: '#7a685d',
      secondaryColor: '#2b231f',
      eyeColor: '#ff3b22'
    },
    cantor: {
      type: 'cantor',
      name: 'VOID-CANTOR',
      hp: 150,
      speed: 1.85,
      damage: 18,
      attackRange: 9.5,
      attackCooldown: 1.5,
      ranged: true,
      projColor: '#b85cff',
      projSpeed: 7.4,
      canBlink: true,
      score: 380,
      scale: 0.96,
      primaryColor: '#8c3bff',
      secondaryColor: '#1f0e38',
      eyeColor: '#ffd15c'
    },
    boss: {
      type: 'boss',
      name: 'ARCHON MALAKOR',
      hp: 1200,
      speed: 1.35,
      damage: 26,
      attackRange: 11.5,
      attackCooldown: 1.45,
      ranged: true,
      isBoss: true,
      projColor: '#ffd15c',
      projSpeed: 6.8,
      score: 3500,
      scale: 1.38,
      primaryColor: '#ff2a1f',
      secondaryColor: '#290826',
      eyeColor: '#ffd15c'
    }
  };

  // =========================================================================
  // 5. THREE STRATA LEVEL MAPS (16 x 16)
  // =========================================================================
  const STRATA = [
    {
      name: 'STRATUM I: THE NADIR VAULT',
      ceilColor: '#09070f',
      floorColor: '#13101c',
      fogColor: [9, 7, 15],
      playerStart: { x: 2.5, y: 2.5, angle: 0 },
      beaconPos: { x: 13.5, y: 13.5 },
      map: [
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
        [1,0,0,0,0,1,0,0,0,0,0,1,0,0,0,1],
        [1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1],
        [1,0,0,1,0,1,0,0,1,0,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,1,0,1,1,1,0,0,0,0,1,1,0,1,1,1],
        [1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1],
        [1,0,0,0,0,0,1,2,2,1,0,0,0,0,0,1],
        [1,0,0,0,0,0,1,2,2,1,0,0,0,0,0,1],
        [1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1],
        [1,1,0,1,1,0,0,0,0,0,1,1,0,1,1,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,0,1,0,0,1,0,0,1,0,0,0,1],
        [1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1],
        [1,0,0,0,0,1,0,0,0,0,0,1,0,0,4,1],
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1]
      ],
      spawns: [
        { type: 'wretch', x: 6.5, y: 2.5 },
        { type: 'wretch', x: 9.5, y: 3.5 },
        { type: 'imp', x: 13.5, y: 2.5 },
        { type: 'wretch', x: 3.5, y: 7.5 },
        { type: 'imp', x: 11.5, y: 7.5 },
        { type: 'wretch', x: 5.5, y: 11.5 },
        { type: 'imp', x: 3.5, y: 13.5 },
        { type: 'gorger', x: 10.5, y: 12.5 },
        { type: 'imp', x: 12.5, y: 10.5 }
      ],
      pickups: [
        { kind: 'shells', x: 4.5, y: 2.5 },
        { kind: 'hp', x: 11.5, y: 4.5 },
        { kind: 'cells', x: 2.5, y: 9.5 },
        { kind: 'armor', x: 13.5, y: 8.5 },
        { kind: 'shells', x: 7.5, y: 13.5 }
      ]
    },
    {
      name: 'STRATUM II: THE CRUCIBLE FOUNDRY',
      ceilColor: '#140907',
      floorColor: '#1d110e',
      fogColor: [20, 9, 7],
      playerStart: { x: 2.5, y: 2.5, angle: 0 },
      beaconPos: { x: 13.5, y: 13.5 },
      map: [
        [2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2],
        [2,0,0,0,2,0,0,0,0,0,0,2,0,0,0,2],
        [2,0,0,0,0,0,0,2,2,0,0,0,0,0,0,2],
        [2,0,0,2,0,0,0,0,0,0,0,0,2,0,0,2],
        [2,2,0,2,2,0,0,0,0,0,0,2,2,0,2,2],
        [2,0,0,0,0,0,2,0,0,2,0,0,0,0,0,2],
        [2,0,0,0,0,2,2,0,0,2,2,0,0,0,0,2],
        [2,0,2,0,0,0,0,0,0,0,0,0,0,2,0,2],
        [2,0,2,0,0,0,0,0,0,0,0,0,0,2,0,2],
        [2,0,0,0,0,2,2,0,0,2,2,0,0,0,0,2],
        [2,0,0,0,0,0,2,0,0,2,0,0,0,0,0,2],
        [2,2,0,2,2,0,0,0,0,0,0,2,2,0,2,2],
        [2,0,0,2,0,0,0,0,0,0,0,0,2,0,0,2],
        [2,0,0,0,0,0,0,2,2,0,0,0,0,0,0,2],
        [2,0,0,0,2,0,0,0,0,0,0,2,0,0,4,2],
        [2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2]
      ],
      spawns: [
        { type: 'imp', x: 6.5, y: 2.5 },
        { type: 'wretch', x: 10.5, y: 2.5 },
        { type: 'cantor', x: 13.5, y: 3.5 },
        { type: 'gorger', x: 7.5, y: 7.5 },
        { type: 'cantor', x: 8.5, y: 8.5 },
        { type: 'wretch', x: 2.5, y: 10.5 },
        { type: 'imp', x: 13.5, y: 10.5 },
        { type: 'gorger', x: 5.5, y: 13.5 },
        { type: 'cantor', x: 10.5, y: 13.5 },
        { type: 'wretch', x: 12.5, y: 12.5 }
      ],
      pickups: [
        { kind: 'cells', x: 3.5, y: 5.5 },
        { kind: 'shells', x: 12.5, y: 5.5 },
        { kind: 'armor', x: 7.5, y: 4.5 },
        { kind: 'hp', x: 7.5, y: 11.5 },
        { kind: 'cells', x: 2.5, y: 13.5 },
        { kind: 'hp', x: 13.5, y: 7.5 }
      ]
    },
    {
      name: 'STRATUM III: THE EMPYREAN BREACH',
      ceilColor: '#13061a',
      floorColor: '#1b0b24',
      fogColor: [19, 6, 26],
      playerStart: { x: 2.5, y: 2.5, angle: 0 },
      beaconPos: { x: 13.5, y: 13.5 },
      map: [
        [3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3],
        [3,0,0,0,0,0,0,3,3,0,0,0,0,0,0,3],
        [3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3],
        [3,0,0,3,0,0,0,0,0,0,0,0,3,0,0,3],
        [3,0,0,0,0,0,3,0,0,3,0,0,0,0,0,3],
        [3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3],
        [3,0,0,0,3,0,0,0,0,0,0,3,0,0,0,3],
        [3,3,0,0,0,0,0,0,0,0,0,0,0,0,3,3],
        [3,3,0,0,0,0,0,0,0,0,0,0,0,0,3,3],
        [3,0,0,0,3,0,0,0,0,0,0,3,0,0,0,3],
        [3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3],
        [3,0,0,0,0,0,3,0,0,3,0,0,0,0,0,3],
        [3,0,0,3,0,0,0,0,0,0,0,0,3,0,0,3],
        [3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3],
        [3,0,0,0,0,0,0,3,3,0,0,0,0,0,4,3],
        [3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3]
      ],
      spawns: [
        { type: 'wretch', x: 6.5, y: 2.5 },
        { type: 'imp', x: 11.5, y: 2.5 },
        { type: 'cantor', x: 3.5, y: 8.5 },
        { type: 'gorger', x: 12.5, y: 8.5 },
        { type: 'cantor', x: 5.5, y: 12.5 },
        { type: 'boss', x: 9.5, y: 9.5 }
      ],
      pickups: [
        { kind: 'cells', x: 4.5, y: 2.5 },
        { kind: 'shells', x: 2.5, y: 5.5 },
        { kind: 'armor', x: 13.5, y: 2.5 },
        { kind: 'hp', x: 2.5, y: 13.5 },
        { kind: 'cells', x: 13.5, y: 6.5 },
        { kind: 'hp', x: 11.5, y: 13.5 },
        { kind: 'cells', x: 7.5, y: 13.5 }
      ]
    }
  ];

  // =========================================================================
  // 6. CORE GAME STATE & INPUT CONTROLLER
  // =========================================================================
  const canvas = document.getElementById('game-canvas');
  const ctx = canvas.getContext('2d');
  const miniCanvas = document.getElementById('minimap-canvas');
  const miniCtx = miniCanvas.getContext('2d');

  const W = canvas.width;
  const H = canvas.height;
  const FOV = Math.PI / 3;
  const NUM_RAYS = 320; // 3px columns for crisp 60fps performance
  const STRIP_W = W / NUM_RAYS;
  const zBuffer = new Float32Array(NUM_RAYS);

  const keys = {};
  let selectedMode = 'campaign';
  let difficultyMult = 1.0;

  const gameState = {
    running: false,
    paused: false,
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
    ascensionGauge: 0,
    beaconUnlocked: false,
    screenShake: 0,
    muzzleFlash: 0,
    weaponKick: 0,
    bobPhase: 0,
    currentMap: STRATA[0].map,
    enemies: [],
    projectiles: [],
    pickups: [],
    particles: [],
    totalStratumEnemies: 0
  };

  const player = {
    x: 2.5,
    y: 2.5,
    angle: 0,
    hp: 100,
    maxHp: 100,
    armor: 50,
    maxArmor: 100,
    shells: 24,
    maxShells: 60,
    cells: 80,
    maxCells: 200,
    weaponIndex: 0,
    fireCooldown: 0,
    dashCooldown: 0,
    dashTimer: 0,
    dashVx: 0,
    dashVy: 0,
    invulnTimer: 0
  };

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

  // Weapon slot clicks
  document.querySelectorAll('.w-slot').forEach(slotBtn => {
    slotBtn.addEventListener('click', () => {
      selectWeapon(parseInt(slotBtn.dataset.slot, 10));
    });
  });

  function selectWeapon(idx) {
    if (idx >= 0 && idx < WEAPONS.length) {
      player.weaponIndex = idx;
      updateHUD();
    }
  }

  // Input listeners
  window.addEventListener('keydown', e => {
    keys[e.code] = true;
    if (!gameState.running) return;

    if (e.code === 'Digit1') selectWeapon(0);
    if (e.code === 'Digit2') selectWeapon(1);
    if (e.code === 'Digit3') selectWeapon(2);
    if (e.code === 'Digit4') selectWeapon(3);
    if (e.code === 'KeyM') sound.toggle();

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

  // Mouse Look & Fire
  let mouseFiring = false;
  canvas.addEventListener('mousedown', e => {
    if (!gameState.running || gameState.paused) return;
    if (document.pointerLockElement !== canvas && canvas.requestPointerLock) {
      canvas.requestPointerLock();
    }
    if (e.button === 0) {
      mouseFiring = true;
      attemptFire();
    }
  });

  window.addEventListener('mouseup', e => {
    if (e.button === 0) mouseFiring = false;
  });

  window.addEventListener('mousemove', e => {
    if (document.pointerLockElement === canvas && gameState.running && !gameState.paused) {
      player.angle += e.movementX * 0.0026;
    }
  });

  window.addEventListener('wheel', e => {
    if (!gameState.running || gameState.paused) return;
    const dir = e.deltaY > 0 ? 1 : -1;
    const next = (player.weaponIndex + dir + WEAPONS.length) % WEAPONS.length;
    selectWeapon(next);
  });

  // =========================================================================
  // 7. MISSION INITIALIZATION & STRATUM LOADER
  // =========================================================================
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

    player.hp = 100;
    player.armor = 50;
    player.shells = 24;
    player.cells = 80;
    player.weaponIndex = 0;
    player.fireCooldown = 0;
    player.dashCooldown = 0;
    player.invulnTimer = 0;

    loadStratum(0);

    document.getElementById('menu-overlay').classList.add('hidden');
    document.getElementById('state-modal').classList.add('hidden');
    gameState.running = true;
    gameState.paused = false;
    sound.startMusic();
    if (canvas.requestPointerLock) canvas.requestPointerLock();
  }

  function loadStratum(index) {
    const stratum = STRATA[index % STRATA.length];
    gameState.stratumIndex = index;
    // Clone map so beacon status works cleanly
    gameState.currentMap = stratum.map.map(row => [...row]);
    player.x = stratum.playerStart.x;
    player.y = stratum.playerStart.y;
    player.angle = stratum.playerStart.angle;

    gameState.enemies = [];
    gameState.projectiles = [];
    gameState.pickups = [];
    gameState.beaconUnlocked = false;

    if (gameState.mode === 'endless') {
      spawnEndlessWave(gameState.wave);
    } else {
      stratum.spawns.forEach(s => {
        spawnEnemy(s.type, s.x, s.y);
      });
      stratum.pickups.forEach(p => {
        gameState.pickups.push({ kind: p.kind, x: p.x, y: p.y, collected: false });
      });
    }

    gameState.totalStratumEnemies = gameState.enemies.length;
    addKillfeed(`ENTERED ${stratum.name}`);
    updateHUD();
  }

  function spawnEnemy(typeKey, x, y) {
    const arch = ENEMY_TYPES[typeKey];
    const scaledHp = Math.round(arch.hp * difficultyMult * (gameState.mode === 'endless' ? 1 + (gameState.wave - 1) * 0.12 : 1));
    gameState.enemies.push({
      ...arch,
      x,
      y,
      hp: scaledHp,
      maxHp: scaledHp,
      cooldown: Math.random() * 1.1,
      hitFlash: 0,
      strafeDir: Math.random() < 0.5 ? 1 : -1,
      summonedMinions: false,
      dead: false
    });
  }

  function spawnEndlessWave(waveNum) {
    const arenaSpots = [
      [6.5, 2.5], [10.5, 2.5], [13.5, 3.5], [7.5, 7.5],
      [8.5, 8.5], [2.5, 10.5], [13.5, 10.5], [5.5, 13.5],
      [10.5, 13.5], [12.5, 12.5], [3.5, 5.5], [12.5, 5.5]
    ];
    const count = Math.min(arenaSpots.length, 5 + waveNum * 2);
    for (let i = 0; i < count; i++) {
      const [sx, sy] = arenaSpots[i % arenaSpots.length];
      let pool = ['wretch', 'imp'];
      if (waveNum >= 2) pool.push('gorger', 'cantor');
      const pick = (waveNum % 4 === 0 && i === 0) ? 'boss' : pool[i % pool.length];
      spawnEnemy(pick, sx, sy);
    }
    // Refresh pickups each wave
    STRATA[1].pickups.forEach(p => {
      gameState.pickups.push({ kind: p.kind, x: p.x, y: p.y, collected: false });
    });
    gameState.totalStratumEnemies = gameState.enemies.length;
    addKillfeed(`ABYSSAL HORDE WAVE ${waveNum} MANIFESTED!`);
  }

  // =========================================================================
  // 8. PLAYER ABILITIES: PHASE-DASH & ASCENSION OVERDRIVE
  // =========================================================================
  function triggerDash() {
    if (player.dashCooldown > 0 || gameState.paused) return;
    let dx = 0;
    let dy = 0;
    const cos = Math.cos(player.angle);
    const sin = Math.sin(player.angle);
    if (keys['KeyW'] || keys['ArrowUp']) { dx += cos; dy += sin; }
    if (keys['KeyS'] || keys['ArrowDown']) { dx -= cos; dy -= sin; }
    if (keys['KeyA']) { dx += sin; dy -= cos; }
    if (keys['KeyD']) { dx -= sin; dy += cos; }
    if (dx === 0 && dy === 0) {
      dx = cos;
      dy = sin;
    }
    const len = Math.hypot(dx, dy) || 1;
    player.dashVx = (dx / len) * 11.5;
    player.dashVy = (dy / len) * 11.5;
    player.dashTimer = 0.18;
    player.invulnTimer = 0.26;
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

  // =========================================================================
  // 9. WEAPON FIRING & BALLISTICS
  // =========================================================================
  function attemptFire() {
    if (player.fireCooldown > 0 || !gameState.running || gameState.paused) return;
    const weapon = WEAPONS[player.weaponIndex];

    // Check ammunition rule
    if (weapon.ammoType === 'shells') {
      if (player.shells < weapon.cost) {
        addKillfeed('OUT OF SLAG SHELLS! SWITCHING TO CINDER-LANCE');
        selectWeapon(0);
        return;
      }
      player.shells -= weapon.cost;
    } else if (weapon.ammoType === 'cells') {
      if (player.cells < weapon.cost) {
        addKillfeed('INSUFFICIENT PLASMA CELLS!');
        selectWeapon(0);
        return;
      }
      player.cells -= weapon.cost;
    }

    const rofFactor = gameState.overdriveActive ? 0.52 : 1.0;
    player.fireCooldown = weapon.cooldown * rofFactor;
    gameState.muzzleFlash = 0.1;
    gameState.weaponKick = 1.0;
    gameState.screenShake = weapon.isBFG ? 0.35 : 0.12;
    sound.playShoot(weapon.id);

    const dmgMult = gameState.overdriveActive ? 2.0 : 1.0;

    if (weapon.isBFG) {
      // Spawn BFG Singularity projectile that damages all enemies in corridor + AoE on impact
      gameState.projectiles.push({
        x: player.x + Math.cos(player.angle) * 0.4,
        y: player.y + Math.sin(player.angle) * 0.4,
        vx: Math.cos(player.angle) * 8.5,
        vy: Math.sin(player.angle) * 8.5,
        damage: weapon.damage * dmgMult,
        color: '#b85cff',
        fromPlayer: true,
        isBFG: true,
        radius: 0.38
      });
    } else {
      // Hitscan ray pellets
      let hitAny = false;
      for (let p = 0; p < weapon.pellets; p++) {
        const rayAngle = player.angle + (Math.random() - 0.5) * weapon.spread;
        const hitEnemy = castHitscanRay(rayAngle);
        if (hitEnemy) {
          hitAny = true;
          damageEnemy(hitEnemy, weapon.damage * dmgMult, weapon.resonanceGain / weapon.pellets);
        }
      }
      if (hitAny) showHitmarker();
    }

    updateHUD();
  }

  function castHitscanRay(rayAngle) {
    const wallDist = castSingleWallDist(player.x, player.y, rayAngle);
    let bestEnemy = null;
    let bestDist = wallDist;

    for (const e of gameState.enemies) {
      if (e.dead) continue;
      const dx = e.x - player.x;
      const dy = e.y - player.y;
      const dist = Math.hypot(dx, dy);
      if (dist >= bestDist) continue;
      const angleToEnemy = Math.atan2(dy, dx);
      let diff = normalizeAngle(angleToEnemy - rayAngle);
      const angularHalfWidth = Math.atan2(0.42 * e.scale, dist);
      if (Math.abs(diff) <= angularHalfWidth) {
        bestDist = dist;
        bestEnemy = e;
      }
    }
    return bestEnemy;
  }

  function damageEnemy(enemy, amount, resonanceBonus = 5) {
    if (enemy.dead) return;
    enemy.hp -= amount;
    enemy.hitFlash = 0.16;

    // Charge Ascension Resonance
    if (!gameState.overdriveActive) {
      gameState.ascensionGauge = Math.min(100, gameState.ascensionGauge + resonanceBonus);
    }

    // Boss 50% HP phase summon
    if (enemy.isBoss && !enemy.summonedMinions && enemy.hp <= enemy.maxHp * 0.5 && enemy.hp > 0) {
      enemy.summonedMinions = true;
      spawnEnemy('wretch', 5.5, 5.5);
      spawnEnemy('imp', 11.5, 5.5);
      gameState.totalStratumEnemies += 2;
      addKillfeed('☠ ARCHON MALAKOR ENRAGED — REINFORCEMENTS SUMMONED!');
    }

    if (enemy.hp <= 0) {
      enemy.hp = 0;
      enemy.dead = true;
      gameState.kills++;
      gameState.comboTimer = 4.5;
      gameState.multiplier = Math.min(5.0, +(gameState.multiplier + 0.5).toFixed(1));
      if (gameState.multiplier > gameState.peakMultiplier) {
        gameState.peakMultiplier = gameState.multiplier;
      }

      const pts = Math.round(enemy.score * gameState.multiplier);
      gameState.score += pts;

      if (!gameState.overdriveActive) {
        gameState.ascensionGauge = Math.min(100, gameState.ascensionGauge + 14);
      }

      if (gameState.mode === 'blitz') {
        gameState.blitzTimer += 2.5;
        addKillfeed(`PURGED ${enemy.name} (+${pts} // +2.5s CHRONO)`);
      } else {
        addKillfeed(`PURGED ${enemy.name} (+${pts} PTS)`);
      }

      // Chance to drop supply crystal
      if (Math.random() < 0.42) {
        const dropTypes = ['hp', 'shells', 'cells', 'armor'];
        const kind = dropTypes[Math.floor(Math.random() * dropTypes.length)];
        gameState.pickups.push({ kind, x: enemy.x, y: enemy.y, collected: false });
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
        }, 1200);
      } else {
        gameState.beaconUnlocked = true;
        addKillfeed('★ STRATUM PURGED! ENTER THE GOLDEN ASCENSION BEACON!');
      }
    }
    updateHUD();
  }

  // =========================================================================
  // 10. SIMULATION UPDATE LOOP (MOVEMENT, AI, PROJECTILES, PICKUPS)
  // =========================================================================
  function isWall(x, y) {
    const gx = Math.floor(x);
    const gy = Math.floor(y);
    if (gy < 0 || gy >= 16 || gx < 0 || gx >= 16) return true;
    const cell = gameState.currentMap[gy][gx];
    // Cell 4 is the Ascension Beacon — walkable when unlocked!
    if (cell === 4 && gameState.beaconUnlocked) return false;
    return cell > 0;
  }

  function tryMoveEntity(entity, dx, dy, radius = 0.28) {
    const nx = entity.x + dx;
    const ny = entity.y + dy;
    if (!isWall(nx + Math.sign(dx) * radius, entity.y)) {
      entity.x = nx;
    }
    if (!isWall(entity.x, ny + Math.sign(dy) * radius)) {
      entity.y = ny;
    }
  }

  function updateSimulation(dt) {
    if (!gameState.running || gameState.paused) return;

    // Timers
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
    if (player.dashCooldown > 0) player.dashCooldown = Math.max(0, player.dashCooldown - dt);
    if (player.invulnTimer > 0) player.invulnTimer = Math.max(0, player.invulnTimer - dt);
    if (gameState.muzzleFlash > 0) gameState.muzzleFlash = Math.max(0, gameState.muzzleFlash - dt);
    if (gameState.weaponKick > 0) gameState.weaponKick = Math.max(0, gameState.weaponKick - dt * 5.5);
    if (gameState.screenShake > 0) gameState.screenShake = Math.max(0, gameState.screenShake - dt * 2.2);

    // Automatic weapon firing
    if (mouseFiring || keys['Space']) {
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

    // Combo multiplier decay rule (4.5s window)
    if (gameState.comboTimer > 0) {
      gameState.comboTimer -= dt;
      if (gameState.comboTimer <= 0) {
        gameState.multiplier = 1.0;
      }
    }

    // Player rotation via keyboard arrows
    const rotSpeed = 2.4;
    if (keys['ArrowLeft']) player.angle -= rotSpeed * dt;
    if (keys['ArrowRight']) player.angle += rotSpeed * dt;

    // Player movement or Phase-Dash
    if (player.dashTimer > 0) {
      player.dashTimer -= dt;
      tryMoveEntity(player, player.dashVx * dt, player.dashVy * dt, 0.3);
    } else {
      const baseSpeed = gameState.overdriveActive ? 4.6 : 3.65;
      let moveX = 0;
      let moveY = 0;
      const cos = Math.cos(player.angle);
      const sin = Math.sin(player.angle);

      if (keys['KeyW'] || keys['ArrowUp']) { moveX += cos; moveY += sin; }
      if (keys['KeyS'] || keys['ArrowDown']) { moveX -= cos; moveY -= sin; }
      if (keys['KeyA']) { moveX += sin; moveY -= cos; }
      if (keys['KeyD']) { moveX -= sin; moveY += cos; }

      const len = Math.hypot(moveX, moveY);
      if (len > 0) {
        moveX = (moveX / len) * baseSpeed * dt;
        moveY = (moveY / len) * baseSpeed * dt;
        tryMoveEntity(player, moveX, moveY, 0.3);
        gameState.bobPhase += dt * 10.5;
      }
    }

    // Check if player stepped into unlocked Ascension Beacon (tile 4)
    if (gameState.beaconUnlocked) {
      const gx = Math.floor(player.x);
      const gy = Math.floor(player.y);
      if (gameState.currentMap[gy] && gameState.currentMap[gy][gx] === 4) {
        handleBeaconEntered();
        return;
      }
    }

    // Pickups collection
    for (const p of gameState.pickups) {
      if (p.collected) continue;
      if (Math.hypot(player.x - p.x, player.y - p.y) < 0.65) {
        collectPickup(p);
      }
    }

    // Enemy AI Update (dilated by 45% during Chrono-Overdrive)
    const enemyTimeScale = gameState.overdriveActive ? 0.55 : 1.0;
    const edt = dt * enemyTimeScale;

    for (const e of gameState.enemies) {
      if (e.dead) continue;
      if (e.hitFlash > 0) e.hitFlash = Math.max(0, e.hitFlash - dt);
      if (e.cooldown > 0) e.cooldown -= edt;

      const dx = player.x - e.x;
      const dy = player.y - e.y;
      const dist = Math.hypot(dx, dy);
      const dirX = dx / (dist || 1);
      const dirY = dy / (dist || 1);

      if (!e.ranged) {
        // Melee rusher (Ash-Wretch)
        if (dist > 0.9) {
          tryMoveEntity(e, dirX * e.speed * edt, dirY * e.speed * edt, 0.3);
        }
        if (dist <= e.attackRange && e.cooldown <= 0) {
          e.cooldown = e.attackCooldown;
          applyDamageToPlayer(e.damage, e.name);
        }
      } else {
        // Ranged skirmisher / Tank / Boss
        const hasLOS = checkLineOfSight(e.x, e.y, player.x, player.y);
        if (dist > 4.2 || !hasLOS) {
          tryMoveEntity(e, dirX * e.speed * edt, dirY * e.speed * edt, 0.32);
        } else {
          // Strafe laterally
          const perpX = -dirY * e.strafeDir;
          const perpY = dirX * e.strafeDir;
          tryMoveEntity(e, perpX * e.speed * 0.75 * edt, perpY * e.speed * 0.75 * edt, 0.32);
          if (Math.random() < 0.02) e.strafeDir *= -1;
        }

        if (hasLOS && dist <= e.attackRange && e.cooldown <= 0) {
          e.cooldown = e.attackCooldown;
          const baseAng = Math.atan2(dy, dx);

          if (e.isBoss) {
            // Archon Malakor fires a 3-way fan barrage
            [-0.18, 0, 0.18].forEach(offset => {
              spawnEnemyProjectile(e, baseAng + offset);
            });
          } else {
            spawnEnemyProjectile(e, baseAng);
          }

          // Void-Cantor blink-step after firing
          if (e.canBlink && Math.random() < 0.55) {
            const blinkSign = Math.random() < 0.5 ? 1 : -1;
            tryMoveEntity(e, -dirY * blinkSign * 1.1, dirX * blinkSign * 1.1, 0.32);
          }
        }
      }
    }

    // Update Projectiles
    for (let i = gameState.projectiles.length - 1; i >= 0; i--) {
      const pr = gameState.projectiles[i];
      const pScale = pr.fromPlayer ? 1.0 : enemyTimeScale;
      pr.x += pr.vx * dt * pScale;
      pr.y += pr.vy * dt * pScale;

      if (isWall(pr.x, pr.y)) {
        if (pr.isBFG) detonateBFG(pr.x, pr.y, pr.damage);
        gameState.projectiles.splice(i, 1);
        continue;
      }

      if (pr.fromPlayer) {
        // Check hit against enemies
        for (const e of gameState.enemies) {
          if (e.dead) continue;
          if (Math.hypot(e.x - pr.x, e.y - pr.y) < 0.65) {
            if (pr.isBFG) {
              detonateBFG(pr.x, pr.y, pr.damage);
            } else {
              damageEnemy(e, pr.damage, 8);
            }
            gameState.projectiles.splice(i, 1);
            break;
          }
        }
      } else {
        // Enemy projectile hitting player
        if (Math.hypot(player.x - pr.x, player.y - pr.y) < 0.42) {
          applyDamageToPlayer(pr.damage, pr.sourceName);
          gameState.projectiles.splice(i, 1);
        }
      }
    }

    updateHUD();
  }

  function spawnEnemyProjectile(enemy, angle) {
    gameState.projectiles.push({
      x: enemy.x + Math.cos(angle) * 0.4,
      y: enemy.y + Math.sin(angle) * 0.4,
      vx: Math.cos(angle) * enemy.projSpeed,
      vy: Math.sin(angle) * enemy.projSpeed,
      damage: Math.round(enemy.damage * difficultyMult),
      color: enemy.projColor,
      fromPlayer: false,
      sourceName: enemy.name
    });
  }

  function detonateBFG(bx, by, baseDmg) {
    sound.playOverdrive();
    gameState.screenShake = 0.45;
    flashScreen('fx-overdrive', 0.25);
    for (const e of gameState.enemies) {
      if (e.dead) continue;
      const d = Math.hypot(e.x - bx, e.y - by);
      if (d < 5.5) {
        const falloff = Math.max(0.35, 1 - d / 6.0);
        damageEnemy(e, Math.round(baseDmg * falloff), 12);
      }
    }
  }

  function applyDamageToPlayer(rawDmg, attackerName) {
    if (player.invulnTimer > 0) return;

    // Rule 1: Aegis Shield Partitioning (65% absorbed by Aegis Armor)
    let hpLoss = rawDmg;
    if (player.armor > 0) {
      const absorbed = Math.min(player.armor, Math.ceil(rawDmg * 0.65));
      player.armor -= absorbed;
      hpLoss = Math.max(1, rawDmg - absorbed);
    }

    player.hp = Math.max(0, player.hp - hpLoss);
    gameState.screenShake = 0.25;
    sound.playPlayerDamage();
    flashScreen('fx-damage', 0.28);

    if (player.hp <= 0) {
      triggerGameOver(`VESSEL SHATTERED BY ${attackerName}`);
    }
  }

  function collectPickup(p) {
    p.collected = true;
    sound.playPickup();
    flashScreen('fx-pickup', 0.18);

    if (p.kind === 'hp') {
      player.hp = Math.min(player.maxHp, player.hp + 30);
      addKillfeed('HARVESTED VITALITY ICHOR (+30 HP)');
    } else if (p.kind === 'armor') {
      player.armor = Math.min(player.maxArmor, player.armor + 35);
      addKillfeed('EQUIPPED AEGIS SHARD (+35 ARMOR)');
    } else if (p.kind === 'shells') {
      player.shells = Math.min(player.maxShells, player.shells + 12);
      addKillfeed('COLLECTED SLAG SHELLS (+12)');
    } else if (p.kind === 'cells') {
      player.cells = Math.min(player.maxCells, player.cells + 35);
      addKillfeed('COLLECTED PLASMA CELLS (+35)');
    }
  }

  // =========================================================================
  // 11. 3D RAYCASTING & SPRITE RENDERING ENGINE
  // =========================================================================
  function castSingleWallDist(ox, oy, angle) {
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    let dist = 0;
    const step = 0.04;
    while (dist < 22) {
      dist += step;
      if (isWall(ox + cos * dist, oy + sin * dist)) {
        return dist;
      }
    }
    return 22;
  }

  function checkLineOfSight(x1, y1, x2, y2) {
    const dx = x2 - x1;
    const dy = y2 - y1;
    const dist = Math.hypot(dx, dy);
    const steps = Math.ceil(dist / 0.15);
    for (let i = 1; i < steps; i++) {
      const t = i / steps;
      if (isWall(x1 + dx * t, y1 + dy * t)) return false;
    }
    return true;
  }

  function renderFrame() {
    const stratum = STRATA[gameState.stratumIndex % STRATA.length];
    const shakeX = (Math.random() - 0.5) * gameState.screenShake * 18;
    const shakeY = (Math.random() - 0.5) * gameState.screenShake * 18;
    const bobOffset = Math.sin(gameState.bobPhase) * 6;

    ctx.save();
    ctx.translate(shakeX, shakeY);

    // 1. Ceiling & Floor Gradients
    const horizon = H / 2 + bobOffset;
    const ceilGrad = ctx.createLinearGradient(0, 0, 0, horizon);
    ceilGrad.addColorStop(0, stratum.ceilColor);
    ceilGrad.addColorStop(1, '#040307');
    ctx.fillStyle = ceilGrad;
    ctx.fillRect(0, 0, W, horizon);

    const floorGrad = ctx.createLinearGradient(0, horizon, 0, H);
    floorGrad.addColorStop(0, '#050408');
    floorGrad.addColorStop(1, stratum.floorColor);
    ctx.fillStyle = floorGrad;
    ctx.fillRect(0, horizon, W, H - horizon);

    // 2. DDA Raycasting for Textured 3D Walls
    const startAngle = player.angle - FOV / 2;
    for (let ray = 0; ray < NUM_RAYS; ray++) {
      const rayAngle = startAngle + (ray / NUM_RAYS) * FOV;
      const hit = castWallDDA(player.x, player.y, rayAngle);
      const correctedDist = Math.max(0.1, hit.dist * Math.cos(rayAngle - player.angle));
      zBuffer[ray] = correctedDist;

      const wallHeight = Math.min(H * 2.5, (H * 0.92) / correctedDist);
      const drawY = horizon - wallHeight / 2;
      const drawX = ray * STRIP_W;

      const tex = wallTextures[hit.wallType] || wallTextures[1];
      const texX = Math.floor(hit.texU * TEX_SIZE) % TEX_SIZE;
      ctx.drawImage(tex, texX, 0, 1, TEX_SIZE, drawX, drawY, STRIP_W + 0.6, wallHeight);

      // Depth fog + side shading
      const shade = Math.min(0.88, correctedDist / 13.5 + (hit.side === 1 ? 0.16 : 0));
      if (hit.wallType === 4 && gameState.beaconUnlocked) {
        // Golden pulsing aura on unlocked beacon
        const pulse = 0.25 + 0.2 * Math.sin(performance.now() * 0.008);
        ctx.fillStyle = `rgba(255, 209, 92, ${pulse})`;
        ctx.fillRect(drawX, drawY, STRIP_W + 0.6, wallHeight);
      } else {
        ctx.fillStyle = `rgba(5, 4, 9, ${shade})`;
        ctx.fillRect(drawX, drawY, STRIP_W + 0.6, wallHeight);
      }
    }

    // 3. Collect & Depth-Sort 3D Sprites (Enemies, Projectiles, Pickups)
    const sprites = [];

    for (const p of gameState.pickups) {
      if (!p.collected) {
        sprites.push({ kind: 'pickup', data: p, dist: Math.hypot(p.x - player.x, p.y - player.y) });
      }
    }
    for (const pr of gameState.projectiles) {
      sprites.push({ kind: 'projectile', data: pr, dist: Math.hypot(pr.x - player.x, pr.y - player.y) });
    }
    for (const e of gameState.enemies) {
      if (!e.dead) {
        sprites.push({ kind: 'enemy', data: e, dist: Math.hypot(e.x - player.x, e.y - player.y) });
      }
    }

    sprites.sort((a, b) => b.dist - a.dist);

    for (const sp of sprites) {
      renderSprite3D(sp, horizon);
    }

    // 4. First-Person Weapon Viewmodel & Muzzle Flash
    renderWeaponViewmodel();

    ctx.restore();

    // 5. Render 2D Tactical Radar Minimap
    renderMinimap();
  }

  function castWallDDA(ox, oy, angle) {
    const rayDirX = Math.cos(angle);
    const rayDirY = Math.sin(angle);

    let mapX = Math.floor(ox);
    let mapY = Math.floor(oy);

    const deltaDistX = Math.abs(1 / (rayDirX || 1e-6));
    const deltaDistY = Math.abs(1 / (rayDirY || 1e-6));

    let stepX, sideDistX, stepY, sideDistY;
    if (rayDirX < 0) {
      stepX = -1;
      sideDistX = (ox - mapX) * deltaDistX;
    } else {
      stepX = 1;
      sideDistX = (mapX + 1.0 - ox) * deltaDistX;
    }
    if (rayDirY < 0) {
      stepY = -1;
      sideDistY = (oy - mapY) * deltaDistY;
    } else {
      stepY = 1;
      sideDistY = (mapY + 1.0 - oy) * deltaDistY;
    }

    let hit = 0;
    let side = 0;
    let wallType = 1;
    let steps = 0;

    while (hit === 0 && steps < 48) {
      steps++;
      if (sideDistX < sideDistY) {
        sideDistX += deltaDistX;
        mapX += stepX;
        side = 0;
      } else {
        sideDistY += deltaDistY;
        mapY += stepY;
        side = 1;
      }
      if (mapY < 0 || mapY >= 16 || mapX < 0 || mapX >= 16) {
        hit = 1;
        wallType = 1;
      } else if (gameState.currentMap[mapY][mapX] > 0) {
        hit = 1;
        wallType = gameState.currentMap[mapY][mapX];
      }
    }

    const perpWallDist = side === 0
      ? (mapX - ox + (1 - stepX) / 2) / (rayDirX || 1e-6)
      : (mapY - oy + (1 - stepY) / 2) / (rayDirY || 1e-6);

    let wallX = side === 0 ? oy + perpWallDist * rayDirY : ox + perpWallDist * rayDirX;
    wallX -= Math.floor(wallX);

    return { dist: Math.max(0.05, perpWallDist), side, wallType, texU: wallX };
  }

  function renderSprite3D(sp, horizon) {
    const obj = sp.data;
    const dx = obj.x - player.x;
    const dy = obj.y - player.y;
    const dist = Math.hypot(dx, dy);
    if (dist < 0.25) return;

    const spriteAngle = normalizeAngle(Math.atan2(dy, dx) - player.angle);
    if (Math.abs(spriteAngle) > FOV * 0.75) return;

    const screenX = (0.5 + spriteAngle / FOV) * W;
    const centerRay = Math.floor((screenX / W) * NUM_RAYS);
    if (centerRay < 0 || centerRay >= NUM_RAYS) return;
    if (zBuffer[centerRay] < dist - 0.15) return; // Occluded by wall

    const baseSize = (H * 0.85) / dist;

    if (sp.kind === 'pickup') {
      const sz = baseSize * 0.34;
      const py = horizon + baseSize * 0.22 + Math.sin(performance.now() * 0.006 + obj.x) * 5;
      const colors = { hp: '#28f088', armor: '#24d8ff', shells: '#ff8c24', cells: '#b85cff' };
      ctx.fillStyle = colors[obj.kind] || '#ffd15c';
      ctx.beginPath();
      ctx.arc(screenX, py, sz * 0.45, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 2;
      ctx.stroke();
    } else if (sp.kind === 'projectile') {
      const sz = baseSize * (obj.isBFG ? 0.42 : 0.22);
      ctx.fillStyle = obj.color || '#ff6b1a';
      ctx.beginPath();
      ctx.arc(screenX, horizon, sz * 0.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(screenX, horizon, sz * 0.22, 0, Math.PI * 2);
      ctx.fill();
    } else if (sp.kind === 'enemy') {
      const e = obj;
      const w = baseSize * 0.72 * e.scale;
      const h = baseSize * 0.96 * e.scale;
      const left = screenX - w / 2;
      const top = horizon - h * 0.48;

      // Check per-column visibility with zBuffer
      const startCol = Math.max(0, Math.floor(left / STRIP_W));
      const endCol = Math.min(NUM_RAYS - 1, Math.floor((left + w) / STRIP_W));
      let visibleCols = 0;
      for (let c = startCol; c <= endCol; c++) {
        if (dist < zBuffer[c]) visibleCols++;
      }
      if (visibleCols === 0) return;

      // Draw stylized procedural demon silhouette
      const flash = e.hitFlash > 0;
      ctx.save();
      // Shadow
      ctx.fillStyle = 'rgba(0, 0, 0, 0.45)';
      ctx.fillRect(left + w * 0.15, top + h * 0.9, w * 0.7, h * 0.1);

      // Torso & Armor
      ctx.fillStyle = flash ? '#ffffff' : e.secondaryColor;
      ctx.fillRect(left + w * 0.18, top + h * 0.24, w * 0.64, h * 0.58);

      ctx.fillStyle = flash ? '#ffd15c' : e.primaryColor;
      ctx.fillRect(left + w * 0.24, top + h * 0.28, w * 0.52, h * 0.42);

      // Horns & Head
      ctx.fillStyle = flash ? '#ffffff' : e.primaryColor;
      ctx.fillRect(left + w * 0.3, top + h * 0.08, w * 0.4, h * 0.2);
      // Left & Right Horns
      ctx.fillRect(left + w * 0.16, top, w * 0.12, h * 0.16);
      ctx.fillRect(left + w * 0.72, top, w * 0.12, h * 0.16);

      // Glowing Eyes
      ctx.fillStyle = e.eyeColor;
      ctx.fillRect(left + w * 0.36, top + h * 0.14, w * 0.09, h * 0.05);
      ctx.fillRect(left + w * 0.55, top + h * 0.14, w * 0.09, h * 0.05);

      // Legs (animated stride)
      const stride = Math.sin(performance.now() * 0.01 + e.x) * (h * 0.04);
      ctx.fillStyle = flash ? '#ffffff' : e.secondaryColor;
      ctx.fillRect(left + w * 0.24, top + h * 0.78 + stride, w * 0.18, h * 0.2);
      ctx.fillRect(left + w * 0.58, top + h * 0.78 - stride, w * 0.18, h * 0.2);

      // Floating Health Bar above damaged enemies
      if (e.hp < e.maxHp) {
        const bw = Math.max(32, w * 0.8);
        const bx = screenX - bw / 2;
        const by = top - 10;
        ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
        ctx.fillRect(bx, by, bw, 5);
        ctx.fillStyle = e.isBoss ? '#b85cff' : '#ff3b22';
        ctx.fillRect(bx, by, bw * (e.hp / e.maxHp), 5);
      }
      ctx.restore();
    }
  }

  function renderWeaponViewmodel() {
    const weapon = WEAPONS[player.weaponIndex];
    const bobX = Math.cos(gameState.bobPhase * 0.5) * 10;
    const bobY = Math.abs(Math.sin(gameState.bobPhase)) * 9;
    const kickY = gameState.weaponKick * 26;

    const baseX = W * 0.5 + bobX;
    const baseY = H - bobY + kickY;

    ctx.save();

    // Muzzle Flash
    if (gameState.muzzleFlash > 0) {
      const grad = ctx.createRadialGradient(baseX, baseY - 125, 6, baseX, baseY - 125, 78);
      grad.addColorStop(0, '#ffffff');
      grad.addColorStop(0.4, weapon.color);
      grad.addColorStop(1, 'rgba(255, 59, 34, 0)');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(baseX, baseY - 125, 78, 0, Math.PI * 2);
      ctx.fill();
    }

    // Overdrive weapon outline glow
    if (gameState.overdriveActive) {
      ctx.shadowColor = '#ffd15c';
      ctx.shadowBlur = 18;
    }

    // Gun Chassis
    ctx.fillStyle = '#1b1724';
    ctx.fillRect(baseX - 38, baseY - 110, 76, 115);

    // Barrel Assembly
    ctx.fillStyle = '#2d2738';
    if (weapon.id === 1) {
      // Twin-bore shotgun barrels
      ctx.fillRect(baseX - 32, baseY - 135, 26, 40);
      ctx.fillRect(baseX + 6, baseY - 135, 26, 40);
    } else if (weapon.id === 3) {
      // Wide BFG Singularity Core
      ctx.fillRect(baseX - 48, baseY - 130, 96, 55);
    } else {
      ctx.fillRect(baseX - 20, baseY - 138, 40, 42);
    }

    // Glowing Energy Core Strip
    ctx.fillStyle = weapon.color;
    ctx.fillRect(baseX - 10, baseY - 105, 20, 68);

    ctx.restore();
  }

  function renderMinimap() {
    const mw = miniCanvas.width;
    const mh = miniCanvas.height;
    const tileW = mw / 16;
    const tileH = mh / 16;

    miniCtx.fillStyle = '#07050a';
    miniCtx.fillRect(0, 0, mw, mh);

    // Draw walls & beacon
    for (let y = 0; y < 16; y++) {
      for (let x = 0; x < 16; x++) {
        const cell = gameState.currentMap[y][x];
        if (cell === 4) {
          miniCtx.fillStyle = gameState.beaconUnlocked ? '#ffd15c' : '#8c3bff';
          miniCtx.fillRect(x * tileW, y * tileH, tileW, tileH);
        } else if (cell > 0) {
          miniCtx.fillStyle = '#2b2238';
          miniCtx.fillRect(x * tileW, y * tileH, tileW - 0.5, tileH - 0.5);
        }
      }
    }

    // Pickups
    for (const p of gameState.pickups) {
      if (p.collected) continue;
      miniCtx.fillStyle = '#24d8ff';
      miniCtx.fillRect(p.x * tileW - 2, p.y * tileH - 2, 4, 4);
    }

    // Enemies
    for (const e of gameState.enemies) {
      if (e.dead) continue;
      miniCtx.fillStyle = e.isBoss ? '#ffd15c' : '#ff3b22';
      miniCtx.beginPath();
      miniCtx.arc(e.x * tileW, e.y * tileH, e.isBoss ? 4.5 : 3, 0, Math.PI * 2);
      miniCtx.fill();
    }

    // Player & View Cone
    const px = player.x * tileW;
    const py = player.y * tileH;
    miniCtx.strokeStyle = 'rgba(40, 240, 136, 0.45)';
    miniCtx.beginPath();
    miniCtx.moveTo(px, py);
    miniCtx.lineTo(px + Math.cos(player.angle - FOV / 2) * 22, py + Math.sin(player.angle - FOV / 2) * 22);
    miniCtx.moveTo(px, py);
    miniCtx.lineTo(px + Math.cos(player.angle + FOV / 2) * 22, py + Math.sin(player.angle + FOV / 2) * 22);
    miniCtx.stroke();

    miniCtx.fillStyle = '#28f088';
    miniCtx.beginPath();
    miniCtx.arc(px, py, 3.5, 0, Math.PI * 2);
    miniCtx.fill();
  }

  // =========================================================================
  // 12. HUD TELEMETRY, KILLFEED & MODAL STATE HANDLERS
  // =========================================================================
  function updateHUD() {
    const stratum = STRATA[gameState.stratumIndex % STRATA.length];
    document.getElementById('hud-mode-badge').textContent = gameState.mode.toUpperCase();
    document.getElementById('hud-stratum-name').textContent =
      gameState.mode === 'endless' ? `CRUCIBLE HORDE // WAVE ${gameState.wave}` : stratum.name;

    const alive = gameState.enemies.filter(e => !e.dead).length;
    const total = Math.max(1, gameState.totalStratumEnemies);
    document.getElementById('hud-enemies-count').textContent = `${alive} / ${total}`;
    document.getElementById('hud-quota-bar').style.width = `${(alive / total) * 100}%`;
    document.getElementById('hud-objective-text').textContent =
      gameState.beaconUnlocked ? 'ENTER GOLDEN ASCENSION BEACON' : 'PURGE HOSTILE GARRISON';

    document.getElementById('interaction-prompt').classList.toggle('hidden', !gameState.beaconUnlocked);

    // Timer
    if (gameState.mode === 'blitz') {
      document.getElementById('hud-clock-label').textContent = 'DOOMSDAY CLOCK';
      document.getElementById('hud-timer').textContent = formatTime(gameState.blitzTimer);
    } else {
      document.getElementById('hud-clock-label').textContent = 'ASCENSION TIME';
      document.getElementById('hud-timer').textContent = formatTime(gameState.elapsedTime);
    }

    // Boss HP Bar
    const boss = gameState.enemies.find(e => e.isBoss && !e.dead);
    const bossContainer = document.getElementById('boss-bar-container');
    if (boss) {
      bossContainer.classList.remove('hidden');
      document.getElementById('boss-hp-text').textContent = `${boss.hp} / ${boss.maxHp}`;
      document.getElementById('boss-hp-bar').style.width = `${(boss.hp / boss.maxHp) * 100}%`;
    } else {
      bossContainer.classList.add('hidden');
    }

    // Score & Multiplier
    document.getElementById('hud-score').textContent = String(gameState.score).padStart(6, '0');
    document.getElementById('hud-multiplier').textContent = `${gameState.multiplier.toFixed(1)}×`;
    document.getElementById('hud-mult-bar').style.width = `${(gameState.comboTimer / 4.5) * 100}%`;

    // Vitals & Dash
    document.getElementById('hud-hp').textContent = player.hp;
    document.getElementById('hud-hp-bar').style.width = `${player.hp}%`;
    document.getElementById('hud-armor').textContent = player.armor;
    document.getElementById('hud-armor-bar').style.width = `${player.armor}%`;

    const dashPct = player.dashCooldown <= 0 ? 100 : Math.round((1 - player.dashCooldown / 1.4) * 100);
    document.getElementById('hud-dash-bar').style.width = `${dashPct}%`;
    document.getElementById('hud-dash-state').textContent = player.dashCooldown <= 0 ? 'READY' : 'RECHARGING';

    // Ascension Resonance Core
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

    // Active Weapon & Slots
    const w = WEAPONS[player.weaponIndex];
    document.getElementById('hud-weapon-name').textContent = w.name;
    document.getElementById('hud-ammo-type').textContent = w.ammoLabel;
    if (w.ammoType === 'infinite') {
      document.getElementById('hud-ammo-current').textContent = '∞';
    } else if (w.ammoType === 'shells') {
      document.getElementById('hud-ammo-current').textContent = player.shells;
    } else {
      document.getElementById('hud-ammo-current').textContent = player.cells;
    }

    document.getElementById('slot-ammo-1').textContent = player.shells;
    document.getElementById('slot-ammo-2').textContent = player.cells;
    document.getElementById('slot-ammo-3').textContent = player.cells;

    document.querySelectorAll('.w-slot').forEach((btn, idx) => {
      btn.classList.toggle('active', idx === player.weaponIndex);
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
    if (document.exitPointerLock) document.exitPointerLock();

    if (gameState.stratumIndex < STRATA.length - 1) {
      showStateModal({
        badge: `STRATUM ${gameState.stratumIndex + 1} COMPLETE`,
        title: 'ASCENSION BEACON BREACHED',
        subtitle: `Tectonic seals broken. Prepare to ascend to ${STRATA[gameState.stratumIndex + 1].name}.`,
        primaryLabel: 'ASCEND TO NEXT STRATUM ▶',
        onPrimary: () => {
          document.getElementById('state-modal').classList.add('hidden');
          player.hp = Math.min(player.maxHp, player.hp + 25);
          loadStratum(gameState.stratumIndex + 1);
          gameState.paused = false;
          if (canvas.requestPointerLock) canvas.requestPointerLock();
        }
      });
    } else {
      // Final Victory!
      gameState.score += 2500;
      showStateModal({
        badge: 'TOTAL ASCENSION ACHIEVED // VICTORY',
        title: 'THE EMPYREAN BREACH IS SEALED',
        subtitle: 'Archon Malakor has fallen. You have conquered all three strata of the Abyss!',
        primaryLabel: 'PLAY AGAIN',
        onPrimary: () => startNewGame()
      });
    }
  }

  function triggerGameOver(reason) {
    gameState.paused = true;
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
        player.shells = Math.max(20, player.shells);
        player.cells = Math.max(60, player.cells);
        loadStratum(gameState.stratumIndex);
        gameState.paused = false;
        sound.startMusic();
        if (canvas.requestPointerLock) canvas.requestPointerLock();
      }
    });
  }

  function pauseGame() {
    if (!gameState.running || gameState.paused) return;
    gameState.paused = true;
    if (document.exitPointerLock) document.exitPointerLock();
    showStateModal({
      badge: 'SIMULATION PAUSED',
      title: 'TACTICAL COMMAND STANDBY',
      subtitle: 'Inspect the 6-Criteria Game Design Rubric above or resume your climb.',
      primaryLabel: 'RESUME ASCENSION ▶',
      onPrimary: () => {
        document.getElementById('state-modal').classList.add('hidden');
        gameState.paused = false;
        if (canvas.requestPointerLock) canvas.requestPointerLock();
      }
    });
  }

  function togglePauseMenu() {
    if (gameState.paused) {
      document.getElementById('state-modal').classList.add('hidden');
      gameState.paused = false;
      if (canvas.requestPointerLock) canvas.requestPointerLock();
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

  // =========================================================================
  // 13. HELPERS & MAIN ANIMATION LOOP
  // =========================================================================
  function normalizeAngle(a) {
    while (a < -Math.PI) a += Math.PI * 2;
    while (a > Math.PI) a -= Math.PI * 2;
    return a;
  }

  function formatTime(sec) {
    const m = Math.floor(sec / 60);
    const s = (sec % 60).toFixed(1).padStart(4, '0');
    return `${String(m).padStart(2, '0')}:${s}`;
  }

  let lastFrameTime = performance.now();
  function mainLoop(now) {
    const dt = Math.min(0.05, (now - lastFrameTime) / 1000);
    lastFrameTime = now;

    if (gameState.running && !gameState.paused) {
      updateSimulation(dt);
    }
    renderFrame();
    requestAnimationFrame(mainLoop);
  }

  // Initial preview render behind main menu
  loadStratum(0);
  requestAnimationFrame(mainLoop);
})();

