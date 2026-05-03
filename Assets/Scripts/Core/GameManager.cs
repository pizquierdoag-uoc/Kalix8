using System.Collections;
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
    public int  CurrentLives   { get; private set; }
    public int  CurrentScore   { get; private set; }
    public int  ContinuesLeft  { get; private set; }
    public bool IsPlaying      => CurrentState == GameState.Playing;

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Game")         ChangeState(GameState.Playing);
        else if (scene == "MainMenu") ChangeState(GameState.MainMenu);
        // TitleScreen: el TitleScreenController gestiona la música; GameManager no interviene
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
                CurrentLives  = GameSettings.StartingLives;
                ContinuesLeft = GameSettings.StartingContinues;
                CurrentScore  = 0;
                ScoreManager.Instance?.ResetScore();
                Time.timeScale = 1f;
                AudioManager.Instance?.PlayGameMusic();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                AudioManager.Instance?.PauseMusic();
                HUDController.Instance?.ShowPause();
                break;

            case GameState.GameOver:
                Time.timeScale = 1f;
                // Guardo marcador
                SaveHiScore();
                // Ponemos música Game Over
                AudioManager.Instance?.PlayGameOverMusic();
                HUDController.Instance?.ShowGameOver();                
                break;
        }        
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing); // Estado playing
        SceneManager.LoadScene("Game"); // Cargamos Game
    }

    public void PauseGame()
    {
        // Sólo para estdo "playing"
        if (CurrentState == GameState.Playing) 
            ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        // Sólo si estamos en Paused
        if (CurrentState == GameState.Paused)
        {            
            CurrentState = GameState.Playing;       // Cambiamos estado
            Time.timeScale = 1f;
            AudioManager.Instance?.ResumeMusic();   // Activa música
            HUDController.Instance?.HidePause();    // Escondemos PAUSE
        }
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

        if (CurrentLives <= 0)
            ChangeState(GameState.GameOver);
        else
            StartCoroutine(RespawnAfterDelay(1.5f));
    }

    public void UseContinue()
    {
        if (ContinuesLeft <= 0) return;
        ContinuesLeft--;
        CurrentLives = GameSettings.StartingLives;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        AudioManager.Instance?.PlayGameMusic();
        FindAnyObjectByType<ScrollManager>()?.ResumeScroll();

        EnemyManager em = FindAnyObjectByType<EnemyManager>();
        if (em != null) em.enabled = true;

        HUDController.Instance?.UpdateLives(CurrentLives);
        HUDController.Instance?.HideGameOver();

        PowerUpManager.Instance?.ResetBombs(GameSettings.StartingBombs);
        HUDController.Instance?.UpdateBombs(GameSettings.StartingBombs);

        StartCoroutine(ContinueRespawn());
    }

    IEnumerator ContinueRespawn()
    {
        yield return new WaitForSeconds(0.5f);
        var players = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (players.Length > 0)
        {
            players[0].gameObject.SetActive(true);
            PowerUpManager.Instance?.ResetSpeedBoost(players[0]);
            players[0].Respawn(new Vector2(-6f, 0f));
            players[0].GetComponent<WeaponSystem>()?.SetWeapon(WeaponSystem.WeaponType.Normal);
        }
        HUDController.Instance?.UpdateWeapon("NORMAL", 1);
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Include inactive: el FrameAnimator de la explosión puede haber desactivado el GO
        var players = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (players.Length > 0)
        {
            players[0].gameObject.SetActive(true);
            PowerUpManager.Instance?.ResetSpeedBoost(players[0]);
            players[0].Respawn(new Vector2(-6f, 0f));
        }
    }

    public void AddScore(int points)
    {
        CurrentScore += points;        
    }

    void SaveHiScore()
    {
        int current = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : CurrentScore;
        int saved   = PlayerPrefs.GetInt("HiScore", 0);
        // Si es mayor que el HI SCORE actuala lo ponemos como HI SCORE
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
