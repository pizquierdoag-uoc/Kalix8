using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageClearManager : MonoBehaviour
{
    public static StageClearManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Panel de resultados (Canvas)")]
    public GameObject resultPanel;

    [Header("Textos del resultado")]
    public TextMeshProUGUI txtStageClear;
    public TextMeshProUGUI txtScoreLabel;
    public TextMeshProUGUI txtScoreValue;
    public TextMeshProUGUI txtLivesBonus;
    public TextMeshProUGUI txtLivesBonusValue;
    public TextMeshProUGUI txtTimeBonus;
    public TextMeshProUGUI txtTimeBonusValue;
    public TextMeshProUGUI txtTotalLabel;
    public TextMeshProUGUI txtTotalValue;

    [Header("Botón continuar")]
    public Button btnContinue;

    [Header("Fundido a negro")]
    public Image fadeImage;  // Image negro que cubre toda la pantalla

    [Header("Texto al matar al boss)")]    
    public GameObject stageClearMessage;

    [Header("Tiempos (segundos)")]
    public float fadeDuration          = 2f;
    public float countDuration         = 2.5f;
    public float delayBeforeShow       = 0.5f;
    public float waitBeforeStop        = 5f;   // segundos entre Stage Clear y parar scroll
    public float bossExplosionDuration = 3f;   // espera la explosión antes de Stage Clear

    [Header("Bonus")]
    public int   bonusPerLife    = 10000; // puntos por vida restante
    public float maxBonusTime    = 500f;  // segundos límite para el bonus de tiempo
    public float pointsPerSecond = 1000f; // puntos por segundo restante: (maxBonusTime - elapsed) * pointsPerSecond

    float _stageStartTime;
    bool  _triggered;
    int   _capturedScore;
    int   _capturedLives;

    void Start()
    {
        _stageStartTime = Time.realtimeSinceStartup;

        AutoWireComponents();

        // Oculta el panel al inicio
        if (resultPanel != null) resultPanel.SetActive(false);

        // Fade image empieza transparente
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(true);
        }

        // Conecta botón
        if (btnContinue != null)
            btnContinue.onClick.AddListener(GoToMainMenu);
    }

    // Busca automáticamente los componentes TMP por nombre si no están asignados en Inspector
    void AutoWireComponents()
    {
        if (resultPanel == null) return;

        var allTMP = resultPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allTMP)
        {
            string n = t.gameObject.name;
            if (txtStageClear      == null && Has(n, "StageClear"))                          txtStageClear      = t;
            if (txtScoreLabel      == null && Has(n, "ScoreLabel"))                          txtScoreLabel      = t;
            if (txtScoreValue      == null && Has(n, "ScoreValue"))                          txtScoreValue      = t;
            bool isLive = Has(n, "LiveBonus") || Has(n, "LivesBonus");
            if (txtLivesBonus      == null && isLive && !Has(n, "Value"))                    txtLivesBonus      = t;
            if (txtLivesBonusValue == null && isLive &&  Has(n, "Value"))                    txtLivesBonusValue = t;
            if (txtTimeBonus       == null && Has(n, "TimeBonus") && !Has(n, "Value"))       txtTimeBonus       = t;
            if (txtTimeBonusValue  == null && Has(n, "TimeBonus") &&  Has(n, "Value"))       txtTimeBonusValue  = t;
            if (txtTotalLabel      == null && Has(n, "TotalLabel"))                          txtTotalLabel      = t;
            if (txtTotalValue      == null && Has(n, "TotalValue"))                          txtTotalValue      = t;
        }

        if (btnContinue == null)
            btnContinue = resultPanel.GetComponentInChildren<Button>(true);

        Debug.Log($"[StageClear] AutoWire — " +
                  $"StageClear={txtStageClear != null} " +
                  $"ScoreLabel={txtScoreLabel != null} ScoreValue={txtScoreValue != null} " +
                  $"LivesBonus={txtLivesBonus != null} LivesBonusValue={txtLivesBonusValue != null} " +
                  $"TimeBonus={txtTimeBonus != null} TimeBonusValue={txtTimeBonusValue != null} " +
                  $"TotalLabel={txtTotalLabel != null} TotalValue={txtTotalValue != null} " +
                  $"Btn={btnContinue != null}");
    }

    static bool Has(string name, string token) =>
        name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;

    public void TriggerStageClear()
    {
        if (_triggered) return;
        _triggered = true;

        // Captura los valores en el momento exacto en que muere el boss
        bool hasScore = ScoreManager.Instance != null;
        bool hasGame  = GameManager.Instance  != null;
        _capturedScore = hasScore ? ScoreManager.Instance.CurrentScore : 0;
        _capturedLives = hasGame  ? GameManager.Instance.CurrentLives  : 0;
        Debug.Log($"[StageClear] TriggerStageClear — " +
                  $"ScoreManager={hasScore} score={_capturedScore}  " +
                  $"GameManager={hasGame} vidas={_capturedLives}");

        StartCoroutine(StageClearSequence());
    }

    IEnumerator StageClearSequence()
    {
        // Espera la explosión del boss
        yield return new WaitForSeconds(bossExplosionDuration);

        // Muestra "STAGE CLEAR" e inicia música de victoria
        if (stageClearMessage != null) stageClearMessage.SetActive(true);
        AudioManager.Instance?.PlayVictoryMusic();

        // Bloquea controles y anima la nave saliendo por la derecha
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && !player.IsDead)
            yield return StartCoroutine(player.ExitScreenRight());

        // Para el scroll y limpia enemigos
        FindAnyObjectByType<ScrollManager>()?.PauseScroll();
        StopAllEnemies();

        if (stageClearMessage != null) stageClearMessage.SetActive(false);

        // Oculta el HUD de juego
        HUDController.Instance?.HideGameHUD();

        // Fundido a negro en 3 segundos
        yield return StartCoroutine(FadeToBlack(3f));

        // Tras el fundido: desactiva el scroll (tiles de fondo) y limpia la cámara
        if (ScrollManager.Instance != null) ScrollManager.Instance.gameObject.SetActive(false);
        if (Camera.main != null)
        {
            Camera.main.clearFlags       = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor  = Color.black;
        }

        // Orden de render en el canvas: fadeImage primero, resultPanel encima
        if (fadeImage   != null) fadeImage.transform.SetAsLastSibling();

        yield return new WaitForSeconds(delayBeforeShow);

        if (resultPanel != null)
        {
            resultPanel.transform.SetAsLastSibling();
            resultPanel.SetActive(true);
        }

        yield return StartCoroutine(AnimateResults());

        if (btnContinue != null) btnContinue.gameObject.SetActive(true);
    }

    void StopAllEnemies()
    {
        // Desactiva el EnemyManager para que deje de spawnear
        EnemyManager em = FindAnyObjectByType<EnemyManager>();
        if (em != null)
        {
            em.StopAllCoroutines();
            em.enabled = false;
        }

        // Desactiva todos los enemigos que haya en pantalla
        foreach (var eh in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (eh != null && eh.gameObject.activeSelf)
                eh.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeToBlack(float duration = -1f)
    {
        if (fadeImage == null) yield break;
        float d = duration > 0f ? duration : fadeDuration;
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / d));
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1f);
    }

    IEnumerator AnimateResults()
    {
        int   baseScore  = _capturedScore;
        int   lives      = _capturedLives;
        int   livesBonus = lives * bonusPerLife;
        float elapsed    = Time.realtimeSinceStartup - _stageStartTime;
        int   timeBonus  = Mathf.Max(0, Mathf.RoundToInt((maxBonusTime - elapsed) * pointsPerSecond));
        int   total      = baseScore + livesBonus + timeBonus;

        Debug.Log($"[StageClear] score={baseScore}  vidas={lives}  livesBonus={livesBonus}  timeBonus={timeBonus}  total={total}  elapsed={elapsed:F1}s");

        // Limpia todos los textos al empezar
        if (txtScoreLabel      != null) txtScoreLabel.text      = "";
        if (txtScoreValue      != null) txtScoreValue.text      = "";
        if (txtLivesBonus      != null) txtLivesBonus.text      = "";
        if (txtLivesBonusValue != null) txtLivesBonusValue.text = "";
        if (txtTimeBonus       != null) txtTimeBonus.text       = "";
        if (txtTimeBonusValue  != null) txtTimeBonusValue.text  = "";
        if (txtTotalLabel      != null) txtTotalLabel.text      = "";
        if (txtTotalValue      != null) txtTotalValue.text      = "";

        if (txtStageClear != null)
        {
            txtStageClear.text  = "STAGE CLEAR";
            txtStageClear.color = new Color(1f, 0.9f, 0.1f);
        }

        yield return new WaitForSeconds(0.8f);

        // Arranca el sonido de conteo en bucle
        AudioManager.Instance?.PlayStageClearScore();

        // ── 1. SCORE — muestra directamente el valor obtenido ─────────────
        if (txtScoreLabel != null) txtScoreLabel.text = "SCORE";
        if (txtScoreValue != null) txtScoreValue.text = baseScore.ToString("D8");

        yield return new WaitForSeconds(0.6f);

        // ── 2. LIVES BONUS — sube bonusPerLife por vida, una por segundo ──
        if (txtLivesBonus      != null) txtLivesBonus.text      = "LIVES BONUS  x" + lives;
        if (txtLivesBonusValue != null) txtLivesBonusValue.text = 0.ToString("D8");

        for (int i = 1; i <= lives; i++)
        {
            yield return new WaitForSeconds(1f);
            if (txtLivesBonusValue != null)
                txtLivesBonusValue.text = (i * bonusPerLife).ToString("D8");
        }

        yield return new WaitForSeconds(0.4f);

        // ── 3. TIME BONUS — cuenta rápida hasta el valor final ────────────
        if (txtTimeBonus      != null) txtTimeBonus.text      = "TIME BONUS";
        if (txtTimeBonusValue != null) txtTimeBonusValue.text = 0.ToString("D8");
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(CountUp(txtTimeBonusValue, 0, timeBonus, 1.5f));

        yield return new WaitForSeconds(0.5f);

        // ── 4. TOTAL — para el sonido de conteo y cuenta en 3 segundos ──
        AudioManager.Instance?.StopStageClearScore();

        if (txtTotalLabel != null) txtTotalLabel.text = "TOTAL";
        if (txtTotalValue != null)
        {
            txtTotalValue.text  = 0.ToString("D8");
            txtTotalValue.color = new Color(1f, 0.9f, 0.1f);
        }
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(CountUp(txtTotalValue, 0, total, 3f));

        ScoreManager.Instance?.SaveHiScore();
    }

    IEnumerator CountUp(TextMeshProUGUI label, int from, int to, float duration)
    {
        if (label == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t / duration));
            label.text = current.ToString("D8");
            yield return null;
        }
        label.text = to.ToString("D8");
    }

    void GoToMainMenu()
    {
        StartCoroutine(FadeOutAndGoToMenu());
    }

    IEnumerator FadeOutAndGoToMenu()
    {
        if (btnContinue != null) btnContinue.interactable = false;

        // Para la música con fundido
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // Fundido a negro
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / fadeDuration));
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1f);
        }

        GameManager.Instance?.GoToMainMenu();
    }
}
