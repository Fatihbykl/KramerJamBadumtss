using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Assumes Universal Render Pipeline (URP)
using ClockworkGearslinger.Player;
using ClockworkGearslinger.Core;
using System.Collections;

namespace ClockworkGearslinger.Visuals
{
    /// <summary>
    /// Phase 2: Post-Processing Reactivity.
    /// Dynamically modifies URP Volume overrides (Bloom, Chromatic Aberration, Vignette)
    /// based on rhythm and gameplay events.
    /// </summary>
    public class PostProcessingManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Global Volume in your scene containing your post-processing profile.")]
        [SerializeField] private Volume globalVolume;
        [SerializeField] private PlayerController playerController;

        [Header("Chromatic Aberration (Jam Glitch)")]
        [Tooltip("How intense the screen fringing gets when the weapon jams.")]
        [SerializeField] private float jamAberrationIntensity = 1f;
        [SerializeField] private float aberrationFadeDuration = 0.6f;

        [Header("Bloom (Shoot Overdrive)")]
        [Tooltip("How bright the bloom spikes when firing the weapon.")]
        [SerializeField] private float shootBloomIntensity = 15f;
        [SerializeField] private float shootBloomFadeDuration = 0.2f;

        [Header("Vignette (Beat Pulse)")]
        [Tooltip("How tight the dark borders pulse on every beat.")]
        [SerializeField] private float beatVignetteIntensity = 0.45f;
        [SerializeField] private float vignetteFadeDuration = 0.15f;

        // Cached Post-Processing Overrides
        private ChromaticAberration chromaticAberration;
        private Bloom bloom;
        private Vignette vignette;

        // Base Values (To smoothly return to your inspector settings)
        private float baseAberration = 0f;
        private float baseBloom = 0f;
        private float baseVignette = 0f;

        private void Start()
        {
            if (globalVolume != null && globalVolume.profile != null)
            {
                // Extract the specific overrides from the Volume Profile
                globalVolume.profile.TryGet(out chromaticAberration);
                globalVolume.profile.TryGet(out bloom);
                globalVolume.profile.TryGet(out vignette);

                // Store their default values so we can revert back to them
                if (chromaticAberration != null) baseAberration = chromaticAberration.intensity.value;
                if (bloom != null) baseBloom = bloom.intensity.value;
                if (vignette != null) baseVignette = vignette.intensity.value;
            }
            else
            {
                Debug.LogWarning("[PostProcessingManager] No Global Volume assigned!");
            }

            // Subscribe to gameplay events
            if (playerController != null)
            {
                playerController.OnGunJammed += TriggerJamGlitch;
                playerController.OnGunFired += TriggerShootBloom;
                playerController.OnRecoveryHit += HandleRecoveryHit;
            }

            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat += TriggerBeatVignette;
            }
        }

        private void OnDestroy()
        {
            if (playerController != null)
            {
                playerController.OnGunJammed -= TriggerJamGlitch;
                playerController.OnGunFired -= TriggerShootBloom;
                playerController.OnRecoveryHit -= HandleRecoveryHit;
            }

            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat -= TriggerBeatVignette;
            }
        }

        /// <summary>
        /// Glitches the screen with heavy color fringing when the gun jams.
        /// </summary>
        private void TriggerJamGlitch()
        {
            if (chromaticAberration == null) return;
            StopCoroutine(nameof(FadeChromaticAberration));
            StartCoroutine(FadeChromaticAberration(jamAberrationIntensity, baseAberration, aberrationFadeDuration));
        }

        /// <summary>
        /// Instantly clears the glitch effect when the player successfully recovers.
        /// </summary>
        private void HandleRecoveryHit(int consecutiveHits)
        {
            if (consecutiveHits >= 3 && chromaticAberration != null)
            {
                StopCoroutine(nameof(FadeChromaticAberration));
                chromaticAberration.intensity.value = baseAberration;
            }
        }

        /// <summary>
        /// Spikes the bloom to make the muzzle flash feel incredibly bright.
        /// </summary>
        private void TriggerShootBloom()
        {
            if (bloom == null) return;
            StopCoroutine(nameof(FadeBloom));
            StartCoroutine(FadeBloom(shootBloomIntensity, baseBloom, shootBloomFadeDuration));
        }

        /// <summary>
        /// Pulses the vignette inwards to subconsciously reinforce the tempo.
        /// </summary>
        private void TriggerBeatVignette()
        {
            if (vignette == null) return;
            StopCoroutine(nameof(FadeVignette));
            StartCoroutine(FadeVignette(beatVignetteIntensity, baseVignette, vignetteFadeDuration));
        }

        // --- Coroutines for smooth lerping ---

        private IEnumerator FadeChromaticAberration(float startValue, float endValue, float duration)
        {
            float elapsed = 0f;
            chromaticAberration.intensity.value = startValue;

            while (elapsed < duration)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(startValue, endValue, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            chromaticAberration.intensity.value = endValue;
        }

        private IEnumerator FadeBloom(float startValue, float endValue, float duration)
        {
            float elapsed = 0f;
            bloom.intensity.value = startValue;

            while (elapsed < duration)
            {
                bloom.intensity.value = Mathf.Lerp(startValue, endValue, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            bloom.intensity.value = endValue;
        }

        private IEnumerator FadeVignette(float startValue, float endValue, float duration)
        {
            float elapsed = 0f;
            vignette.intensity.value = startValue;

            while (elapsed < duration)
            {
                vignette.intensity.value = Mathf.Lerp(startValue, endValue, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            vignette.intensity.value = endValue;
        }
    }
}
