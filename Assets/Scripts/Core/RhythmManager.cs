using UnityEngine;
using System;
using System.Collections.Generic;
using ClockworkGearslinger.Audio; // Requires access to AudioManager

namespace ClockworkGearslinger.Core
{
    /// <summary>
    /// Tracks the precise position of the song based on AudioSettings.dspTime.
    /// Provides mathematical helper methods to validate if player input is "on beat".
    /// </summary>
    public class RhythmManager : MonoBehaviour
    {
        public static RhythmManager Instance { get; private set; }

        [Header("Song Configuration")]
        [Tooltip("Beats Per Minute of the current song.")]
        [SerializeField] private float bpm = 120f;
        
        [Tooltip("The tolerance window (in seconds) for a player input to be considered a 'hit'.")]
        [SerializeField] private float defaultInputTolerance = 0.15f;

        [Tooltip("Offset in seconds to fix tracks that have a tiny bit of silence at the start, or to compensate for hardware audio latency. Tweak this until the visual pulses match the audio hits exactly.")]
        [SerializeField] private float audioOffset = 0f;

        [Header("MIDI Synchronization")]
        [Tooltip("Enable to use a MIDI file for exact beat timings instead of mathematical BPM.")]
        [SerializeField] private bool useMidiFile = false;
        
        [Tooltip("The .mid file renamed to .bytes to be read by Unity.")]
        [SerializeField] private TextAsset midiFile;
        
        [Tooltip("Which MIDI note to track as the 'Beat' (e.g. 36 for standard Kick Drum). Leave as -1 to track ALL notes.")]
        [SerializeField] private int targetMidiNote = 36;
        
        [Tooltip("Force the MIDI parser to use the Game's BPM and ignore any Tempo data embedded in the MIDI file. Turn this ON if your MIDI beats drift and get faster/slower than the audio.")]
        [SerializeField] private bool overrideMidiTempo = true;

        private List<float> beatTimestamps;

        // Event for visuals (UI, environment pulses). 
        // DO NOT use this for gameplay input logic; use IsOnBeat() instead.
        public event Action OnBeat;

        // Core timing data
        private float secPerBeat;
        public float SongPositionInBeats { get; private set; }

        public float InputToleranceInBeats
        {
            get { return defaultInputTolerance / secPerBeat; }
        }

        // Tracks the last beat we fired an event for
        private int lastRecordedBeat = -1;

        private void Awake()
        {
            // Standard Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Pre-calculate the duration of a single beat
            secPerBeat = 60f / bpm;

            if (useMidiFile && midiFile != null)
            {
                // Pass the current mathematical BPM and the override flag
                beatTimestamps = MidiBeatParser.Parse(midiFile.bytes, targetMidiNote, bpm, overrideMidiTempo);
                if (beatTimestamps == null || beatTimestamps.Count == 0)
                {
                    Debug.LogError("[RhythmManager] Failed to parse MIDI file or found no matching notes. Falling back to BPM.");
                    useMidiFile = false;
                }
                else
                {
                    Debug.Log($"[RhythmManager] Successfully loaded {beatTimestamps.Count} beats from MIDI.");
                }
            }
        }

