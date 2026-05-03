using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Header("Textos")]
    public TextMeshProUGUI txtScore;
    public TextMeshProUGUI txtHiScore;
    public TextMeshProUGUI txtWeapon;
    public TextMeshProUGUI txtDifficulty;

    [Header("Iconos de vidas")]
    public Image[] lifeIcons;
    public Color   lifeActiveColor   = Color.white;
    public Color   lifeInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    [Header("Pips de nivel de arma")]
    public Image[] weaponPips;
    public Color   pipActiveColor   = new Color(0.2f, 0.8f, 1f);
    public Color   pipInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    [Header("Colores por arma")]
    public Color colorNormal = new Color(1f, 0.85f, 0.1f);
    public Color colorSpread = new Color(0.2f, 1f, 0.3f);
    public Color colorLaser  = new Color(0.3f, 0.85f, 1f);
    public Color colorHoming = new Color(1f,   0.35f, 0.25f);

    [Header("Bombas")]
    public Image           bombIcon;
    public TextMeshProUGUI txtBombCount;

    [Header("Panel Game Over")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI txtGameOverTitle;
    public TextMeshProUGUI txtScoreLabel;
    public TextMeshProUGUI txtFinalScore;
    Image _gameOverBg;

    [Header("Fundido a negro en Game Over")]    
    public Image fadeImage;

    [Tooltip("Segundos que espera antes de parar el scroll y hacer fadeout")]
    public float gameOverWait = 5f;

    [Header("Game Over — Cuenta atrás / Continue")]
    [Tooltip("Cantidad de segundos de cuenta atrás para pulsar Continue")]
    public float               gameOverCountdown  = 10f;
    public TextMeshProUGUI     txtCountdown;
    public TextMeshProUGUI     txtContinuePrompt;
    public TextMeshProUGUI     txtContinuesLeft;

    [Header("Timer de la fase")]
    public bool showTimer = true;
    public TextMeshProUGUI txtTimer;

    [Header("FPS")]
    public TextMeshProUGUI txtFPS;

    [Header("Barra de vida del boss")]
    public UnityEngine.UI.Slider bossHealthBar;

    [Header("Debug / Test (F1 para activar)")]    
    public GameObject hudGroup;

    [Header("Kamikaze Warning")]
    public GameObject warningPanel;

    [Header("Boss Warning")]
    public GameObject      bossWarningPanel;
    public TextMeshProUGUI txtBossWarning;

    [Header("Pausa")]
    public GameObject      pausePanel;
    public TextMeshProUGUI txtPause;

    Coroutine _scorePopRoutine;
    Coroutine _pauseBlinkRoutine;
    Coroutine _bossWarningRoutine;
    float _phaseTime;
    bool  _timerRunning;
    float _fpsTimer;
    int   _fpsFrames;
    float _fpsDisplay;
    bool  _debugVisible;

    void Start()
    {
        UpdateScore(0, 0);
        UpdateHiScore(PlayerPrefs.GetInt("HiScore", 0));
        UpdateLives(GameSettings.StartingLives);
        UpdateBombs(PowerUpManager.Instance?.BombCount ?? 0);
        UpdateWeapon("NORMAL", 1);
        if (txtDifficulty != null) txtDifficulty.text = GameSettings.DifficultyName;
        if (gameOverPanel     != null) gameOverPanel.SetActive(false);
        if (warningPanel      != null) warningPanel.SetActive(false);
        if (bossWarningPanel  != null) bossWarningPanel.SetActive(false);
        if (pausePanel        != null) pausePanel.SetActive(false);
        if (bossHealthBar     != null) bossHealthBar.gameObject.SetActive(false);
        ResetTimer();
        StartTimer();

        // Al inicio todo oculto y F1 lo activo para tests
        _debugVisible = false;
        SetDebugHudVisible(false);
    }

    void Update()
    {
        UpdateFPS();

        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            _debugVisible = !_debugVisible;
            SetDebugHudVisible(_debugVisible);
        }

        if (!_timerRunning || !showTimer) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        _phaseTime += Time.deltaTime;
        if (txtTimer != null)
        {
            int mins = (int)_phaseTime / 60;
            int secs = (int)_phaseTime % 60;
            txtTimer.text = string.Format("{0:00}:{1:00}", mins, secs);
        }
    }

    void UpdateFPS()
    {
        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fpsDisplay = _fpsFrames / _fpsTimer;
            _fpsFrames  = 0;
            _fpsTimer   = 0f;
            if (txtFPS != null)
                txtFPS.text = $"{_fpsDisplay:F0} FPS";
        }
    }

    public void StartTimer()
    {
        _timerRunning = true;
        if (txtTimer != null) txtTimer.gameObject.SetActive(showTimer);
    }

    public void StopTimer()  => _timerRunning = false;

    public void ResetTimer()
    {
        _phaseTime = 0f;
        if (txtTimer != null) txtTimer.text = "00:00";
        if (txtTimer != null) txtTimer.gameObject.SetActive(showTimer);
    }

    public float PhaseTime => _phaseTime;

    public void UpdateScore(int total, int earned)
    {
        if (txtScore != null) txtScore.text = total.ToString("D8");
        if (earned > 0) StartScorePop();
    }

    public void UpdateHiScore(int hi)
    {
        if (txtHiScore != null) txtHiScore.text = hi.ToString("D8");
    }

    public void UpdateLives(int currentLives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            bool show = i < currentLives;
            lifeIcons[i].gameObject.SetActive(show);
            if (show) lifeIcons[i].color = lifeActiveColor;
        }
        
        // Animación en el último icono visible al perder una vida
        int lastIdx = currentLives - 1;
        if (lastIdx >= 0 && lastIdx < lifeIcons.Length && lifeIcons[lastIdx] != null)
            StartCoroutine(ScalePunch(lifeIcons[lastIdx].transform));
    }

    public void UpdateBombs(int count)
    {
        if (txtBombCount != null) txtBombCount.text = "×" + count;
        if (bombIcon     != null) bombIcon.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
    }

    public void UpdateWeapon(string weaponName, int level)
    {
        if (txtWeapon != null) { txtWeapon.text = weaponName; txtWeapon.color = GetWeaponColor(weaponName); }
        Color pipOn = GetWeaponColor(weaponName);
        for (int i = 0; i < weaponPips.Length; i++)
        {
            if (weaponPips[i] == null) continue;
            weaponPips[i].color = i < level ? pipOn : pipInactiveColor;
        }
    }

    Color GetWeaponColor(string name)
    {
        switch (name.ToUpper())
        {
            case "SPREAD":  return colorSpread;
            case "LASER":   return colorLaser;
            case "HOMING":  return colorHoming;
            case "NORMAL":  return colorNormal;
            default:        return colorNormal;
        }
    }

    public void ShowGameOver()
    {
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        // Secuencia Game Over

        // Fase 1: pantalla limpia mientras dura la explosión
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        yield return new WaitForSeconds(gameOverWait);

        // Para scroll y elimina enemigos y proyectiles de la pantalla
        FindAnyObjectByType<ScrollManager>()?.PauseScroll();
        EnemyManager em = FindAnyObjectByType<EnemyManager>();
        if (em != null) { em.StopAllCoroutines(); em.enabled = false; }
        foreach (var eh in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (eh != null && eh.gameObject.activeSelf) eh.gameObject.SetActive(false);

        // Fase 2: fundido a negro (1.5 s y cubre TODOS los layers)
        if (fadeImage != null)
        {
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / 1.5f));
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1f);
        }

        // Fase 3: muestra panel con GAME OVER + score
        EnsureGameOverPanel();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (txtGameOverTitle != null) txtGameOverTitle.text = "GAME OVER";
            if (txtScoreLabel    != null) txtScoreLabel.text    = "FINAL SCORE";
            if (txtFinalScore    != null)
                txtFinalScore.text = ScoreManager.Instance != null
                    ? ScoreManager.Instance.CurrentScore.ToString("D8")
                    : "00000000";
        }

        // Fade in del fondo
        if (_gameOverBg != null)
        {
            float ft = 0f;
            Color bgColor = new Color(0.04f, 0.04f, 0.08f, 0.92f);
            while (ft < 1f)
            {
                ft += Time.unscaledDeltaTime / 0.8f;
                _gameOverBg.color = new Color(bgColor.r, bgColor.g, bgColor.b,
                                              Mathf.Clamp01(ft) * bgColor.a);
                yield return null;
            }
            _gameOverBg.color = bgColor;
        }

        // Fase 4: Cuenta atrás con opción de Continue
        int continuesLeft = GameManager.Instance != null ? GameManager.Instance.ContinuesLeft : 0;
        bool hasContinues = continuesLeft > 0;

        if (txtContinuesLeft != null)
            txtContinuesLeft.text = hasContinues
                ? $"CONTINUES: {continuesLeft}"
                : "NO CONTINUES LEFT";

        if (txtContinuePrompt != null)
            txtContinuePrompt.text = hasContinues
                ? "PRESS SPACE / FIRE TO CONTINUE"
                : "";

        float timeLeft = gameOverCountdown;
        bool  continuePressed = false;

        while (timeLeft > 0f)
        {
            timeLeft -= Time.unscaledDeltaTime;
            int secs = Mathf.CeilToInt(Mathf.Max(timeLeft, 0f));
            if (txtCountdown != null) txtCountdown.text = secs.ToString();

            if (hasContinues && Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.zKey.wasPressedThisFrame     ||
                 Keyboard.current.enterKey.wasPressedThisFrame))
            {
                continuePressed = true;
                break;
            }

            yield return null;
        }

        if (txtCountdown != null) txtCountdown.text = "";

        if (continuePressed)
        {
            GameManager.Instance?.UseContinue();
        }
        else
        {
            GameManager.Instance?.GoToTitleScreen();
        }
    }

    void EnsureGameOverPanel()
    {
        Transform panelT;

        if (gameOverPanel != null)
        {
            panelT = gameOverPanel.transform;

            // Textos existentes por nombre
            foreach (var tmp in gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = tmp.gameObject.name;
                if      (txtGameOverTitle == null && (GOHas(n,"Title") || GOHas(n,"GameOver"))) txtGameOverTitle = tmp;
                else if (txtScoreLabel    == null && (GOHas(n,"Label") || GOHas(n,"ScoreLabel"))) txtScoreLabel = tmp;
                else if (txtFinalScore    == null && (GOHas(n,"Score") || GOHas(n,"Final") || GOHas(n,"Value"))) txtFinalScore = tmp;
            }
        }
        else
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Contenedor fullscreen
            var root = new GameObject("GameOverPanel");
            root.transform.SetParent(canvas.transform, false);
            StretchFull(root.AddComponent<RectTransform>());
            gameOverPanel = root;

            // Tarjeta centrada con fondo oscuro con mismas proporciones que el ResultPanel del StageClear
            var card   = new GameObject("Card");
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.SetParent(root.transform, false);
            cardRT.anchorMin = new Vector2(0.15f, 0.12f);
            cardRT.anchorMax = new Vector2(0.85f, 0.88f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.08f, 0f);
            _gameOverBg = bg;

            panelT = card.transform;
        }

        //  Título
        if (txtGameOverTitle == null)
            txtGameOverTitle = MakeGOTMP("TxtGameOverTitle", panelT,
                new Vector2(0f, 0.76f), new Vector2(1f, 1.00f),
                "GAME OVER", 72f, new Color(1f, 0.9f, 0.1f),
                TextAlignmentOptions.Center, FontStyles.Bold);

        // Fila SCORE
        if (txtScoreLabel == null)
            txtScoreLabel = MakeGOTMP("TxtScoreLabel", panelT,
                new Vector2(0.05f, 0.58f), new Vector2(0.50f, 0.74f),
                "FINAL SCORE", 26f, Color.white,
                TextAlignmentOptions.MidlineRight, FontStyles.Normal);

        if (txtFinalScore == null)
            txtFinalScore = MakeGOTMP("TxtFinalScore", panelT,
                new Vector2(0.52f, 0.58f), new Vector2(0.95f, 0.74f),
                "00000000", 30f, new Color(1f, 0.9f, 0.1f),
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        else
        {
            var rt = txtFinalScore.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.52f, 0.58f);
            rt.anchorMax = new Vector2(0.95f, 0.74f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            txtFinalScore.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // Fila CONTINUES
        if (txtContinuesLeft == null)
            txtContinuesLeft = MakeGOTMP("TxtContinuesLeft", panelT,
                new Vector2(0f, 0.40f), new Vector2(1f, 0.56f),
                "CONTINUES: 3", 26f, new Color(0.4f, 0.9f, 1f),
                TextAlignmentOptions.Center, FontStyles.Normal);

        // Prompt
        if (txtContinuePrompt == null)
            txtContinuePrompt = MakeGOTMP("TxtContinuePrompt", panelT,
                new Vector2(0f, 0.22f), new Vector2(1f, 0.38f),
                "PRESS SPACE / FIRE TO CONTINUE", 20f, new Color(1f, 1f, 0.5f),
                TextAlignmentOptions.Center, FontStyles.Normal);

        // Cuenta atrás
        if (txtCountdown == null)
            txtCountdown = MakeGOTMP("TxtCountdown", panelT,
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.20f),
                "10", 64f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);

        gameOverPanel.SetActive(false);
    }

    TextMeshProUGUI MakeGOTMP(string goName, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        string text, float fontSize, Color color,
        TextAlignmentOptions align, FontStyles style)
    {
        var go = new GameObject(goName);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        return tmp;
    }

    static bool GOHas(string name, string token) =>
        name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

    public void HideGameOver() => gameOverPanel?.SetActive(false);

    // Kamikaze Warning
    public void ShowKamikazeWarning()
    {
        if (warningPanel != null)
        {
            var tmp = warningPanel.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = "WARNING"; tmp.alpha = 1f; }
            warningPanel.SetActive(true);
        }
        AudioManager.Instance?.PlayWarningSFXOneShot();
    }

    public void HideKamikazeWarning()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    // Boss Warning
    public void StartBossWarning()
    {
        // Paramos todo
        if (_bossWarningRoutine != null) StopCoroutine(_bossWarningRoutine);

        // Activamos el panel y la rutina
        if (bossWarningPanel != null) bossWarningPanel.SetActive(true);
        _bossWarningRoutine = StartCoroutine(BlinkBossWarning());

        // Efecto de sonido del Warning
        AudioManager.Instance?.PlayWarningSFX();
    }

    public void StopBossWarning()
    {
        // Fin del Warning
        if (_bossWarningRoutine != null) { StopCoroutine(_bossWarningRoutine); _bossWarningRoutine = null; }

        //Hacemos desaparecer el panel
        if (txtBossWarning   != null) txtBossWarning.alpha = 1f;
        if (bossWarningPanel != null) bossWarningPanel.SetActive(false);

        // Quitamos el efecto de sonido
        AudioManager.Instance?.StopWarningSFX();
    }

    IEnumerator BlinkBossWarning()
    {
        while (true)
        {
            if (txtBossWarning != null) txtBossWarning.alpha = 1f;
            yield return new WaitForSeconds(0.25f);
            if (txtBossWarning != null) txtBossWarning.alpha = 0f;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Pausa
    public void ShowPause()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (_pauseBlinkRoutine != null) StopCoroutine(_pauseBlinkRoutine);
        if (txtPause != null) _pauseBlinkRoutine = StartCoroutine(BlinkPauseText());
    }

    public void HidePause()
    {
        if (_pauseBlinkRoutine != null) { StopCoroutine(_pauseBlinkRoutine); _pauseBlinkRoutine = null; }
        if (txtPause  != null) txtPause.alpha = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    IEnumerator BlinkPauseText()
    {
        // WaitForSecondsRealtime porque Time.timeScale = 0 durante la pausa
        while (true)
        {
            if (txtPause != null) txtPause.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.5f);
            if (txtPause != null) txtPause.alpha = 0f;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    void StartScorePop()
    {
        if (_scorePopRoutine != null) StopCoroutine(_scorePopRoutine);
        _scorePopRoutine = StartCoroutine(ScorePopRoutine());
    }

    IEnumerator ScorePopRoutine()
    {
        if (txtScore == null) yield break;
        float t = 0f;
        while (t < 0.15f)
        {
            float s = 1f + Mathf.Sin(t / 0.15f * Mathf.PI) * 0.18f;
            txtScore.transform.localScale = Vector3.one * s;
            t += Time.deltaTime;
            yield return null;
        }
        txtScore.transform.localScale = Vector3.one;
    }

    IEnumerator ScalePunch(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float t = 0f;

        // El tiempo del punch
        while (t < 0.2f)
        {
            float s = 1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.25f;
            target.localScale = originalScale * s;
            t += Time.deltaTime;
            yield return null;
        }
        target.localScale = originalScale;
    }

    // Oculta todos los elementos del HUD de juego antes para Stage Clear
    public void HideGameHUD()
    {
        if (hudGroup       != null) hudGroup.SetActive(false);
        if (txtTimer       != null) txtTimer.gameObject.SetActive(false);
        if (txtFPS         != null) txtFPS.gameObject.SetActive(false);
        if (txtScore       != null) txtScore.gameObject.SetActive(false);
        if (txtHiScore     != null) txtHiScore.gameObject.SetActive(false);
        if (txtWeapon      != null) txtWeapon.gameObject.SetActive(false);
        if (txtDifficulty  != null) txtDifficulty.gameObject.SetActive(false);
        if (bombIcon       != null) bombIcon.gameObject.SetActive(false);
        if (txtBombCount   != null) txtBombCount.gameObject.SetActive(false);
        if (bossHealthBar  != null) bossHealthBar.gameObject.SetActive(false);
        foreach (var icon in lifeIcons)
            if (icon != null) icon.gameObject.SetActive(false);
        foreach (var pip in weaponPips)
            if (pip != null) pip.gameObject.SetActive(false);
    }

    //  Debug / Test (F1)
    void SetDebugHudVisible(bool visible)
    {
        if (hudGroup  != null) hudGroup.SetActive(visible);
        if (txtTimer  != null) txtTimer.gameObject.SetActive(visible && showTimer);
        if (txtFPS    != null) txtFPS.gameObject.SetActive(visible);
    }

    // Barra de vida del boss
    public UnityEngine.UI.Slider GetOrCreateBossHealthBar()
    {
        if (bossHealthBar != null) return bossHealthBar;
        bossHealthBar = BuildBossHealthBar();
        return bossHealthBar;
    }

    UnityEngine.UI.Slider BuildBossHealthBar()
    {
        // Creamos el componente dle canvas manualmente
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        // Contenedor
        var root   = new GameObject("BossHealthBar");
        var rootRT = root.AddComponent<RectTransform>();
        root.transform.SetParent(canvas.transform, false);
        rootRT.anchorMin = new Vector2(0.1f, 0.02f);
        rootRT.anchorMax = new Vector2(0.9f, 0.075f);
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        // Fondo oscuro
        var bg    = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.08f, 0.03f, 0.03f, 0.9f);
        StretchFull(bg.GetComponent<RectTransform>());

        // Área de relleno
        var fillArea   = new GameObject("FillArea");
        fillArea.transform.SetParent(root.transform, false);
        StretchFull(fillArea.AddComponent<RectTransform>());

        // Imagen de relleno de color rojo para el boss
        var fill    = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(0.85f, 0.1f, 0.08f, 1f);
        var fillRT  = fill.GetComponent<RectTransform>();
        StretchFull(fillRT);

        // Slider sin handle visible
        var slider         = root.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect    = fillRT;
        slider.direction   = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.minValue    = 0f;
        slider.maxValue    = 1f;
        slider.value       = 1f;

        root.SetActive(false);
        return slider;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
