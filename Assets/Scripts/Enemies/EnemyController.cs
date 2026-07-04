using UnityEngine;
using UnityEngine.AI;
using ClockworkGearslinger.Player; // To access PlayerHealth

namespace ClockworkGearslinger.Enemies
{
    /// <summary>
    /// Basic NavMesh enemy. Chases the player continuously.
    /// Has 1 HP. Kills player instantly on touch.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        private NavMeshAgent navAgent;
        private Transform playerTarget;

        private void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();
            
            // Find the player automatically by tag. 
            // IMPORTANT: Ensure your Player GameObject has the tag "Player" in the Inspector!
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
            else
            {
                Debug.LogError("[EnemyController] Could not find object with tag 'Player'!");
            }
        }

        private void Update()
        {
            // Continuously update destination to chase the player
            if (playerTarget != null)
            {
                navAgent.SetDestination(playerTarget.position);
            }
        }

        // Triggered when the enemy touches something
        private void OnTriggerEnter(Collider other)
        {
            // If the enemy touches the player, the player dies instantly.
            if (other.CompareTag("Player"))
            {
                PlayerHealth pHealth = other.GetComponentInParent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.Die();
                }
            }
        }

        /// <summary>
        /// Called by the PlayerController's raycast when perfectly shot on the beat.
        /// </summary>
        public void Die()
        {
            Debug.Log("[EnemyController] Enemy killed by Player!");
            
            // Phase 3: Trigger the death splatter VFX
            if (ClockworkGearslinger.Visuals.VFXManager.Instance != null)
            {
                ClockworkGearslinger.Visuals.VFXManager.Instance.SpawnEnemyDeath(transform.position);
            }
            
            // Destroy the enemy GameObject (1 hit kill)
            Destroy(gameObject);
        }
    }
}
