# DOOM: HELL'S ASCENSION

> **A Kinetic Vertical-Ascension First-Person Shooter (FPS) & Complete 6-Criteria Game Design Project**

---

## Overview

**DOOM: HELL'S ASCENSION** is an original browser-based 3D First-Person Shooter built with a custom raycasting & depth-buffered sprite engine, procedural texture synthesis, and real-time WebAudio industrial sound design.

Unlike traditional corridor shooters where the protagonist descends deeper into the underworld, **DOOM: HELL'S ASCENSION** inverts the paradigm: you awaken chained at the **Nadir Vault** (the lowest frozen-ash floor of the Abyss) as a **Resonance Warden**. Your sole means of escape is **upward kinetic propulsion**—harvesting **Ascension Resonance** from fallen fiends to unlock the **Ascension Spire Beacons**, shift tectonic strata, and fight upward through three escalating layers of the Inferno.

---

## Game Design Criteria (6 Items × 10 Points = 60 Points)

### 1. Gameplay (10 Points)

**DOOM: HELL'S ASCENSION** blends high-velocity first-person marksmanship with an original **Soul-Thermodynamics & Ascension Resonance** combat loop:

- **Push-Forward Resonance Combat**: Enemies do not drop passive health kits from afar. Eliminating abyssal entities at close-to-mid range releases **Soul-Ichor Crystals** and charges the player's **Ascension Gauge (0–100%)**.
- **Abyssal Phase-Dash (`[SHIFT]`)**: Players execute high-speed omnidirectional phase dashes with brief invulnerability frames (`i-frames`) to weave through projectile curtains and reposition across hazard zones.
- **Ascension Overdrive (`[Q]` / `[E]`)**: Once the Ascension Gauge reaches **100%**, activating Overdrive triggers a **Chrono-Thermal Inversion** for 8 seconds—dilating enemy movement/projectiles by 45%, doubling weapon fire rate, restoring Aegis Integrity, and amplifying damage output by **2.0×**.
- **Four-Tier Symbiotic Arsenal**:
  1. **Cinder-Lance (`[1]`)**: Self-charging thermal sidearm with infinite reserve energy and pinpoint accuracy for critical weak-point strikes.
  2. **Hellfire Scatter-Maw (`[2]`)**: Twin-bore pneumatic slag cannon firing a 7-pellet horizontal arc that staggers charging brutes.
  3. **Soul-Tether Pulse Rifle (`[3]`)**: High-RPM automatic plasma repeater that builds Ascension Resonance rapidly on sustained hit streaks.
  4. **Brimstone Fissure Generator — B.F.G. (`[4]`)**: Fires an unstable singularity core that detonates in a cataclysmic radial shockwave, obliterating clustered hostiles.

---

### 2. Rules (10 Points)

The simulation is governed by strict deterministic rules that enforce aggressive, tactical decision-making:

1. **The Law of Kinetic Harvesting**: Standing still causes **Abyssal Frost/Cinder Decay** during high-corruption waves; players must maintain movement and combat engagement to sustain their **Combo Multiplier (`1.0×` to `5.0×`)**, which decays after 4.5 seconds of inactivity.
2. **Ammunition & Thermal Equilibrium**:
   - Weapon `[1]` (*Cinder-Lance*) utilizes regenerative thermal cells (infinite reserve) to prevent soft-locking.
   - Weapons `[2]` and `[3]` consume **Slag Shells** (max 60) and **Plasma Cells** (max 200) respectively.
   - Weapon `[4]` (*B.F.G.*) consumes **25 Plasma Cells** per singularity discharge.
3. **Aegis Shield Partitioning**: Incoming damage is mitigated by **65%** as long as the player possesses **Aegis Armor (`0–100`)**, absorbing the brunt of the impact before depleting **Vitality (`0–100`)**.
4. **Stratum Gate Lock Rule**: The **Ascension Portal Beacon** in each Stratum remains sealed behind an impenetrable runic barrier until **100% of the Stratum's Hostile Quota** is purged.
5. **Line-of-Sight & Spatial Acoustics**: Ranged projectiles travel in real-time 3D coordinate space and are blocked by obsidian pillars, whereas *Void-Cantors* can phase-step laterally if targeted continuously.

---

### 3. Victory and Loss Conditions (10 Points)

#### Victory Conditions
- **Stratum Victory (Stage Clear)**: Eliminate all manifested abyssal entities within the current Stratum to unlock the **Ascension Beacon**, then step into the golden-crimson rift to ascend to the next vertical tier.
- **Grand Campaign Victory (Ascension Complete)**: Conquer all three vertical strata—**Stratum I: The Nadir Vault**, **Stratum II: The Crucible Foundry**, and **Stratum III: The Empyrean Breach**—and defeat the apex guardian, **Archon Malakor, Sovereign of Cinders**, sealing the Abyssal Rift permanently.
- **Mode-Specific Triumphs**:
  - *Purgatory Time-Trial*: Complete all three strata before the **180-second Doomsday Clock** expires.
  - *Endless Abyssal Horde*: Survive as many escalating waves as possible to achieve an **S+ Sovereign Rank** (`Score ≥ 15,000`).

#### Loss Conditions
- **Vitality Depletion (`HP ≤ 0`)**: If the player's Vitality drops to zero from melee strikes, hell-orb projectiles, or boss shockwaves, the Warden's physical vessel shatters, triggering an immediate **Ascension Failure (Game Over)**.
- **Chrono-Collapse (Time-Trial Mode Only)**: If the Purgatory countdown reaches `00:00.00` before the final beacon is breached, the tectonic plates seal forever, resulting in an automatic loss.

---

### 4. Interactivity Modes (10 Points)

The game features distinct **System Interactivity Layers** and **Playable Game Modes**:

