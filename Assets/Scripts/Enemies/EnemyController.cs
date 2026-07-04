using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using ClockworkGearslinger.Player;
using ClockworkGearslinger.Core;

namespace ClockworkGearslinger.Enemies
{
    /// <summary>
    /// Basic NavMesh enemy. Chases the player step-by-step on the beat.
    /// Has 1 HP. Kills player instantly on touch.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        private NavMeshAgent navAgent;
        private Transform playerTarget;
        
        [Header("Movement Settings")]
        [Tooltip("How far the enemy moves per beat.")]
        [SerializeField] private float moveDistance = 2f;
        [Tooltip("How fast the lerp is when moving.")]
        [SerializeField] private float moveDuration = 0.1f;

        private bool isMoving = false;

        private void Start()
        {
            navAgent = GetComponent<NavMeshAgent>();
            
            // Disable agent's automatic movement update to move step by step manually
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
            navAgent.isStopped = true; // Stop continuous movement
            
            // Find the player automatically by tag. 
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
            else
            {
                Debug.LogError("[EnemyController] Could not find object with tag 'Player'!");
            }
            
            // Subscribe to the rhythm manager's beat event
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat += MoveOnBeat;
            }
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat -= MoveOnBeat;
            }
        }

        private void MoveOnBeat()
        {
            if (playerTarget != null && !isMoving)
            {
                navAgent.nextPosition = transform.position; // Sync agent start
                navAgent.SetDestination(playerTarget.position);
                StartCoroutine(StepToTarget());
            }
        }

        private IEnumerator StepToTarget()
        {
            isMoving = true;
            
            // Wait one frame to let NavMeshAgent calculate the path
            yield return null;
            
            if (navAgent.path.corners.Length > 1)
            {
                Vector3 startPos = transform.position;
                Vector3 nextCorner = navAgent.path.corners[1];
                
                Vector3 direction = (nextCorner - startPos);
                direction.y = 0; // Keep horizontal
                
                if (direction.sqrMagnitude > 0.001f)
                {
                    direction.Normalize();
                    
                    Vector3 targetPos = startPos + (direction * moveDistance);
                    
                    // Sample position to ensure the target stays on the NavMesh
                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, moveDistance, NavMesh.AllAreas))
                    {
                        targetPos = hit.position;
                    }

                    // Smoothly rotate to face the direction
                    transform.rotation = Quaternion.LookRotation(direction);

                    float elapsedTime = 0f;
                    while (elapsedTime < moveDuration)
                    {
                        transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    transform.position = targetPos;
                    navAgent.nextPosition = targetPos; // Keep agent synced with visual position
                }
            }
            
            isMoving = false;
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
            
            // Trigger the death splatter VFX
            if (ClockworkGearslinger.Visuals.VFXManager.Instance != null)
            {
                ClockworkGearslinger.Visuals.VFXManager.Instance.SpawnEnemyDeath(transform.position);
            }
            
            // Destroy the enemy GameObject (1 hit kill)
            Destroy(gameObject);
        }
    }
}
