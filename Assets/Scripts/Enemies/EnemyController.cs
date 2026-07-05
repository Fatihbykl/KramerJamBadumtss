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

        private Animator animator;
        
        [Header("Stats Settings")]
        [Tooltip("Maximum health of the enemy. Can be increased for bosses.")]
        [SerializeField] private int maxHealth = 1;
        private int currentHealth;

        [Header("Movement Settings")]
        [Tooltip("How far the enemy moves per beat.")]
        [SerializeField] private float moveDistance = 2f;
        [Tooltip("How fast the lerp is when moving.")]
        [SerializeField] private float moveDuration = 0.1f;
        [Tooltip("The enemy will move every N beats (1 = every beat, 2 = every other beat).")]
        [SerializeField] private int beatsPerMove = 1;

        private int beatCounter = 0;
        private bool isMoving = false;

        private void Start()
        {
            currentHealth = maxHealth;
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();

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
            beatCounter++;
            if (beatCounter >= beatsPerMove)
            {
                beatCounter = 0;
                if (playerTarget != null && !isMoving)
                {
                    navAgent.nextPosition = transform.position; // Sync agent start
                    navAgent.SetDestination(playerTarget.position);
                    StartCoroutine(StepToTarget());
                }
            }
        }

        private IEnumerator StepToTarget()
        {
            isMoving = true;
            animator.SetTrigger("Lunge");

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
            
            animator.SetTrigger("Stop");
            isMoving = false;
        }

        [Header("Combat Settings")]
        [Tooltip("If the enemy gets within this distance of the player, the player dies.")]
        [SerializeField] private float killDistance = 1.5f;
        private bool hasKilledPlayer = false;

        private void Update()
        {
            // Reliable distance-based kill check to bypass Unity Physics trigger quirks
            if (playerTarget != null && !hasKilledPlayer)
            {
                if (Vector3.Distance(transform.position, playerTarget.position) <= killDistance)
                {
                    PlayerHealth pHealth = playerTarget.GetComponentInParent<PlayerHealth>();
                    if (pHealth != null)
                    {
                        hasKilledPlayer = true;
                        pHealth.Die();
                    }
                }
            }
        }

        // Triggered when the enemy touches something (Fallback for physics-based collisions)
        private void OnTriggerEnter(Collider other)
        {
            if (hasKilledPlayer) return;
            
            // If the enemy touches the player, the player dies instantly.
            if (other.CompareTag("Player"))
            {
                PlayerHealth pHealth = other.GetComponentInParent<PlayerHealth>();
                if (pHealth != null)
                {
                    hasKilledPlayer = true;
                    pHealth.Die();
                }
            }
        }

        /// <summary>
        /// Called by the PlayerController's raycast when perfectly shot on the beat.
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                Debug.Log($"[EnemyController] Enemy hit! Health remaining: {currentHealth}");
            }
        }

        /// <summary>
        /// Kills the enemy and plays the death effect.
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
