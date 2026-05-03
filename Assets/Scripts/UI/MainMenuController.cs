using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI hiScoreText;
    public GameObject      optionsPanel;

    void Start()
    {
        int hiScore = PlayerPrefs.GetInt("HiScore", 0);
        if (hiScoreText != null)
            hiScoreText.text = "HI-SCORE  " + hiScore.ToString("D8");

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // La música la gestiona AudioManager no se duplica con una fuente propia
        AudioManager.Instance?.PlayMenuMusic();
    }

    public void PlayGame()
    {
        PlayButtonSound();
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
        else
            SceneManager.LoadScene("Game");
    }

    public void OpenOptions()
    {
        PlayButtonSound();
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayButtonSound();
        optionsPanel?.SetActive(false);
    }

    public void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
    }

    void PlayButtonSound() => AudioManager.Instance?.PlaySFX("menu_confirm");
}