        private void Update()
        {
            // Ensure audio manager exists and music is scheduled
            if (AudioManager.Instance == null || !AudioManager.Instance.IsPlaying) 
                return;

            // Ensure the song has actually started playing (accounting for the start delay)
            if (AudioSettings.dspTime < AudioManager.Instance.SongStartTime)
                return;

            // Calculate precise continuous song position in seconds
            float continuousSongPosition = (float)(AudioSettings.dspTime - AudioManager.Instance.SongStartTime);
            float trackDuration = AudioManager.Instance.TrackDuration;
            
            int loopCount = 0;
            float loopedSongPosition = continuousSongPosition;

            // If the audio track has a duration, handle looping math seamlessly
            if (trackDuration > 0.1f)
            {
                loopCount = Mathf.FloorToInt(continuousSongPosition / trackDuration);
                loopedSongPosition = continuousSongPosition % trackDuration;
            }

            // Apply custom audio offset to the looped position
            float songPositionInSeconds = loopedSongPosition + audioOffset;
            
            float baseBeatForThisLoop = 0f;
            float beatWithinLoop = 0f;

            if (useMidiFile && beatTimestamps != null && beatTimestamps.Count > 0)
            {
                baseBeatForThisLoop = loopCount * beatTimestamps.Count;

                // Calculate float SongPositionInBeats based on MIDI timestamps
                int lastPassedIndex = -1;
                for (int i = 0; i < beatTimestamps.Count; i++)
                {
                    if (songPositionInSeconds >= beatTimestamps[i])
                    {
                        lastPassedIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lastPassedIndex == -1)
                {
                    // We are before the very first beat
                    float diff = beatTimestamps[0] - songPositionInSeconds;
                    beatWithinLoop = -diff / secPerBeat;
                }
                else if (lastPassedIndex == beatTimestamps.Count - 1)
                {
                    // Passed the last beat
                    float diff = songPositionInSeconds - beatTimestamps[lastPassedIndex];
                    beatWithinLoop = lastPassedIndex + (diff / secPerBeat);
                }
                else
                {
                    // Interpolate between last passed and next beat
                    float currentBeatTime = beatTimestamps[lastPassedIndex];
                    float nextBeatTime = beatTimestamps[lastPassedIndex + 1];
                    float duration = nextBeatTime - currentBeatTime;
                    
                    float fraction = 0f;
                    if (duration > 0f)
                    {
                        fraction = (songPositionInSeconds - currentBeatTime) / duration;
                    }
                    
                    beatWithinLoop = lastPassedIndex + fraction;
                }
            }
            else
            {
                // Convert to beats mathematically
                if (trackDuration > 0.1f)
                {
                    baseBeatForThisLoop = loopCount * (trackDuration / secPerBeat);
                }
                beatWithinLoop = songPositionInSeconds / secPerBeat;
            }

            // Combine the loop base with the current progress
            SongPositionInBeats = baseBeatForThisLoop + beatWithinLoop;

            // Check if we crossed a whole integer beat boundary (e.g. beat 1.0, 2.0)
            int currentBeat = Mathf.FloorToInt(SongPositionInBeats);
            if (currentBeat > lastRecordedBeat)
            {
                lastRecordedBeat = currentBeat;
                
                // Fire the beat event for UI/Visuals
                OnBeat?.Invoke();
            }
        }

        /// <summary>
        /// Checks if the current time is within the acceptable tolerance window of the nearest beat.
        /// Call this the exact frame the player presses movement/shoot input.
        /// </summary>
        /// <param name="customTolerance">Optional override. If less than 0, uses default tolerance.</param>
        /// <returns>True if the timing was correct, false if they missed the beat.</returns>
        public bool IsOnBeat(float customTolerance = -1f)
        {
            // Determine which tolerance window to use
            float toleranceToUse = customTolerance >= 0f ? customTolerance : defaultInputTolerance;

            if (useMidiFile && beatTimestamps != null && beatTimestamps.Count > 0)
            {
                float continuousSongPosition = (float)(AudioSettings.dspTime - AudioManager.Instance.SongStartTime);
                float trackDuration = AudioManager.Instance.TrackDuration;
                if (trackDuration > 0.1f)
                {
                    continuousSongPosition %= trackDuration;
                }
                
                float songTimeSeconds = continuousSongPosition + audioOffset;
                
                // Find closest timestamp
                float closestTime = beatTimestamps[0];
                float minDiff = Mathf.Abs(songTimeSeconds - closestTime);
                
                for (int i = 1; i < beatTimestamps.Count; i++)
                {
                    float diff = Mathf.Abs(songTimeSeconds - beatTimestamps[i]);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closestTime = beatTimestamps[i];
                    }
                    else if (diff > minDiff) 
                    {
                        break; 
                    }
                }
                
                return minDiff <= toleranceToUse;
            }

            // 1. Find the closest whole beat to our current decimal position
            float closestBeat = Mathf.Round(SongPositionInBeats);

            // 2. Calculate the distance (in beats) from our current position to that closest whole beat
            float differenceInBeats = Mathf.Abs(SongPositionInBeats - closestBeat);
            
            // 3. Convert that beat distance back into real time (seconds)
            float differenceInSeconds = differenceInBeats * secPerBeat;

            // 4. Return true if the distance in seconds is within our allowed tolerance window
            return differenceInSeconds <= toleranceToUse;
        }
    }
}
