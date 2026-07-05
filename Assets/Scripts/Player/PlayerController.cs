using UnityEngine;
using System;
using System.Collections;
using ClockworkGearslinger.Core;
using ClockworkGearslinger.Audio;

namespace ClockworkGearslinger.Player
{
    public enum PlayerState
    {
        Normal,
        Jammed
    }

    /// <summary>
    /// Handles Grid-based movement and the core rhythmic shooting mechanics.
    /// Acts as a State Machine shifting between Normal and Jammed states.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Animator revolverAnimator;
        [SerializeField] private ParticleSystem shootParticle;
        
        [Header("Grid Movement Settings")]
        [Tooltip("How far the player moves per grid step.")]
        [SerializeField] private float moveDistance = 2f;
        [Tooltip("How fast the lerp is when moving between grid tiles.")]
        [SerializeField] private float moveDuration = 0.1f;
        
        [Tooltip("How many beats the player is prevented from moving if they miss a movement beat.")]
        [SerializeField] private int movePenaltyBeats = 1;
        
        // Core State Properties
        public PlayerState CurrentState { get; private set; } = PlayerState.Normal;
        public int BulletCount { get; private set; } = 1;
        public int ConsecutiveRecoveryHits { get; private set; } = 0;

        // Track actions separately so players can move AND shoot on the same beat
        private int lastMoveBeat = -1;
        private int lastShootBeat = -1;
        private int lastRecoveryBeat = -1;
        
        // Tracks the penalty if player misses a movement beat
        private int lockedUntilBeat = -1;

        // C# Events for UI and Polish (Phase 5)
        public event Action OnGunJammed;
        public event Action<int> OnRecoveryHit;
        public event Action OnRecoveryFailed;
        public event Action OnGunFired;
        public event Action OnMovementSuccess;

        private bool isMoving = false;

        private void Update()
        {
            HandleMovement();

            // Simple State Machine logic
            if (CurrentState == PlayerState.Normal)
            {
                HandleNormalState();
            }
            else if (CurrentState == PlayerState.Jammed)
            {
                HandleJammedState();
            }
        }

