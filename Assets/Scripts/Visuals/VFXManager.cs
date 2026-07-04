using UnityEngine;
using ClockworkGearslinger.Player;

namespace ClockworkGearslinger.Visuals
{
    /// <summary>
    /// Phase 3: Manages the spawning and destruction of particle effects (VFX).
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Script References")]
        [SerializeField] private PlayerController playerController;
        
        [Header("Spawn Points")]
        [Tooltip("Empty GameObject at the tip of your gun model")]
        [SerializeField] private Transform gunBarrelPoint;
        [Tooltip("Empty GameObject near the player's feet")]
        [SerializeField] private Transform playerFeetPoint;

        [Header("Particle Prefabs")]
        [Tooltip("Sparks and smoke for shooting")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [Tooltip("Dust or steam when moving on the grid")]
        [SerializeField] private GameObject dashDustPrefab;
        [Tooltip("Gears and oil when an enemy dies")]
        [SerializeField] private GameObject enemyDeathSplatterPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playerController != null)
            {
                // Hook up the VFX triggers to the player's actions
                playerController.OnGunFired += SpawnMuzzleFlash;
                playerController.OnMovementSuccess += SpawnDashDust;
            }
            else
            {
                Debug.LogWarning("[VFXManager] PlayerController is not assigned!");
            }
        }

        private void OnDestroy()
        {
            if (playerController != null)
            {
                playerController.OnGunFired -= SpawnMuzzleFlash;
                playerController.OnMovementSuccess -= SpawnDashDust;
            }
        }

        private void SpawnMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && gunBarrelPoint != null)
            {
                // Spawn parented to the barrel so it follows the gun if the player turns
                GameObject flash = Instantiate(muzzleFlashPrefab, gunBarrelPoint.position, gunBarrelPoint.rotation, gunBarrelPoint);
                // Clean up memory
                Destroy(flash, 2f); 
            }
        }

        private void SpawnDashDust()
        {
            if (dashDustPrefab != null && playerFeetPoint != null)
            {
                // Spawn unparented so the dust is left behind at the old grid position
                GameObject dust = Instantiate(dashDustPrefab, playerFeetPoint.position, Quaternion.identity);
                Destroy(dust, 2f);
            }
        }

        /// <summary>
        /// Global method so enemies can trigger their own death effects.
        /// </summary>
        public void SpawnEnemyDeath(Vector3 position)
        {
            if (enemyDeathSplatterPrefab != null)
            {
                // Add a slight vertical offset so particles don't spawn inside the floor
                Vector3 spawnPos = position + (Vector3.up * 1f);
                GameObject splatter = Instantiate(enemyDeathSplatterPrefab, spawnPos, Quaternion.identity);
                
                // Keep the debris around a little longer before destroying
                Destroy(splatter, 4f); 
            }
        }
    }
}
