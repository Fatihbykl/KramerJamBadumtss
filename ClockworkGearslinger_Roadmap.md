# Clockwork Gearslinger: 48-Hour Technical Roadmap

This roadmap is designed for a 48-hour game jam. The focus is on modularity, strict audio synchronization, and clear separation of concerns to avoid spaghetti code, allowing for rapid iteration and debugging.

## Phase 1: Precise Audio Stem Synchronizer
**Goal:** Play all music stems (Drums/Bass, Rhythm Guitar, Lead Guitar) in perfect synchronization without phase cancellation or drifting.

*   **Architecture:** `AudioManager` (MonoBehaviour, typically a Singleton or Service Locator).
*   **Components Needed:** Multiple `AudioSource` components attached to the manager (one for each stem).
*   **Implementation Steps:**
    1.  Create an array or list of `AudioSource` references.
    2.  Disable `Play On Awake` on all AudioSources.
    3.  In `Start()`, calculate a precise start time slightly in the future:
        `double nextEventTime = AudioSettings.dspTime + 1.0d;`
    4.  Loop through all stems and schedule them:
        `stemAudioSource.PlayScheduled(nextEventTime);`
    5.  Create public methods to handle dynamic mixing, e.g., `MuteGuitars()` and `UnmuteGuitars()`. Do not pause or stop the sources, just change their `volume` to 0 or 1. This guarantees they never lose synchronization.

## Phase 2: Rhythm & Beat Controller
**Goal:** Track the exact position of the song in beats and provide a robust tolerance window for player inputs.

*   **Architecture:** `RhythmManager` (MonoBehaviour).
*   **Key Variables:** `float bpm`, `float secPerBeat`, `double songStartTime`, `float songPositionInBeats`, `float inputTolerance` (e.g., 0.15 seconds).
*   **Implementation Steps:**
    1.  Store the DSP time when the music actually starts (from Phase 1): `songStartTime = nextEventTime;`.
    2.  In `Update()`, calculate the current song position:
        `float songPosition = (float)(AudioSettings.dspTime - songStartTime);`
        `songPositionInBeats = songPosition / secPerBeat;`
    3.  Implement an `IsOnBeat()` helper function. When the player inputs an action, check the decimal part of `songPositionInBeats`. If it is close to `0.0` or `1.0` (within `inputTolerance / secPerBeat`), the hit is valid.
    4.  Create a `public event Action OnBeat;`. Track the current integer beat and fire this event every time a new beat is crossed to drive UI/Environment animations. *Note: Avoid tying gameplay logic to this event; rely on the polling `IsOnBeat()` method when input occurs for exact precision.*

## Phase 3: State Machine & Input Handling (Normal vs Jammed)
**Goal:** Handle player grid movement and the 1-bullet firing mechanic cleanly based on rhythm accuracy.

*   **Architecture:** `PlayerController` utilizing a simple State Machine pattern (Enum or State classes).
    `public enum PlayerState { Normal, Jammed }`
*   **Implementation Steps:**
    1.  **Movement (`Normal` State):** When WASD is pressed, check `RhythmManager.Instance.IsOnBeat()`. If true, trigger a coroutine or tween to move the player to the next grid tile over exactly one beat duration.
    2.  **Firing (`Normal` State):** When LMB is pressed:
        *   Check `IsOnBeat()`.
        *   If **True** and `bulletCount == 1`: Fire weapon, spawn VFX/Hitscan, set `bulletCount = 0`.
        *   If **False** (Missed beat): Instantly transition to `PlayerState.Jammed`.
    3.  **Jam Event:** When entering the `Jammed` state, trigger an event `public event Action OnGunJammed;`.

## Phase 4: The 3-Beat Recovery Logic ("One More Time")
**Goal:** Force the player to hit 3 consecutive beats to recover from a jam, integrated with dynamic audio feedback.

*   **Architecture:** Handled within the `Jammed` state logic of the `PlayerController`.
*   **Implementation Steps:**
    1.  **Entering Jam State:**
        *   Call `AudioManager.Instance.MuteGuitars()`.
        *   Set `consecutiveHits = 0`.
    2.  **Recovery Input Loop:** Wait for the specific recovery input (e.g., Spacebar or LMB).
    3.  **Input Validation:**
        *   If input occurs and `IsOnBeat()` is **True**: `consecutiveHits++`. Play a metallic click/ratchet SFX.
        *   If input occurs and `IsOnBeat()` is **False**: Reset `consecutiveHits = 0`. Play an error/buzz SFX.
    4.  **Success Condition:** Once `consecutiveHits >= 3`:
        *   Call `AudioManager.Instance.UnmuteGuitars()` for heavy musical impact.
        *   Set `bulletCount = 1`.
        *   Transition back to `PlayerState.Normal`.

## Phase 5: UI & Polish
**Goal:** Provide critical visual feedback so the player can internalize the rhythm and understand the game state.

*   **Architecture:** `UIManager` listening to C# events from Managers.
*   **Implementation Steps:**
    1.  **Crosshair Metronome:** Subscribe to `RhythmManager.OnBeat`. Scale the crosshair up and lerp it down every beat.
    2.  **Jam Notification:** Subscribe to `PlayerController.OnGunJammed`. Activate a flashy "ONE MORE TIME!" text UI (use screen shake or chromatic aberration post-processing here for impact).
    3.  **Recovery Feedback:** Show a 3-pip UI element above the gun or crosshair. Fill one pip for every `consecutiveHits`. Flash them red if a beat is missed.
    4.  **Success Impact:** On recovery success, flash the screen slightly white or trigger a heavy camera shake to emphasize the guitars returning.

---
### 💡 Jam Tips for this Architecture:
*   **Never use `Time.deltaTime` or standard AudioSource.time for rhythm checks.** `AudioSettings.dspTime` is the single source of truth.
*   Keep logic purely mathematical in the Managers. Let the visuals and UI *react* via events.
*   If time gets tight, skip complex grid pathfinding and just raycast forward/backward/sides to check for walls before executing a movement tween.
