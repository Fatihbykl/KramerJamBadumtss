using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro is highly recommended for sharp, modern UI in Unity
using ClockworkGearslinger.Core;
using ClockworkGearslinger.Player;
using System.Collections;

namespace ClockworkGearslinger.UI
{
    /// <summary>
    /// Phase 5: Manages visual feedback for rhythm and gameplay states.
    /// Purely event-driven; it listens to the Core and Player scripts without tightly coupling to them.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Script References")]
        [Tooltip("Drag the Player GameObject here so we can listen to its events.")]
        [SerializeField] private PlayerController playerController;
        
        [Header("UI Elements")]
        [Tooltip("The central crosshair that pulses to the beat.")]
        [SerializeField] private RectTransform crosshair;
        
        [Tooltip("The massive text that flashes when jammed.")]
        [SerializeField] private TextMeshProUGUI jamText; 
        
        [Tooltip("Array of 3 UI Images (pips/dots) to track recovery progress.")]
        [SerializeField] private SkinnedMeshRenderer[] recoveryPips;

        [Header("Polish Settings")]
        [SerializeField] private float crosshairPulseScale = 1.3f;
        [SerializeField] private float pulseDuration = 0.15f;
        [SerializeField] private Material pipFilledMaterial;
        [SerializeField] private Material pipErrorMaterial;

        private Vector2 originalCrosshairPos;

        private void Start()
        {
            // Initialize UI state
            if (crosshair != null) originalCrosshairPos = crosshair.anchoredPosition;
            if (jamText != null) jamText.gameObject.SetActive(false);
            ResetPips(pipFilledMaterial);

            // 1. Subscribe to the Metronome
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat += HandleBeatPulse;
            }

            // 2. Subscribe to Player Actions
            if (playerController != null)
            {
                playerController.OnGunJammed += ShowJamUI;
                playerController.OnRecoveryHit += UpdateRecoveryPips;
                playerController.OnRecoveryFailed += ShowRecoveryError;
                playerController.OnGunFired += HandleGunFired;
            }
            else
            {
                Debug.LogWarning("[UIManager] PlayerController reference is missing! Please assign it in the Inspector.");
            }
        }

        private void OnDestroy()
        {
            // ALWAYS unsubscribe from events to prevent memory leaks in Unity
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat -= HandleBeatPulse;
            }

            if (playerController != null)
            {
                playerController.OnGunJammed -= ShowJamUI;
                playerController.OnRecoveryHit -= UpdateRecoveryPips;
                playerController.OnRecoveryFailed -= ShowRecoveryError;
                playerController.OnGunFired -= HandleGunFired;
            }
        }

        /// <summary>
        /// Fires every integer beat. Scales the crosshair up and lerps it back down.
        /// </summary>
        private void HandleBeatPulse()
        {
            if (crosshair == null || !gameObject.activeInHierarchy) return;

            StopCoroutine(nameof(PulseCrosshair));
            StartCoroutine(nameof(PulseCrosshair));
        }

        private IEnumerator PulseCrosshair()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * crosshairPulseScale;
            float elapsedTime = 0f;

            // Instantly pop the crosshair large
            crosshair.localScale = targetScale;

            // Smoothly shrink it back over 'pulseDuration' seconds
            while (elapsedTime < pulseDuration)
            {
                crosshair.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / pulseDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            crosshair.localScale = originalScale;
        }

        private void ShowJamUI()
        {
            if (jamText != null)
            {
                jamText.text = "ONE MORE TIME!";
                StopCoroutine(nameof(BounceText));
                StartCoroutine(nameof(BounceText));
            }
            ResetPips(pipErrorMaterial);
        }

        private IEnumerator BounceText()
        {
            if (jamText == null) yield break;
            jamText.gameObject.SetActive(true);
            
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 finalScale = Vector3.one;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Classic elastic overshoot equation
                float elasticT = Mathf.Sin(-13f * (t + 1) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
                
                jamText.transform.localScale = Vector3.LerpUnclamped(startScale, finalScale, elasticT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            jamText.transform.localScale = finalScale;
        }

        /// <summary>
        /// Fills the UI pips one by one as the player hits consecutive recovery beats.
        /// </summary>
        private void UpdateRecoveryPips(int currentHits)
        {
            for (int i = 0; i < recoveryPips.Length; i++)
            {
                if (recoveryPips[i] == null) continue;

                Material[] mats = recoveryPips[i].sharedMaterials;
                if (i < currentHits)
                {
                    mats[1] = pipFilledMaterial;
                }
                else
                {
                    mats[1] = pipErrorMaterial;
                }
                recoveryPips[i].sharedMaterials = mats;
            }

            // Hide the jam UI when successfully recovered
            if (currentHits >= 3)
            {
                if (jamText != null) jamText.gameObject.SetActive(false);
                ResetPips(pipFilledMaterial); 
                // Note: Good place to flash the screen white as a "success" impact!
            }
        }

        /// <summary>
        /// Flashes the pips red to communicate that the sequence was broken.
        /// </summary>
        private void ShowRecoveryError()
        {
            foreach (var pip in recoveryPips)
            {
                if (pip != null)
                {
                    Material[] mats = pip.sharedMaterials;
                    mats[1] = pipErrorMaterial;
                    pip.sharedMaterials = mats;
                }
            }

            // Return them to empty after a quick delay so the player can try again
            Invoke(nameof(ResetPips), 0.2f);
        }

        private void ResetPips(Material targetMaterial)
        {
            foreach (var pip in recoveryPips)
            {
                if (pip != null)
                {
                    Material[] mats = pip.sharedMaterials;
                    mats[1] = targetMaterial;
                    pip.sharedMaterials = mats;
                }
            }
        }

        private void HandleGunFired()
        {
            // Add extra juice when a perfect shot is fired!
            if (crosshair != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(nameof(PulseCrosshair));
                // Make the pulse bigger than a normal beat pulse
                crosshair.localScale = Vector3.one * (crosshairPulseScale * 1.5f);
                StartCoroutine(nameof(PulseCrosshair));

                // Physical kick upward
                StopCoroutine(nameof(RecoilCrosshair));
                StartCoroutine(nameof(RecoilCrosshair));
            }
        }

        private IEnumerator RecoilCrosshair()
        {
            if (crosshair == null) yield break;
            
            Vector2 recoilPos = originalCrosshairPos + new Vector2(0f, 40f); 
            float duration = 0.2f;
            float elapsed = 0f;

            // Instantly kick up, then smoothly ease down
            crosshair.anchoredPosition = recoilPos;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Cubic ease out
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                crosshair.anchoredPosition = Vector2.Lerp(recoilPos, originalCrosshairPos, easeT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            crosshair.anchoredPosition = originalCrosshairPos;
        }
    }
}
