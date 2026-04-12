using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleScreenController : MonoBehaviour
{
    [Header("Componentes de animación")]
    public ShipShowcaseAnim shipAnim;
    public TitleLogoEffect  logoEffect;

    [Header("UI")]
    public Image           fadePanel;       // Panel negro que cubre toda la pantalla
    public TextMeshProUGUI pressStartText;  // "PRESS ANY KEY"

    [Header("Parallax de fondo")]
    public ScrollManager scrollManager;
    public float         titleScrollSpeed = 2.5f;  // Lento y atmosférico

    [Header("Tiempos (segundos)")]
    public float fadeInDuration   = 1.2f;  // Duración del fade-in inicial
    public float shipEnterDelay   = 0.4f;  // Pausa antes de que entre la nave
    public float logoEnterDelay   = 0.6f;  // Pausa tras que la nave llega a su sitio
    public float pressStartDelay  = 0.8f;  // Pausa antes de mostrar "PRESS ANY KEY"
    public float autoAdvanceTime  = 20f;   // Avance automático si no hay input

    [Header("Escena destino")]
    public string mainMenuScene = "MainMenu";

    bool      _canSkip;
    bool      _exiting;
    float     _idleTimer;
    Coroutine _blinkRoutine;

    void Start()
    {
        StartCoroutine(TitleSequence());
    }

    void Update()
    {
        if (!_canSkip || _exiting) return;

        _idleTimer += Time.deltaTime;
        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || _idleTimer >= autoAdvanceTime)
            StartCoroutine(ExitToMenu());
    }

    IEnumerator TitleSequence()
    {
        // Estado inicial — todo oculto, pantalla completamente negra
        SetFade(1f);
        shipAnim?.Hide();
        logoEffect?.Hide();
        if (pressStartText != null) pressStartText.gameObject.SetActive(false);

        // El fondo empieza a desplazarse (sin esperar al fade)
        scrollManager?.SetSpeed(titleScrollSpeed);

        // Música de la pantalla de título
        AudioManager.Instance?.PlayTitleMusic();

        // Fade-in: el espacio aparece lentamente
        yield return StartCoroutine(FadeTo(0f, fadeInDuration));
        yield return new WaitForSeconds(shipEnterDelay);

        // La nave entra desde abajo
        if (shipAnim != null)
        {
            shipAnim.Show();
            shipAnim.PlayEntry();
            yield return new WaitUntil(() => shipAnim.EntryComplete);
        }

        yield return new WaitForSeconds(logoEnterDelay);

        // El logo aparece desde arriba con rebote
        if (logoEffect != null)
        {
            logoEffect.Show();
            logoEffect.PlayEntry();
            yield return new WaitUntil(() => logoEffect.EntryComplete);
        }

        yield return new WaitForSeconds(pressStartDelay);

        // "PRESS ANY KEY" con parpadeo arcade
        if (pressStartText != null)
        {
            pressStartText.gameObject.SetActive(true);
            _blinkRoutine = StartCoroutine(BlinkText());
        }

        _canSkip = true;
    }

    IEnumerator BlinkText()
    {
        while (true)
        {
            if (pressStartText != null) pressStartText.alpha = 1f;
            yield return new WaitForSeconds(0.55f);
            if (pressStartText != null) pressStartText.alpha = 0f;
            yield return new WaitForSeconds(0.45f);
        }
    }

    IEnumerator ExitToMenu()
    {
        _exiting = true;
        _canSkip = false;

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        if (pressStartText != null) pressStartText.gameObject.SetActive(false);

        yield return StartCoroutine(FadeTo(1f, 0.7f));
        SceneManager.LoadScene(mainMenuScene);
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start   = fadePanel != null ? fadePanel.color.a : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFade(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }
        SetFade(target);
    }

    void SetFade(float alpha)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = alpha;
        fadePanel.color = c;
    }
}
