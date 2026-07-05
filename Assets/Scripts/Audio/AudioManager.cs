using UnityEngine;

namespace ClockworkGearslinger.Audio
{
    /// <summary>
    /// Manages highly precise audio stem synchronization using AudioSettings.dspTime.
    /// Acts as a Singleton for easy access across the game.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Stems")]
        [Tooltip("The foundational drum and bass track that never mutes.")]
        [SerializeField] private AudioSource drumsAndBassSource;
        
        [Tooltip("The rhythm guitar track that mutes on jams.")]
        [SerializeField] private AudioSource rhythmGuitarSource;
        
        [Tooltip("The lead guitar track that mutes on jams.")]
        [SerializeField] private AudioSource leadGuitarSource;

        [Header("SFX")]
        [Tooltip("Audio source for playing one-shot sound effects (like shooting).")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Assign your shoot sounds here to play them randomly.")]
        [SerializeField] private AudioClip[] shootSounds;
        [SerializeField] private AudioClip movementSound;

        [Header("Settings")]
        [Tooltip("Delay in seconds before the audio starts playing. Gives time for loading.")]
        [SerializeField] private double startDelay = 1.0d;

        // Exposed property to let other systems (like RhythmManager) know exactly when the song starts.
        public double SongStartTime { get; private set; }
        
        public bool IsPlaying { get; private set; }

        public float CurrentPlaybackTime
        {
            get { return drumsAndBassSource != null ? drumsAndBassSource.time : 0f; }
        }

        public float TrackDuration
        {
            get { return (drumsAndBassSource != null && drumsAndBassSource.clip != null) ? drumsAndBassSource.clip.length : 0f; }
        }

        private void Awake()
        {
            // Standard Singleton pattern setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Optional: Uncomment if the music needs to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Start the music automatically.
            // If you prefer to start music based on game state (e.g. after a countdown), 
            // you can move this call to a public GameManager method.
            StartMusic();
        }

        /// <summary>
        /// Schedules all audio stems to start playing at exactly the same DSP time.
        /// </summary>
        public void StartMusic()
        {
            if (drumsAndBassSource == null || rhythmGuitarSource == null || leadGuitarSource == null)
            {
                Debug.LogError("[AudioManager] Missing AudioSource references. Please assign them in the inspector.");
                return;
            }

            // Force stop and reset all sources to prevent 'Play On Awake' desyncs
            drumsAndBassSource.Stop();
            rhythmGuitarSource.Stop();
            leadGuitarSource.Stop();

            drumsAndBassSource.playOnAwake = false;
            rhythmGuitarSource.playOnAwake = false;
            leadGuitarSource.playOnAwake = false;

            drumsAndBassSource.time = 0f;
            rhythmGuitarSource.time = 0f;
            leadGuitarSource.time = 0f;

            // Calculate precise start time in the future using the highly accurate dspTime
            SongStartTime = AudioSettings.dspTime + startDelay;

            // Schedule all sources to start precisely at SongStartTime.
            // This guarantees they will perfectly sync and not drift.
            drumsAndBassSource.PlayScheduled(SongStartTime);
            rhythmGuitarSource.PlayScheduled(SongStartTime);
            leadGuitarSource.PlayScheduled(SongStartTime);

            IsPlaying = true;
            Debug.Log($"[AudioManager] Scheduled music to start precisely at DSP time: {SongStartTime}");
        }

        /// <summary>
        /// Instantly mutes the guitar stems (used when the player enters the Jammed state).
        /// We change volume instead of pausing/stopping to ensure stems stay perfectly synchronized in the background.
        /// </summary>
        public void MuteGuitars()
        {
            if (rhythmGuitarSource != null) rhythmGuitarSource.volume = 0f;
            if (leadGuitarSource != null) leadGuitarSource.volume = 0f;
        }

        /// <summary>
        /// Unmutes the guitar stems (used when the player successfully recovers from a Jam).
        /// </summary>
        /// <param name="volume">The target volume level (defaults to 1.0)</param>
        public void UnmuteGuitars(float volume = 1f)
        {
            if (rhythmGuitarSource != null) rhythmGuitarSource.volume = volume;
            if (leadGuitarSource != null) leadGuitarSource.volume = volume;
        }
        
        /// <summary>
        /// Stops all music immediately if needed.
        /// </summary>
        public void StopMusic()
        {
            if (drumsAndBassSource != null) drumsAndBassSource.Stop();
            if (rhythmGuitarSource != null) rhythmGuitarSource.Stop();
            if (leadGuitarSource != null) leadGuitarSource.Stop();
            IsPlaying = false;
        }

        /// <summary>
        /// Plays a randomly selected shooting sound effect.
        /// </summary>
        public void PlayRandomShootSound()
        {
            if (sfxSource != null && shootSounds != null && shootSounds.Length > 0)
            {
                int randomIndex = Random.Range(0, shootSounds.Length);
                sfxSource.PlayOneShot(shootSounds[randomIndex]);
            }
            else
            {
                Debug.LogWarning("[AudioManager] Attempted to play shoot sound, but sfxSource or shootSounds array is missing/empty!");
            }
        }
        
        public void PlayMovementSound()
        {
            sfxSource.PlayOneShot(movementSound);
        }
    }
}
