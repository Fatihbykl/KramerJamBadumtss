using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button StartButton, QuitButton;

    private void Start()
    {
        StartButton.onClick.AddListener(() => SceneManager.LoadScene("Game"));
        QuitButton.onClick.AddListener(() => Application.Quit());
    }
}