        private void HandleMovement()
        {
            // 1. Grid Movement
            if (!isMoving)
            {
                Vector3 inputDirection = Vector3.zero;

                if (Input.GetKeyDown(KeyCode.W)) inputDirection = transform.forward;
                else if (Input.GetKeyDown(KeyCode.S)) inputDirection = -transform.forward;
                else if (Input.GetKeyDown(KeyCode.A)) inputDirection = -transform.right;
                else if (Input.GetKeyDown(KeyCode.D)) inputDirection = transform.right;

                if (inputDirection != Vector3.zero)
                {
                    int currentClosestBeat = Mathf.RoundToInt(RhythmManager.Instance.SongPositionInBeats);

                    // 1a. Check if we are currently serving a movement penalty
                    if (currentClosestBeat <= lockedUntilBeat)
                    {
                        // Player is penalized and cannot move.
                        // We skip movement processing.
                    }
                    else
                    {
                        // 1b. Process movement normally
                        if (RhythmManager.Instance.IsOnBeat())
                        {
                            if (currentClosestBeat != lastMoveBeat)
                            {
                                lastMoveBeat = currentClosestBeat;
                                StartCoroutine(MoveGrid(inputDirection));
                                AudioManager.Instance.PlayMovementSound();
                                OnMovementSuccess?.Invoke();
                            }
                        }
                        else
                        {
                            // MISSED A MOVE BEAT! Apply penalty
                            lockedUntilBeat = currentClosestBeat + movePenaltyBeats;
                            Debug.Log($"[PlayerController] MOVE FAILED! Movement locked until beat {lockedUntilBeat}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Phase 3: Handles standard gameplay. Shoot on the beat.
        /// </summary>
        private void HandleNormalState()
        {
            // 2. Firing
            if (Input.GetMouseButtonDown(0)) // Left Mouse Button
            {
                if (RhythmManager.Instance.IsOnBeat())
                {
                    int currentBeat = Mathf.RoundToInt(RhythmManager.Instance.SongPositionInBeats);
                    if (currentBeat != lastShootBeat)
                    {
                        lastShootBeat = currentBeat;
                        //if (BulletCount > 0)
                        //{
                            FireWeapon();
                        //}
                    }
                }
                else
                {
                    // MISSED THE BEAT! Trigger the punishing Jam mechanic.
                    EnterJammedState();
                }
            }
        }

        private void FireWeapon()
        {
            BulletCount--;
            Debug.Log("[PlayerController] BANG! Perfect beat hit.");
            
            // Play random shoot sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRandomShootSound();
            }

            // Fire event for UI crosshair flash, screen shake
            OnGunFired?.Invoke(); 
            shootParticle.Play();
            if (revolverAnimator != null) revolverAnimator.SetTrigger("Shoot");

            // Perform Hitscan (Raycast) from the main camera's center
            if (Camera.main != null)
            {
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                
                // Shoot a ray 100 units forward
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    // Did we hit an enemy?
                    var enemy = hit.collider.GetComponentInParent<ClockworkGearslinger.Enemies.EnemyController>();
                    if (enemy != null)
                    {
                        enemy.Die();
                    }
                }
            }
        }

        private void EnterJammedState()
        {
            CurrentState = PlayerState.Jammed;
            ConsecutiveRecoveryHits = 0; // Reset counter
            
            Debug.Log("[PlayerController] GUN JAMMED! PUNISHED STATE ENTERED.");
            
            // Phase 4: Instantly cut the guitars for dynamic audio punishment
            if (AudioManager.Instance != null)
                AudioManager.Instance.MuteGuitars();

            // Trigger UI "ONE MORE TIME!" flash
            OnGunJammed?.Invoke();
        }

        /// <summary>
        /// Phase 4: Forces the player to hit 3 consecutive beats to reload and recover.
        /// </summary>
        private void HandleJammedState()
        {
            // Use Left Click to attempt to un-jam the weapon
            if (Input.GetMouseButtonDown(0))
            {
                if (RhythmManager.Instance.IsOnBeat())
                {
                    int currentBeat = Mathf.RoundToInt(RhythmManager.Instance.SongPositionInBeats);
                    if (currentBeat != lastRecoveryBeat)
                    {
                        lastRecoveryBeat = currentBeat;
                        ConsecutiveRecoveryHits++;
                        revolverAnimator.SetTrigger("Reload");
                        Debug.Log($"[PlayerController] Recovery Hit {ConsecutiveRecoveryHits}/3");
                        
                        // Update UI pips
                        OnRecoveryHit?.Invoke(ConsecutiveRecoveryHits);

                        // Successfully recovered!
                        if (ConsecutiveRecoveryHits >= 3)
                        {
                            RecoverFromJam();
                        }
                    }
                }
                else
                {
                    // MISSED A BEAT DURING RECOVERY! Reset entirely.
                    ConsecutiveRecoveryHits = 0;
                    Debug.Log("[PlayerController] Recovery sequence broken! Back to 0.");
                    
                    // Trigger UI error flash
                    OnRecoveryFailed?.Invoke();
                }
            }
        }

        private void RecoverFromJam()
        {
            Debug.Log("[PlayerController] JAM CLEARED! READY TO ROCK!");
            
            BulletCount = 1; // Reload the 1 bullet
            CurrentState = PlayerState.Normal;
            ConsecutiveRecoveryHits = 0;
            
            // Phase 4: Re-introduce the heavy guitars as a musical reward
            if (AudioManager.Instance != null)
                AudioManager.Instance.UnmuteGuitars();
        }

        /// <summary>
        /// Simple coroutine to smoothly lerp the player from tile to tile.
        /// </summary>
        private IEnumerator MoveGrid(Vector3 direction)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + (direction * moveDistance);
            float elapsedTime = 0f;

            // In a real jam setting, you might want to raycast forward here first 
            // to ensure 'targetPos' isn't inside a wall before moving.

            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            isMoving = false;
        }
    }
}
