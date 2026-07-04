using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClockworkGearslinger.Player
{
    /// <summary>
    /// Handles the Player's 1-HP system.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        /// <summary>
        /// Instantly kills the player.
        /// </summary>
        public void Die()
        {
            Debug.Log("[PlayerHealth] Player touched by Enemy! GAME OVER.");
            
            // For a Game Jam, instantly restarting the active scene is the most robust and quick way to handle game over.
            // You can replace this with a Game Over UI screen if you have time.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
