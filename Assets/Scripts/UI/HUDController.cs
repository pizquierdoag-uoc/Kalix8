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

    [Header("Panel Game Over")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI txtFinalScore;

    [Header("Fundido a negro (Game Over)")]
    [Tooltip("Arrastra aquí la misma Image de fundido que usa StageClearManager")]
    public Image fadeImage;

    [Tooltip("Segundos que espera antes de parar el scroll y hacer fadeout")]
    public float gameOverWait = 5f;

    Coroutine _scorePopRoutine;

    void Start()
    {
        UpdateScore(0, 0);
        UpdateHiScore(PlayerPrefs.GetInt("HiScore", 0));
        UpdateLives(GameSettings.StartingLives);
        UpdateWeapon("NORMAL", 0);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

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
            lifeIcons[i].color = i < currentLives ? lifeActiveColor : lifeInactiveColor;
            if (i == currentLives) StartCoroutine(ScalePunch(lifeIcons[i].transform));
        }
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
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (txtFinalScore != null)
                txtFinalScore.text = ScoreManager.Instance != null
                    ? ScoreManager.Instance.CurrentScore.ToString("D8")
                    : "00000000";
        }

        // Asegura que la imagen de fundido empiece transparente
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        yield return new WaitForSeconds(gameOverWait);

        FindAnyObjectByType<ScrollManager>()?.PauseScroll();
        EnemyManager em = FindAnyObjectByType<EnemyManager>();
        if (em != null) { em.StopAllCoroutines(); em.enabled = false; }
        foreach (var eh in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (eh != null && eh.gameObject.activeSelf) eh.gameObject.SetActive(false);

        if (fadeImage != null)
        {
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(t / 2f));
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1f);
        }

        // El jugador puede pulsar el botón para ir al menú
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
        float t = 0f;
        while (t < 0.2f)
        {
            float s = 1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.25f;
            target.localScale = Vector3.one * s;
            t += Time.deltaTime;
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}
