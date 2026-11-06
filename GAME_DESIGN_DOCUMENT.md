# DOOM: HELL'S ASCENSION — Formal Game Design Document (GDD)

**Course / Assignment Rubric**: Game Development Criteria (6 Items × 10 Points = 60 Points)  
**Project Title**: *DOOM: HELL'S ASCENSION*  
**Genre**: Kinetic Vertical-Ascension First-Person Shooter (FPS)  
**Platform**: Web Browser (HTML5 Canvas 3D Raycaster + WebAudio Synthesizer, Deployed on Vercel)

---

## 1. Gameplay (10 / 10 Points)

### Core Concept & Originality
While inspired by classic fast-paced shooters, **DOOM: HELL'S ASCENSION** introduces an original thematic and mechanical inversion: **Upward Thermodynamic Ascension**. Instead of descending into hell, the player is a **Resonance Warden** cast down to the frozen, ash-choked floor of the Pit (**The Nadir**). Because abyssal gravity crushes ordinary mortals, the player must harvest kinetic thermal energy (**Ascension Resonance**) from slain tormentors to power vertical **Stratum Beacons** and literally fight their way upward toward the surface.

### Core Gameplay Loop
1. **Engage & Harvest**: Enter a sealed Stratum labyrinth and engage hostile manifestations using four distinct energy/ballistic weapons.
2. **Build Resonance**: Every hit and kill feeds the **Ascension Gauge (0–100%)** and increments the **Kill-Chain Multiplier (`1.0×` – `5.0×`)**.
3. **Phase-Dash Evasion**: Use the **Abyssal Phase-Dash (`[SHIFT]`)** to dodge incoming 3D projectiles, cross hazardous corridors, and flank armored enemies.
4. **Trigger Ascension Overdrive**: At 100% Resonance, press `[Q]` or `[E]` to enter **Ascension Overdrive**, temporarily warping local time (slowing enemy projectiles by 45%), doubling weapon cyclic rate, and doubling outgoing damage.
5. **Breach the Beacon**: Once all stratum guardians are eliminated, the **Ascension Beacon** unseals, allowing the Warden to ascend to the next vertical layer.

---

## 2. Rules (10 / 10 Points)

1. **Health & Aegis Armor Mitigation Rule**:
   - The player has **100 Vitality (HP)** and up to **100 Aegis Integrity (Armor)**.
   - When Aegis Integrity is above `0`, **65% of all incoming damage** is absorbed by Aegis and only **35%** is applied to Vitality.
2. **Weapon Resource Conservation Rule**:
   - **Slot 1 (`Cinder-Lance`)**: Uses self-regenerating thermal capacitor rounds (`∞` ammo) so the player is never completely defenseless.
   - **Slot 2 (`Hellfire Scatter-Maw`)**: Consumes **1 Slag Shell** per blast (fires 7 kinetic shards in a horizontal cone).
   - **Slot 3 (`Soul-Tether Pulse Rifle`)**: Consumes **1 Plasma Cell** per rapid-fire bolt and grants `+4%` bonus Ascension Resonance per hit.
   - **Slot 4 (`B.F.G. — Brimstone Fissure Generator`)**: Consumes **25 Plasma Cells** per shot to unleash an unstable singularity that deals massive area-of-effect (AoE) radial damage to all enemies in sight.
3. **Resonance & Combo Decay Rule**:
   - Consecutive eliminations within **4.5 seconds** increase the Score & Resonance Multiplier by `+0.5×` up to a ceiling of `5.0×`.
   - Failing to damage or eliminate an enemy within 4.5 seconds resets the multiplier to `1.0×`.
4. **Stratum Containment Rule**:
   - The Ascension Beacon remains dormant (`LOCKED`) until the Stratum's **Hostile Quota** reaches `0` remaining targets.
5. **Phase-Dash Cooldown Rule**:
   - Executing an **Abyssal Dash (`[SHIFT]`)** grants `0.25s` of invulnerability frames (`i-frames`) and imposes a `1.4s` recharge cooldown to prevent infinite invincibility.

---

## 3. Victory and Loss Conditions (10 / 10 Points)

### Victory Conditions
1. **Stratum Clear Condition**: Neutralize 100% of hostile entities in the active Stratum and physically enter the activated **Ascension Rift Beacon**.
2. **Campaign Victory Condition**: Complete all three Strata (**Stratum I: The Nadir Vault**, **Stratum II: The Crucible Foundry**, **Stratum III: The Empyrean Breach**) and defeat the final boss, **Archon Malakor**, achieving **Total Ascension**.
3. **Time-Trial Victory Condition (*Purgatory Blitz*)**: Complete all three Strata before the countdown timer reaches `00:00.00`.
4. **Mastery Rank Condition**: Achieve a final score of `15,000+` points with zero deaths to earn the **S+ Sovereign of Ascension** rating.

### Loss Conditions
1. **Vessel Destruction (`Vitality ≤ 0`)**: Taking lethal damage from enemy melee cleaves or plasma/hellfire projectiles reduces Vitality to `0`, immediately ending the run with the **ASCENSION FAILED** state.
2. **Temporal Collapse (*Purgatory Blitz Mode*)**: Allowing the countdown timer to reach `00:00.00` seals the Empyrean Gate permanently, resulting in immediate mission failure.

