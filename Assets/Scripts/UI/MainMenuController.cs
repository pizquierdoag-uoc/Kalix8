using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI hiScoreText;
    public GameObject      optionsPanel;

    [Header("Botones del menú principal")]
    public Button btnPlay;
    public Button btnOptions;
    public Button btnQuit;

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
        SetMainButtonsInteractable(false);
    }

    public void CloseOptions()
    {
        PlayButtonSound();
        optionsPanel?.SetActive(false);
        SetMainButtonsInteractable(true);
    }

    void SetMainButtonsInteractable(bool interactable)
    {
        if (btnPlay    != null) btnPlay.interactable    = interactable;
        if (btnOptions != null) btnOptions.interactable = interactable;
        if (btnQuit    != null) btnQuit.interactable    = interactable;
    }

    public void QuitGame()
    {
        PlayButtonSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlayButtonSound() => AudioManager.Instance?.PlaySFX("menu_confirm");
}
