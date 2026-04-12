using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI hiScoreText;
    public GameObject      optionsPanel;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip   buttonSound;

    void Start()
    {
        int hiScore = PlayerPrefs.GetInt("HiScore", 0);
        if (hiScoreText != null)
            hiScoreText.text = "HI-SCORE  " + hiScore.ToString("D8");

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
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

    public void CloseOptions() => optionsPanel?.SetActive(false);

    public void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
    }

    void PlayButtonSound()
    {
        if (buttonSound != null)
            AudioSource.PlayClipAtPoint(buttonSound, Camera.main.transform.position);
    }
}
