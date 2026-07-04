using UnityEngine;
using UnityEngine.AI;
using ClockworkGearslinger.Core;

namespace ClockworkGearslinger.Enemies
{
    /// <summary>
    /// Spawns enemies rhythmically and a boss at a specific beat.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Spawn an enemy every X beats.")]
        [SerializeField] private int enemySpawnIntervalBeats = 4;
        
        [Tooltip("Spawn the boss exactly at this beat of the song.")]
        [SerializeField] private int bossSpawnBeat = 64;

        [Tooltip("Radius around this spawner object to randomly spawn enemies.")]
        [SerializeField] private float spawnRadius = 20f;

        private bool bossSpawned = false;
        private int lastSpawnedEnemyBeat = -1;

        private void Start()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat += HandleBeat;
            }
        }

        private void OnDestroy()
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.OnBeat -= HandleBeat;
            }
        }

        private void HandleBeat()
        {
            int currentBeat = Mathf.FloorToInt(RhythmManager.Instance.SongPositionInBeats);

            // Check if it's time to spawn the boss
            if (!bossSpawned && currentBeat >= bossSpawnBeat)
            {
                SpawnEntity(bossPrefab);
                bossSpawned = true;
                Debug.Log($"[EnemySpawner] Boss spawned at beat {currentBeat}!");
            }

            // Check if it's time to spawn a regular enemy
            if (currentBeat > lastSpawnedEnemyBeat && currentBeat % enemySpawnIntervalBeats == 0)
            {
                SpawnEntity(enemyPrefab);
                lastSpawnedEnemyBeat = currentBeat;
            }
        }

        private void SpawnEntity(GameObject prefab)
        {
            if (prefab == null) return;

            Vector3 randomPoint = GetRandomPointOnNavMesh();
            Instantiate(prefab, randomPoint, Quaternion.identity);
        }

        private Vector3 GetRandomPointOnNavMesh()
        {
            // Try multiple times to find a valid spot on the NavMesh
            for (int i = 0; i < 30; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Sample position to ensure the target is on the NavMesh
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // Fallback to spawner's exact position if we couldn't find a spot
            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
