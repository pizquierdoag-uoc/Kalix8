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

    [Header("Panel de resultados (en el Canvas)")]
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

    [Header("Overlay inmediato (se muestra al matar al boss)")]
    [Tooltip("Panel/texto que aparece nada más morir el boss. Opcional.")]
    public GameObject stageClearMessage;

    [Header("Tiempos (segundos)")]
    public float fadeDuration       = 2f;
    public float countDuration      = 2.5f;
    public float delayBeforeShow    = 0.5f;
    public float waitBeforeStop     = 5f;   // segundos entre panel y parar scroll

    [Header("Bonus")]
    public int bonusPerLife  = 10000;
    public int maxTimeBonus  = 50000;
    public float maxBonusTime = 120f; // segundos — si tardas menos gets max bonus

    float _stageStartTime;
    bool  _triggered;

    void Start()
    {
        _stageStartTime = Time.realtimeSinceStartup;

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

    public void TriggerStageClear()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(StageClearSequence());
    }

    IEnumerator StageClearSequence()
    {
        if (stageClearMessage != null) stageClearMessage.SetActive(true);

        yield return new WaitForSeconds(waitBeforeStop);

        ScrollManager sm = FindAnyObjectByType<ScrollManager>();
        sm?.PauseScroll();
        StopAllEnemies();

        if (stageClearMessage != null) stageClearMessage.SetActive(false);

        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(FadeToBlack());

        yield return new WaitForSeconds(delayBeforeShow);

        if (resultPanel != null) resultPanel.SetActive(true);

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

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1f);
    }

    IEnumerator AnimateResults()
    {
        int baseScore  = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int lives      = GameManager.Instance   != null ? GameManager.Instance.CurrentLives  : 0;
        int livesBonus = lives * bonusPerLife;

        float elapsed   = Time.realtimeSinceStartup - _stageStartTime;
        float timeRatio = Mathf.Clamp01(1f - (elapsed / maxBonusTime));
        int   timeBonus = Mathf.RoundToInt(maxTimeBonus * timeRatio);
        int   total     = baseScore + livesBonus + timeBonus;

        // Muestra STAGE CLEAR
        if (txtStageClear != null)
        {
            txtStageClear.text  = "STAGE CLEAR";
            txtStageClear.color = new Color(1f, 0.9f, 0.1f);
        }

        yield return new WaitForSeconds(0.6f);

        // Inicializa todos los valores a cero antes de animar
        if (txtScoreValue      != null) txtScoreValue.text      = "00000000";
        if (txtLivesBonusValue != null) txtLivesBonusValue.text = "00000000";
        if (txtTimeBonusValue  != null) txtTimeBonusValue.text  = "00000000";
        if (txtTotalValue      != null) txtTotalValue.text      = "00000000";

        // Muestra etiquetas
        if (txtScoreLabel  != null) txtScoreLabel.text  = "SCORE";
        if (txtLivesBonus  != null) txtLivesBonus.text  = "LIVES BONUS  x" + lives;
        if (txtTimeBonus   != null) txtTimeBonus.text   = "TIME BONUS";
        if (txtTotalLabel  != null) txtTotalLabel.text  = "TOTAL";

        yield return new WaitForSeconds(0.3f);

        // Cuenta animada — score base
        yield return StartCoroutine(CountUp(txtScoreValue, 0, baseScore, countDuration * 0.4f));
        yield return new WaitForSeconds(0.2f);

        // Cuenta animada — lives bonus
        yield return StartCoroutine(CountUp(txtLivesBonusValue, 0, livesBonus, countDuration * 0.3f));
        yield return new WaitForSeconds(0.2f);

        // Cuenta animada — time bonus
        yield return StartCoroutine(CountUp(txtTimeBonusValue, 0, timeBonus, countDuration * 0.3f));
        yield return new WaitForSeconds(0.4f);

        // Cuenta animada — total (más dramático)
        if (txtTotalValue != null) txtTotalValue.color = new Color(1f, 0.9f, 0.1f);
        yield return StartCoroutine(CountUp(txtTotalValue, 0, total, countDuration * 0.5f));

        // Guarda el hi-score final
        ScoreManager.Instance?.SaveHiScore();

        // Sonido de victoria
        AudioManager.Instance?.PlayVictoryMusic();
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
        GameManager.Instance?.GoToMainMenu();
    }
}
