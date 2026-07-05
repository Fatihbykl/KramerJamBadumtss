using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button StartButton, QuitButton;

    public TextMeshProUGUI title;

    private float timer;

    private void Start()
    {
        StartButton.onClick.AddListener(() => SceneManager.LoadScene("Game"));
        QuitButton.onClick.AddListener(() => Application.Quit());
    }

    private void Update()
    {
        timer += Time.deltaTime;
        title.color = new Color(Mathf.Lerp(180f, 255f, Mathf.Sin(timer)) / 255f, Mathf.Lerp(0f, 255f, Mathf.Sin(timer)), 0f);
    }
}
