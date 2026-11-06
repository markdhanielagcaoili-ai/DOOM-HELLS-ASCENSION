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

      if (weaponId === 0) {
        // Hell-Carbine: punchy suppressed metallic rifle crack
        this.tone(290, 55, 0.13, 'sawtooth', 0.28);
        this.noise(0.09, 0.26);
      } else if (weaponId === 1) {
        // Hellfire Breacher-12: heavy 12-gauge boom
        this.tone(140, 34, 0.3, 'sawtooth', 0.38);
        this.noise(0.26, 0.42);
      } else if (weaponId === 2) {
        // Soul-Shredder LMG: heavy rapid machine gun thump
        this.tone(240, 65, 0.11, 'square', 0.25);
        this.noise(0.08, 0.22);
      } else if (weaponId === 3) {
        // B.F.G. Singularity Launcher
        this.tone(95, 26, 0.65, 'sawtooth', 0.44);
        this.tone(520, 90, 0.55, 'sine', 0.32);
        this.noise(0.5, 0.4);
      }
    }

    playReload() {
      if (!this.enabled) return;
      this.init();
      if (!this.ctx) return;
      this.tone(520, 310, 0.08, 'triangle', 0.16);
      setTimeout(() => this.tone(340, 680, 0.09, 'sawtooth', 0.18), 420);
      setTimeout(() => this.tone(720, 480, 0.07, 'square', 0.15), 850);
    }

    playHit(isKill = false) {
      if (!this.enabled || !this.ctx) return;
      if (isKill) {
        this.tone(220, 58, 0.24, 'sawtooth', 0.26);
        this.tone(620, 920, 0.12, 'triangle', 0.18);
      } else {
        this.tone(820, 440, 0.05, 'triangle', 0.16);
      }
    }

    playPlayerDamage() {
      if (!this.enabled || !this.ctx) return;
      this.tone(125, 42, 0.22, 'sawtooth', 0.32);
      this.noise(0.15, 0.25);
    }

    playPickup() {
      if (!this.enabled || !this.ctx) return;
      this.tone(460, 920, 0.14, 'sine', 0.2);
    }

    playDash() {
      if (!this.enabled || !this.ctx) return;
      this.tone(260, 640, 0.13, 'sine', 0.2);
      this.noise(0.1, 0.14);
    }

    playOverdrive() {
      if (!this.enabled || !this.ctx) return;
      this.tone(200, 880, 0.48, 'sawtooth', 0.32);
      this.tone(330, 660, 0.48, 'triangle', 0.25);
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
      const bufferSize = Math.floor(this.ctx.sampleRate * duration);
      const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i++) {
        data[i] = Math.random() * 2 - 1;
      }
      const src = this.ctx.createBufferSource();
      src.buffer = buffer;
      const filter = this.ctx.createBiquadFilter();
      filter.type = 'lowpass';
      filter.frequency.value = 1250;
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
      const riff = [55, 55, 65.4, 55, 73.4, 55, 61.7, 58.3];
      this.musicTimer = setInterval(() => {
        if (!this.enabled || !gameState.running || gameState.paused) return;
        const freq = riff[this.step % riff.length];
        this.tone(freq, freq * 0.98, 0.16, 'sawtooth', 0.065);
        if (this.step % 4 === 0) {
          this.tone(105, 32, 0.12, 'sine', 0.1);
        }
        if (this.step % 4 === 2) {
          this.noise(0.06, 0.045);
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
  // 3. WEAPONS, ENEMY ARCHETYPES & STRATA THEMES
  // =========================================================================
  const WEAPONS = [
    {
      id: 0,
      name: 'HELL-CARBINE MK-VI',
      ammoType: 'infinite',
      ammoLabel: '[R] RELOAD // 7.62 BRIMSTONE',
      magSize: 30,
      cost: 1,
      damage: 28,
      cooldown: 0.11,
      pellets: 1,
      spread: 0.016,
      adsSpread: 0.003,
      color: 0xff7722,
      auto: true,
      resonanceGain: 5,
      recoilKick: 0.045
    },
    {
      id: 1,
      name: 'HELLFIRE BREACHER-12',
      ammoType: 'shells',
      ammoLabel: '[R] RELOAD // 12G SLAG SHELLS',
      magSize: 8,
      cost: 1,
      damage: 21,
      cooldown: 0.56,
      pellets: 8,
      spread: 0.085,
      adsSpread: 0.045,
      color: 0xff3b22,
      auto: false,
      resonanceGain: 12,
      recoilKick: 0.11
    },
    {
      id: 2,
      name: 'SOUL-SHREDDER LMG',
      ammoType: 'cells',
      ammoLabel: '[R] RELOAD // PLASMA BELT',
      magSize: 60,
      cost: 1,
      damage: 25,
      cooldown: 0.085,
      pellets: 1,
      spread: 0.028,
      adsSpread: 0.008,
      color: 0x24d8ff,
      auto: true,
      resonanceGain: 6,
      recoilKick: 0.055
    },
    {
      id: 3,
      name: 'B.F.G. SINGULARITY',
      ammoType: 'cells',
      ammoLabel: '25 PLASMA CELLS / CORE',
      magSize: 1,
      cost: 25,
      damage: 420,
      cooldown: 1.05,
      pellets: 1,
      spread: 0,
      adsSpread: 0,
      color: 0xb85cff,
      auto: false,
      isBFG: true,
      resonanceGain: 25,
      recoilKick: 0.16
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
    muzzleTimer: 0,
    recoilOffset: 0,
    recoilPitch: 0,
    bobPhase: 0,
    isADS: false,
    reloading: false,
    reloadTimer: 0,
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
    shells: 32,
    maxShells: 64,
    cells: 120,
    maxCells: 240,
    mags: [30, 8, 60, 1],
    weaponIndex: 0,
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
    player.weaponIndex = idx;
    gameState.reloading = false;
    document.getElementById('reload-indicator').classList.add('hidden');
    const w = WEAPONS[idx];
    if (runeStripMat) runeStripMat.color.setHex(w.color);
    if (barrelTwinGroup) barrelTwinGroup.visible = idx === 1;
    if (bfgCoreMesh) bfgCoreMesh.visible = idx === 3;
    updateHUD();
  }

  window.addEventListener('keydown', e => {
    keys[e.code] = true;
    if (!gameState.running) return;

    if (e.code === 'Digit1') selectWeapon(0);
    if (e.code === 'Digit2') selectWeapon(1);
    if (e.code === 'Digit3') selectWeapon(2);
    if (e.code === 'Digit4') selectWeapon(3);
    if (e.code === 'KeyM') sound.toggle();
    if (e.code === 'KeyR') startReload();

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
    player.shells = 32;
    player.cells = 120;
    player.mags = [30, 8, 60, 1];
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
    sound.playReload();
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
          // Headshot bonus check!
          const isHeadshot = hitPoint && hitPoint.y > 1.85 * closestEnemy.scale;
          const finalDmg = weapon.damage * dmgMult * (isHeadshot ? 1.5 : 1.0);
          damageEnemy(closestEnemy, finalDmg, weapon.resonanceGain / weapon.pellets, isHeadshot);
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

      if (!gameState.overdriveActive) {
        gameState.ascensionGauge = Math.min(100, gameState.ascensionGauge + 14);
      }

      if (gameState.mode === 'blitz') {
        gameState.blitzTimer += 2.5;
        addKillfeed(`${isHeadshot ? 'HEADSHOT! ' : ''}PURGED ${enemy.name} (+${pts} // +2.5s)`);
      } else {
        addKillfeed(`${isHeadshot ? 'HEADSHOT! ' : ''}PURGED ${enemy.name} (+${pts} PTS)`);
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
    if (player.dashCooldown > 0) player.dashCooldown = Math.max(0, player.dashCooldown - dt);
    if (player.invulnTimer > 0) player.invulnTimer = Math.max(0, player.invulnTimer - dt);

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

    const targetFov = gameState.isADS ? 46 : 70;
    if (Math.abs(camera.fov - targetFov) > 0.1) {
      camera.fov = THREE.MathUtils.lerp(camera.fov, targetFov, dt * 14);
      camera.updateProjectionMatrix();
    }

    // Update 3D Weapon Viewmodel (Hip-fire <-> ADS interpolation + Reload tilt + Recoil)
    const targetPos = gameState.isADS ? ADS_POS : HIP_POS;
    const targetRot = gameState.isADS ? ADS_ROT : HIP_ROT;
    weaponRig.position.lerp(targetPos, dt * 15);
    weaponRig.rotation.x = THREE.MathUtils.lerp(weaponRig.rotation.x, targetRot.x + gameState.recoilPitch, dt * 15);
    weaponRig.rotation.y = THREE.MathUtils.lerp(weaponRig.rotation.y, targetRot.y, dt * 15);
    weaponRig.rotation.z = THREE.MathUtils.lerp(
      weaponRig.rotation.z,
      targetRot.z + (gameState.reloading ? 0.48 : 0),
      dt * 12
    );

    const swayAmp = gameState.isADS ? 0.002 : 0.011;
    gunGroup.position.set(
      Math.cos(gameState.bobPhase * 0.5) * swayAmp,
      Math.abs(Math.sin(gameState.bobPhase)) * swayAmp - (gameState.reloading ? 0.06 : 0),
      gameState.recoilOffset
    );
    if (boltMesh) {
      boltMesh.position.z = 0.02 + gameState.recoilOffset * 1.4;
    }
    if (magMesh) {
      magMesh.position.y = gameState.reloading
        ? -0.11 - Math.sin((1 - gameState.reloadTimer / 1.05) * Math.PI) * 0.16
        : -0.11;
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
    document.getElementById('hud-multiplier').textContent = `${gameState.multiplier.toFixed(1)}×`;
    document.getElementById('hud-mult-bar').style.width = `${(gameState.comboTimer / 4.5) * 100}%`;

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

    document.querySelectorAll('.w-slot').forEach((btn, sIdx) => {
      btn.classList.toggle('active', sIdx === idx);
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
  // 13. MAIN RENDER & ANIMATION LOOP
  // =========================================================================
  let lastFrameTime = performance.now();
  function mainLoop(now) {
    const dt = Math.min(0.05, (now - lastFrameTime) / 1000);
    lastFrameTime = now;

    updateAtmosphereFX(dt);

    if (gameState.running && !gameState.paused) {
      updateSimulation(dt);
    } else if (!gameState.running) {
      // Dramatic background showcase angle matching the user's reference screenshot!
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
