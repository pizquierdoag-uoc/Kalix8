using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    public int  CurrentLives   { get; private set; }
    public int  CurrentScore   { get; private set; }
    public int  ContinuesLeft  { get; private set; }
    public bool IsPlaying      => CurrentState == GameState.Playing;

    // Cooldown para evitar doble-detección del botón Start con timeScale=0
    float _pauseCooldown;

    protected override void Awake()
    {
        base.Awake();
        // Suscribimos a sceneLoaded para detectar cambios de escena cada vez,
        // no solo en el Start() inicial (GameManager es DontDestroyOnLoad).
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")          ChangeState(GameState.Playing);
        else if (scene.name == "MainMenu") ChangeState(GameState.MainMenu);
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
                // El juego sigue corriendo (timeScale=1): enemigos, scroll y balas
                // continúan. Solo el jugador está muerto. Al continuar reaparece.
                SaveHiScore();
                AudioManager.Instance?.PlayGameOverMusic();
                HUDController.Instance?.ShowGameOver();
                break;
        }        
    }

    public void StartGame()
    {
        // No cambiamos el estado aquí: Start() detectará la escena "Game"
        // y llamará a ChangeState(Playing) tras cargarse, evitando doble
        // inicialización y que la música de juego empiece todavía en MainMenu.
        SceneManager.LoadScene("Game");
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
            ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            AudioManager.Instance?.ResumeMusic();
            HUDController.Instance?.HidePause();
        }
    }

    public void GoToMainMenu()
    {
        // Restaura el contador de vidas al valor inicial para que cualquier código
        // que acceda a CurrentLives en el menú no lea un valor residual de 0.
        CurrentLives = GameSettings.StartingLives;

        // Limpia el estado interno de los singletons persistentes que acumulan
        // referencias a GameObjects de la escena de juego.
        PowerUpManager.Instance?.ResetOnMenuReturn();

        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToTitleScreen()
    {
        // Mismo saneamiento que GoToMainMenu para evitar referencias colgantes.
        CurrentLives = GameSettings.StartingLives;
        PowerUpManager.Instance?.ResetOnMenuReturn();

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

        AudioManager.Instance?.PlayGameMusic();
        FindAnyObjectByType<ScrollManager>()?.ResumeScroll();  // seguridad: limpia IsPaused

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
        int saved = PlayerPrefs.GetInt("HiScore", 0);
        if (current > saved) { PlayerPrefs.SetInt("HiScore", current); PlayerPrefs.Save(); }
    }

    void Update()
    {
        // unscaledDeltaTime funciona aunque timeScale = 0
        _pauseCooldown -= Time.unscaledDeltaTime;

        bool pausePressed = _pauseCooldown <= 0f
                         && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                          || (Gamepad.current  != null && Gamepad.current.startButton.wasPressedThisFrame));

        if (pausePressed)
        {
            _pauseCooldown = 0.3f;   // 300 ms de margen para evitar doble disparo

            if (CurrentState == GameState.Playing)
            {
                // Desactivamos el EventSystem para que el mando no interactúe
                // con la UI mientras el juego está en marcha
                if (EventSystem.current != null) EventSystem.current.enabled = false;
                PauseGame();
                if (EventSystem.current != null) EventSystem.current.enabled = true;
            }
            else if (CurrentState == GameState.Paused)
            {
                // Desactivamos el EventSystem durante el frame de reanudación
                // para que el botón Start no sea captado también como Submit UI
                if (EventSystem.current != null) EventSystem.current.enabled = false;
                ResumeGame();
                if (EventSystem.current != null) EventSystem.current.enabled = true;
            }
        }
    }
}