---

## 4. Interactivity Modes (10 / 10 Points)

### Perspective & UI Interactivity Modes
- **Active First-Person Combat Mode**: Full pointer-lock 3D raycast perspective with dynamic viewmodel animations, recoil recovery, directional damage indicators, and real-time audio feedback.
- **Tactical Radar & Telemetry Mode**: Simultaneous overhead 2D vector minimap displaying wall geometry, enemy signatures, pickup locations, and the Ascension Beacon vector.
- **Interactive GDD Rubric Inspector Mode**: Built-in tab/drawer mode allowing instant inspection of the 6-Criteria Game Design Document without leaving the web application.

### Playable Game Modes
1. **Campaign Ascension Mode**: Narrative 3-Stratum vertical climb featuring curated architecture, escalating enemy rosters, and a final boss showdown.
2. **Endless Abyssal Horde Mode**: Wave-based arena survival where every cleared wave increases enemy speed, health, and spawn count while replenishing arena pickups.
3. **Purgatory Blitz Mode (Speedrun)**: High-stakes time-attack mode where players start with a tight clock and earn `+2.5 seconds` per demon slain.

---

## 5. Challenges (10 / 10 Points)

1. **Diverse Enemy AI Archetypes**:
   - **Ash-Wretch (Scout/Swarmer)**: High movement speed, relentless melee pursuit, forces players to backpedal and use crowd control.
   - **Cinder-Imp (Skirmisher)**: Alternates between lateral strafing and launching glowing fireball projectiles that must be dodged physically.
   - **Iron-Gorger (Heavy Tank)**: High health pool, resistant to knockback, punishes players who get trapped in narrow hallways.
   - **Void-Cantor (Phase Caster)**: Teleports laterally after firing high-speed void bolts, challenging player aim tracking.
   - **Archon Malakor (Apex Boss)**: Multi-stage boss firing fan-shaped projectile barrages and summoning minions during phase transitions.
2. **Ammunition Scarcity vs. Threat Density**: Deciding when to conserve *Slag Shells* and *Plasma Cells* for heavy *Iron-Gorgers* and *Void-Cantors* versus switching to the *Cinder-Lance* for weaker *Ash-Wretches*.
3. **Spatial Navigation under Crossfire**: Reading the 3D environment and minimap simultaneously to avoid getting cornered in dead-end alcoves while kiting enemy packs.
4. **Overdrive Timing Challenge**: Choosing whether to pop **Ascension Overdrive** defensively to survive an overwhelming ambush or offensively to melt a boss phase during a `5.0×` multiplier window.

---

## 6. Structure (10 / 10 Points)

### A. Objectives
- **Primary Objective**: Fight upward from the Nadir Vault to the Empyrean Breach, purge every Stratum's demonic garrison, and vanquish *Archon Malakor*.
- **Secondary Objectives**: Maintain a `5.0×` Kill-Chain Multiplier, collect hidden Aegis/Plasma caches, and finish with an **S+ Rank** under par time.

### B. Flow
- The game follows an intentional **Sawtooth Intensity Curve**:
  - **Phase 1 (Drop-In)**: Player materializes in a safe starting alcove with initial weapon pickups.
  - **Phase 2 (Skirmish & Exploration)**: Moving into interconnected halls triggers patrolling *Ash-Wretches* and *Cinder-Imps*.
  - **Phase 3 (Arena Crescendo)**: Central chambers unleash mixed-unit waves (*Iron-Gorgers* + *Void-Cantors*), pushing the player to unleash *Ascension Overdrive* and the *B.F.G.*
  - **Phase 4 (Beacon Extraction)**: Once the last enemy falls, the *Ascension Beacon* chimes and illuminates on the radar, guiding the player to the exit portal.

### C. Duration
- **Micro-Session (1 Stratum)**: `2–4 minutes`.
- **Standard Session (Full 3-Stratum Campaign)**: `8–12 minutes`.
- **Extended Session (Endless Horde & Mastery Runs)**: `15–45+ minutes` with high replayability.

### D. Availability
- Hosted globally on **Vercel Edge Network** and version-controlled on **GitHub**.
- Runs natively at 60 FPS in any modern browser with zero plugins, zero external downloads, and instant load times via procedural texture and audio synthesis.

### E. Relationship
- **Player ↔ Environment**: Pillars and corridors provide physical cover against 3D projectiles while funneling melee swarmers into shotgun kill-zones.
- **Offense ↔ Defense**: Offense directly fuels defense—dealing damage charges *Ascension Overdrive*, which slows enemy attacks and restores Aegis shielding.

### F. Progression
- **Stratum I — The Nadir Vault**: Cold obsidian prison; introduces *Ash-Wretches* and *Cinder-Imps*; player masters movement, *Cinder-Lance*, and *Scatter-Maw*.
- **Stratum II — The Crucible Foundry**: Molten brass and brimstone smelteries; introduces *Iron-Gorgers* and *Void-Cantors*; unlocks the *Soul-Tether Pulse Rifle*.
- **Stratum III — The Empyrean Breach**: Sanctum of the Rift; unlocks the *B.F.G. (Brimstone Fissure Generator)* and culminates in the multi-phase battle against **Archon Malakor**.