#### A. System Interactivity Layers
1. **First-Person Kinetic Combat Mode (Primary Active View)**: Direct 360° pointer-locked mouse-look, WASD strafing, real-time depth-shaded 3D viewport, tactical radar minimap, dynamic crosshair hit-markers, and animated weapon viewmodels.
2. **Tactical HUD & Telemetry Mode**: Real-time peripheral feedback including directional damage vignettes, combo decay bars, Ascension Resonance meters, and live objective tracking.
3. **Interactive Codex & Design Rubric Mode (`[TAB]` or Menu Overlay)**: In-application academic and technical inspection interface allowing players or evaluators to switch seamlessly between live FPS combat and the structured 6-Criteria Game Design specification.

#### B. Playable Game Modes
1. **Campaign Ascension (Story Mode)**: Structured 3-Stratum vertical progression culminating in the boss battle against *Archon Malakor*.
2. **Endless Abyssal Horde (Survival Arena)**: Infinite procedural wave generator with scaling enemy density, elite spawns, and high-score leaderboard tracking.
3. **Purgatory Blitz (Speedrun Time-Trial)**: High-adrenaline race against a strict countdown clock where kills grant `+2.5s` time extensions.

---

### 5. Challenges (10 Points)

Players must overcome five interlocking layers of challenge:

1. **Archetype Synergy & Target Prioritization**:
   - **Ash-Wretches** swarm rapidly in melee packs to corner the player.
   - **Cinder-Imps** strafe laterally and lob predictive fireball projectiles.
   - **Iron-Gorgers** act as heavily armored sponges that soak shotgun blasts while advancing relentlessly.
   - **Void-Cantors** blink-teleport across sightlines and fire high-velocity void bolts.
2. **Spatial & Architectural Awareness**: Navigating multi-chamber obsidian labyrinths, choke points, and pillar arenas using both direct 3D sightlines and the top-right **Tactical Radar**.
3. **Resource & Cooldown Triage**: Balancing scarce *Plasma Cells* between sustained *Pulse Rifle* suppression and saving 25 cells for emergency *B.F.G.* crowd-clearing blasts, while timing *Abyssal Dash* cooldowns (`1.4s`).
4. **Combo Preservation Under Pressure**: Maintaining aggressive sightlines to prevent the `5.0×` score and resonance multiplier from expiring without recklessly overextending into crossfire.
5. **The Apex Boss Encounter (Archon Malakor)**: A towering multi-phase colossus in Stratum III that fires tri-spread plasma volleys, summons reinforcement thralls at 50% HP, and demands constant lateral dashing and Ascension Overdrive timing.

---

### 6. Structure: Objectives, Flow, Duration, Availability, Relationship, Progression (10 Points)

| Structural Dimension | Design Specification in *DOOM: HELL'S ASCENSION* |
| :--- | :--- |
| **Objectives** | **Macro Objective**: Ascend from the bottom of the Abyss to the Empyrean Breach and slay *Archon Malakor*.<br>**Micro Objectives**: Clear each Stratum's demon quota, harvest Ichor/Aegis caches, maintain high kill-chain multipliers, charge the Ascension Gauge, and reach the unlocked Ascension Beacon. |
| **Flow** | Follows a rhythmic **"Tension → Climax → Release → Escalation"** cadence: Exploration & initial skirmish → Intense multi-directional arena wave → Overdrive counter-offensive → Beacon extraction & Stratum briefing intermission. |
| **Duration** | **Single Stratum**: `2–4 minutes` of high-intensity combat.<br>**Full Campaign Ascension**: `8–12 minutes` per complete playthrough.<br>**Purgatory Blitz**: Capped at `3 minutes` base timer.<br>**Endless Horde**: Open-ended (`5–30+ minutes`). |
| **Availability** | Deployed as a **zero-install, cross-platform WebGL/Canvas application on Vercel**, accessible instantly on any desktop browser (Windows, macOS, Linux) with keyboard + mouse support and offline-capable procedural assets. |
| **Relationship** | Every subsystem is tightly coupled in a **closed-loop economy**: Movement (*Dash*) enables survival → Aggressive kills generate *Ascension Resonance* & *Ammo Drops* → *Ascension Overdrive* empowers weapons to clear elite waves → Clearing waves unlocks spatial progression (*Stratum Beacons*). |
| **Progression** | **Vertical Spatial Progression**: Rising from frozen ash (*Stratum I: Nadir Vault*) → molten industrial machinery (*Stratum II: Crucible Foundry*) → cosmic-infernal cathedral (*Stratum III: Empyrean Breach*).<br>**Arsenal & Mastery Progression**: Unlocking heavier ordnance, facing smarter enemy compositions, and climbing from Rank `D` to `S+ SOVEREIGN`. |

---

## Controls

| Input | Action |
| :--- | :--- |
| **`W` `A` `S` `D` / Arrow Keys** | Move Forward, Strafe Left, Move Backward, Strafe Right |
| **Mouse Move / `←` `→`** | Look / Rotate Camera (Click canvas to lock pointer) |
| **Left Mouse Click / `SPACE`** | Fire Equipped Weapon |
| **`1` / `2` / `3` / `4` or Scroll** | Switch Weapons (Cinder-Lance, Scatter-Maw, Pulse Rifle, B.F.G.) |
| **`SHIFT`** | Abyssal Phase-Dash (Directional Dodge + Brief Invulnerability) |
| **`Q` or `E`** | Activate **Ascension Overdrive** (Requires 100% Ascension Gauge) |
| **`M`** | Toggle Procedural Industrial Synth Soundtrack |
| **`ESC` / `P`** | Pause Game / Open Tactical & Criteria Menu |

---

## Local Development & Deployment

```bash
# Run locally
npx serve .

# Deploy to Vercel
npx vercel --prod
```
