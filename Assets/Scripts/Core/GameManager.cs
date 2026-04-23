using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    // Las vidas iniciales se leen de GameSettings (se cambian desde Opciones)
    public int  CurrentLives { get; private set; }
    public int  CurrentScore { get; private set; }
    public bool IsPlaying    => CurrentState == GameState.Playing;

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        ChangeState(scene == "Game" ? GameState.Playing : GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                AudioManager.Instance?.PlayMenuMusic();
                break;

            case GameState.Playing:
                CurrentLives = GameSettings.StartingLives;
                CurrentScore = 0;
                Time.timeScale = 1f;
                AudioManager.Instance?.PlayGameMusic();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                SaveHiScore();
                AudioManager.Instance?.PlayGameOverMusic();
                HUDController.Instance?.ShowGameOver();
                Debug.Log("GAME OVER — Score: " + CurrentScore);
                break;
        }

        Debug.Log("GameState → " + newState);
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing);
        SceneManager.LoadScene("Game");
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToTitleScreen()
    {
        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("TitleScreen");
    }

    public void PlayerDied()
    {
        CurrentLives--;
        HUDController.Instance?.UpdateLives(CurrentLives);
        ScoreManager.Instance?.SaveHiScore();
        Debug.Log("Vidas restantes: " + CurrentLives);

        if (CurrentLives <= 0)
            ChangeState(GameState.GameOver);
        else
            RespawnPlayer();
    }

    void RespawnPlayer()
    {        
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.Respawn(new Vector2(-6f, 0f));
    }

    public void AddScore(int points)
    {
        CurrentScore += points;
        Debug.Log("Score: " + CurrentScore);
    }

    void SaveHiScore()
    {
        int current = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : CurrentScore;
        int saved   = PlayerPrefs.GetInt("HiScore", 0);
        if (current > saved) { PlayerPrefs.SetInt("HiScore", current); PlayerPrefs.Save(); }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing)       PauseGame();
            else if (CurrentState == GameState.Paused)   ResumeGame();
        }
    }
}
