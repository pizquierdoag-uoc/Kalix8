using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Textos")]
    public TextMeshProUGUI txtScore;
    public TextMeshProUGUI txtHiScore;
    public TextMeshProUGUI txtWeapon;

    [Header("Iconos de vidas")]
    public Image[] lifeIcons;
    public Color   lifeActiveColor   = new Color(0.27f, 0.67f, 1f);
    public Color   lifeInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    [Header("Pips de nivel de arma")]
    public Image[] weaponPips;
    public Color   pipActiveColor   = new Color(0.2f, 0.8f, 1f);
    public Color   pipInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    [Header("Colores por arma")]
    public Color colorNormal = new Color(0.2f, 0.8f,  1f);
    public Color colorSpread = new Color(0.8f, 0.4f,  1f);
    public Color colorLaser  = new Color(1f,   0.65f, 0f);
    public Color colorHoming = new Color(1f,   0.35f, 0.25f);

    [Header("Bombas")]
    public Image           bombIcon;
    public TextMeshProUGUI txtBombCount;

    [Header("Panel Game Over")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI txtFinalScore;

    [Header("Fundido a negro (Game Over)")]
    [Tooltip("Arrastra aquí la misma Image de fundido que usa StageClearManager")]
    public Image fadeImage;

    [Tooltip("Segundos que espera antes de parar el scroll y hacer fadeout")]
    public float gameOverWait = 5f;

    [Header("Timer de fase")]
    public bool showTimer = true;
    public TextMeshProUGUI txtTimer;

    [Header("FPS")]
    public TextMeshProUGUI txtFPS;

    Coroutine _scorePopRoutine;
    float _phaseTime;
    bool  _timerRunning;
    float _fpsTimer;
    int   _fpsFrames;
    float _fpsDisplay;

    void Start()
    {
        UpdateScore(0, 0);
        UpdateHiScore(PlayerPrefs.GetInt("HiScore", 0));
        UpdateLives(GameSettings.StartingLives);
        UpdateBombs(PowerUpManager.Instance?.BombCount ?? 0);
        UpdateWeapon("NORMAL", 0);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        ResetTimer();
        StartTimer();
    }

    void Update()
    {
        UpdateFPS();

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
        // Punch en el último icono visible al perder una vida
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
            weaponPips[i].color = i <= level ? pipOn : pipInactiveColor;
        }
    }

    Color GetWeaponColor(string name)
    {
        switch (name.ToUpper())
        {
            case "SPREAD":  return colorSpread;
            case "LASER":   return colorLaser;
            case "HOMING":  return colorHoming;
            default:        return colorNormal;
        }
    }

    public void ShowGameOver()
    {
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        // Fade image empieza transparente (pantalla limpia mientras se ve la explosión)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        // Espera gameOverWait segundos (explosión visible + pausa dramática)
        yield return new WaitForSeconds(gameOverWait);

        // Para el scroll y elimina enemigos antes de mostrar el panel
        FindAnyObjectByType<ScrollManager>()?.PauseScroll();
        EnemyManager em = FindAnyObjectByType<EnemyManager>();
        if (em != null) { em.StopAllCoroutines(); em.enabled = false; }
        foreach (var eh in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (eh != null && eh.gameObject.activeSelf) eh.gameObject.SetActive(false);

        // Ahora sí muestra el panel Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (txtFinalScore != null)
                txtFinalScore.text = ScoreManager.Instance != null
                    ? ScoreManager.Instance.CurrentScore.ToString("D8")
                    : "00000000";
        }

        // Pausa para que el jugador lea el resultado
        yield return new WaitForSeconds(3f);

        // Fundido a negro
        if (fadeImage != null)
        {
            float t = 0f;
            while (t < 2f)
            {
                t += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / 2f));
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1f);
        }

        // Vuelve a la pantalla de título
        GameManager.Instance?.GoToTitleScreen();
    }

    public void HideGameOver() => gameOverPanel?.SetActive(false);

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
        while (t < 0.2f)
        {
            float s = 1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.25f;
            target.localScale = originalScale * s;
            t += Time.deltaTime;
            yield return null;
        }
        target.localScale = originalScale;
    }
}
