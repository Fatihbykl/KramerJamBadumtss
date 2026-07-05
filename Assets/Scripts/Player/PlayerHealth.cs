using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClockworkGearslinger.Player
{
    /// <summary>
    /// Handles the Player's 1-HP system.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        private bool isDead = false;

        /// <summary>
        /// Kills the player, shows game over state, and restarts the level.
        /// </summary>
        public void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("[PlayerHealth] Player touched by Enemy! GAME OVER.");
            
            // Show Game Over UI
            var uiManager = FindObjectOfType<ClockworkGearslinger.UI.UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowGameOver();
            }

            // Disable player input
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            // Restart the level after 3 seconds so the player can see what killed them
            Invoke(nameof(RestartScene), 3f);
        }

        private void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
