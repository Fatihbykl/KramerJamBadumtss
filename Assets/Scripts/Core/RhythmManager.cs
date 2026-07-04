using UnityEngine;
using System;
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
        }

        private void Update()
        {
            // Ensure audio manager exists and music is scheduled
            if (AudioManager.Instance == null || !AudioManager.Instance.IsPlaying) 
                return;

            // Ensure the song has actually started playing (accounting for the start delay)
            if (AudioSettings.dspTime < AudioManager.Instance.SongStartTime)
                return;

            // Calculate precise song position mathematically based on DSP time
            float songPositionInSeconds = (float)(AudioSettings.dspTime - AudioManager.Instance.SongStartTime);
            
            // Convert to beats
            SongPositionInBeats = songPositionInSeconds / secPerBeat;

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
