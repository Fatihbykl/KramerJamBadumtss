using UnityEngine;
using ClockworkGearslinger.Player;
using Unity.Cinemachine;

namespace ClockworkGearslinger.Visuals
{
    /// <summary>
    /// Listens to Player events and triggers Cinemachine Impulse Shakes.
    /// Keeps camera juice logic entirely separated from gameplay math.
    /// </summary>
    public class CameraShakeManager : MonoBehaviour
    {
        [Header("Script References")]
        [SerializeField] private PlayerController playerController;

        [Header("Cinemachine Impulse Sources")]
        [Tooltip("The impulse source configured for a sharp, quick recoil jolt.")]
        [SerializeField] private CinemachineImpulseSource shootShake;
        
        [Tooltip("The impulse source configured for a messy, buzzing horizontal glitch.")]
        [SerializeField] private CinemachineImpulseSource jamShake;
        
        [Tooltip("The impulse source configured for a massive, heavy vertical drop/slam.")]
        [SerializeField] private CinemachineImpulseSource recoverySlamShake;

        private void Start()
        {
            if (playerController != null)
            {
                // Subscribe to the player's gameplay events
                playerController.OnGunFired += HandleShootShake;
                playerController.OnGunJammed += HandleJamShake;
                playerController.OnRecoveryHit += HandleRecoveryHit;
            }
            else
            {
                Debug.LogWarning("[CameraShakeManager] PlayerController is not assigned!");
            }
        }

        private void OnDestroy()
        {
            if (playerController != null)
            {
                // Unsubscribe to prevent memory leaks
                playerController.OnGunFired -= HandleShootShake;
                playerController.OnGunJammed -= HandleJamShake;
                playerController.OnRecoveryHit -= HandleRecoveryHit;
            }
        }

        private void HandleShootShake()
        {
            if (shootShake != null)
            {
                // Generates a shake using the default velocity and settings on the component
                shootShake.GenerateImpulse();
            }
        }

        private void HandleJamShake()
        {
            if (jamShake != null)
            {
                jamShake.GenerateImpulse();
            }
        }

        private void HandleRecoveryHit(int consecutiveHits)
        {
            // We only want to trigger the massive slam when the player completely recovers (3 hits)
            if (consecutiveHits >= 3)
            {
                if (recoverySlamShake != null)
                {
                    recoverySlamShake.GenerateImpulse();
                }
            }
        }
    }
}
