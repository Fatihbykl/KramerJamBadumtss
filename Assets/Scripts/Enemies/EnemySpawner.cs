using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ClockworkGearslinger.Core;

namespace ClockworkGearslinger.Enemies
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave 1";
        [Tooltip("At which beat this wave should begin spawning.")]
        public int startBeat;
        [Tooltip("How many enemies to spawn in this wave.")]
        public int enemyCount = 5;
        [Tooltip("How many beats to wait between each spawn in this wave.")]
        public int spawnIntervalBeats = 4;
        [Tooltip("The specific prefab to spawn for this wave (e.g. regular enemy or boss).")]
        public GameObject enemyPrefab;
    }

    /// <summary>
    /// Spawns enemies rhythmically using an editable Wave System.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Wave Configuration")]
        [Tooltip("Define your waves here. The game finishes when the last wave is defeated.")]
        [SerializeField] private List<Wave> waves = new List<Wave>();

        [Header("Spawn Restrictions")]
        [Tooltip("Radius around this spawner object to randomly spawn enemies.")]
        [SerializeField] private float spawnRadius = 20f;
        [Tooltip("Minimum distance from the player to spawn.")]
        [SerializeField] private float minimumPlayerDistance = 10f;
        [Tooltip("Ensure enemies spawn off-screen.")]
        [SerializeField] private bool requireOffScreenSpawn = true;

        private int currentWaveIndex = 0;
        private int spawnedEnemiesInCurrentWave = 0;
        private int lastSpawnedEnemyBeat = -1;
        private bool isGameFinished = false;

        private System.Collections.Generic.List<GameObject> activeEnemies = new System.Collections.Generic.List<GameObject>();

        private Transform playerTransform;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

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
            if (isGameFinished) return;

            int currentBeat = Mathf.FloorToInt(RhythmManager.Instance.SongPositionInBeats);
            
            // Clean up destroyed enemies from the list
            activeEnemies.RemoveAll(e => e == null);

            // Check win condition
            if (currentWaveIndex >= waves.Count)
            {
                if (activeEnemies.Count == 0)
                {
                    FinishGame();
                }
                return;
            }

            Wave currentWave = waves[currentWaveIndex];

            // Wait until the beat reaches the start of the current wave
            if (currentBeat >= currentWave.startBeat)
            {
                // Only spawn if enough beats have passed since the last spawn
                if (currentBeat > lastSpawnedEnemyBeat && (currentBeat - currentWave.startBeat) % currentWave.spawnIntervalBeats == 0)
                {
                    SpawnEntity(currentWave.enemyPrefab);
                    lastSpawnedEnemyBeat = currentBeat;
                    spawnedEnemiesInCurrentWave++;

                    // If we've spawned all enemies for this wave, progress to the next wave
                    if (spawnedEnemiesInCurrentWave >= currentWave.enemyCount)
                    {
                        currentWaveIndex++;
                        spawnedEnemiesInCurrentWave = 0;
                        Debug.Log($"[EnemySpawner] Finished spawning wave {currentWaveIndex}.");
                    }
                }
            }
        }

        private void SpawnEntity(GameObject prefab)
        {
            if (prefab == null) 
            {
                Debug.LogWarning("[EnemySpawner] Attempted to spawn a null prefab. Please assign a prefab to the wave in the inspector.");
                return;
            }

            Vector3 randomPoint = GetRandomPointOnNavMesh();
            GameObject newEnemy = Instantiate(prefab, randomPoint, Quaternion.identity);
            activeEnemies.Add(newEnemy);
        }

        private void FinishGame()
        {
            isGameFinished = true;
            Debug.Log("[EnemySpawner] ALL WAVES DEFEATED! GAME FINISHED!");
            
            // Find the UIManager and trigger the finish text
            var uiManager = FindObjectOfType<ClockworkGearslinger.UI.UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowGameFinished();
            }
        }

        private Vector3 GetRandomPointOnNavMesh()
        {
            // Try multiple times to find a valid spot on the NavMesh
            for (int i = 0; i < 50; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Distance check
                if (playerTransform != null && Vector3.Distance(randomPos, playerTransform.position) < minimumPlayerDistance)
                {
                    continue;
                }

                // Off-screen check
                if (requireOffScreenSpawn && mainCamera != null)
                {
                    Vector3 viewportPoint = mainCamera.WorldToViewportPoint(randomPos);
                    bool isOnScreen = viewportPoint.z > 0 && viewportPoint.x > -0.1f && viewportPoint.x < 1.1f && viewportPoint.y > -0.1f && viewportPoint.y < 1.1f;
                    if (isOnScreen)
                    {
                        continue;
                    }
                }

                // Sample position to ensure the target is on the NavMesh
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            if (minimumPlayerDistance > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, minimumPlayerDistance);
            }
        }
    }
}
