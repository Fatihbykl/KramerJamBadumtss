# Clockwork Gearslinger: Game Feel & "Juice" Plan

To make a rhythm FPS feel incredible, the visual feedback must be as sharp and punchy as the heavy metal music. Below is a structured plan to implement maximum "juice" for the 48-hour game jam.

## Phase 1: Camera Shake (Cinemachine Impulse)
The camera needs to physically react to the beat and the player's performance. We will use Unity's **Cinemachine Impulse** system because it requires very little code and looks highly realistic.

1. **The "Perfect Shot" Recoil:**
   * **Trigger:** When `OnGunFired` occurs.
   * **Effect:** A sharp, high-frequency, short-duration jolt pushing the camera backwards and slightly upwards to simulate heavy revolver recoil.
2. **The "Jam" Glitch:**
   * **Trigger:** When `OnGunJammed` occurs.
   * **Effect:** A messy, horizontal, vibrating shake. It should feel wrong and disorienting, matching the sudden silence of the guitars being muted.
3. **The "Recovery Slam":**
   * **Trigger:** On the 3rd consecutive recovery hit.
   * **Effect:** A massive, low-frequency, heavy vertical "drop" shake to emphasize the guitars coming back into the mix. It should feel like a bass drop.

## Phase 2: Post-Processing Reactivity
We will use Unity's Global Volume (URP or HDRP) to dynamically manipulate the screen's rendering based on the rhythm state.

1. **Dynamic Chromatic Aberration (Color Fringing):**
   * Normally at `0`.
   * When the gun jams, instantly spike it to `1.0` (max glitch effect) and slowly fade it out over the duration of the Jam state.
2. **Bloom Overdrive:**
   * Briefly spike the Bloom intensity exactly on the beat when firing the weapon to make the muzzle flash blind the player slightly, selling the power of the 1-bullet revolver.
3. **Beat Vignette:**
   * A subtle dark border that pulses slightly tighter to the center exactly on every beat, subconsciously reinforcing the tempo even if the player isn't looking at the UI.

## Phase 3: Particle Effects (VFX)
Mechanical, steampunk-themed particles to sell the industrial setting.

1. **Enemy Death (Gear Splatter):**
   * Instead of generic blood, enemies explode into a shower of physical gears, cogs, sparks, and black oil. The gears should have rigidbodies so they bounce on the floor.
2. **Muzzle Flash (Clockwork Sparks):**
   * A massive burst of bright orange sparks and thick smoke emerging from the gun barrel when fired.
3. **Dash Dust (Movement Feedback):**
   * Since grid movement is instantaneous/fast, spawn a small burst of steam or dust at the player's feet every time they successfully move on the beat. This gives weight to the grid steps.

## Phase 4: UI Animation (The "BPM" feel)
Static UI is boring. Everything should move.

1. **"ONE MORE TIME!" Animation:**
   * Instead of just toggling `SetActive(true)`, we will scale the text from `0` to `1.5` instantly, then elastic-bounce back to `1.0`. 
   * Add a slight randomized rotation (shake) to the text while it is on screen.
2. **Crosshair Recoil:**
   * Update the `UIManager` so that firing doesn't just pulse the crosshair, but actually kicks it visually upward on the canvas, slowly settling back to the center, mimicking real gun recoil.
3. **Recovery Pips Feedback:**
   * When a pip is filled, it flashes bright white for 0.1s before settling to gold.
   * If a recovery beat is missed, the pips physically shake side-to-side (like a "No" head shake) while flashing red.

## Implementation Steps (Next Actions)
If you agree with this plan, we can tackle them one by one. I recommend this order:
* **Step 1:** Implement Cinemachine Impulse Camera Shakes. (Highest impact, lowest effort).
* **Step 2:** Hook up a Post-Processing script to manipulate Chromatic Aberration.
* **Step 3:** Create the Particle Prefabs and spawn them via code.
* **Step 4:** Add Coroutines for the UI elastic bouncing.
