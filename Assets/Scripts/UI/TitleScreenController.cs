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

    [Header("Efecto salida")]
    public float exitBombDelay = 0.25f;  // Segundos tras PlayExit antes del flash de bomba

    [Header("Banda sonora de explosiones (salida)")]
    [Tooltip("Duración total durante la cual se reproducirán SFX de explosión")]
    public float bombSfxDuration   = 4f;
    [Tooltip("Intervalo mínimo entre SFX de explosión (segundos)")]
    public float bombSfxMinSpacing = 0.13f;
    [Tooltip("Intervalo máximo entre SFX de explosión (segundos)")]
    public float bombSfxMaxSpacing = 0.32f;
    [Tooltip("Probabilidad (0-1) de usar el clip de bomba grande en lugar del de explosión pequeña")]
    [Range(0f,1f)] public float bombSfxBigChance = 0.25f;
    [Tooltip("Multiplicador de volumen aplicado a cada explosión (evita saturar al solaparse)")]
    [Range(0f,1f)] public float bombSfxVolumeScale = 0.55f;

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
        bool gamepadPressed = Gamepad.current != null && (
            Gamepad.current.buttonSouth.wasPressedThisFrame  ||
            Gamepad.current.buttonNorth.wasPressedThisFrame  ||
            Gamepad.current.buttonEast.wasPressedThisFrame   ||
            Gamepad.current.buttonWest.wasPressedThisFrame   ||
            Gamepad.current.startButton.wasPressedThisFrame  ||
            Gamepad.current.leftShoulder.wasPressedThisFrame ||
            Gamepad.current.rightShoulder.wasPressedThisFrame);

        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
         || gamepadPressed
         || _idleTimer >= autoAdvanceTime)
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

        shipAnim?.PlayExit();
        yield return new WaitForSeconds(exitBombDelay);
        BombScreenEffect.Instance?.Trigger();
        StartCoroutine(PlayBombExplosionSoundtrack());
        if (shipAnim != null)
            yield return new WaitUntil(() => shipAnim.ExitComplete);
        yield return StartCoroutine(FadeTo(1f, 2.1f));
        SceneManager.LoadScene(mainMenuScene);
    }

    /// <summary>
    /// Reproduce una ráfaga de SFX de explosión sincronizada con la animación visual
    /// de BombScreenEffect (20 explosiones en ~4s). Mezcla la bomba grande con la
    /// explosión pequeña a pitch/volumen variados para crear sensación de campo de batalla
    /// sin saturar el canal de audio.
    /// </summary>
    IEnumerator PlayBombExplosionSoundtrack()
    {
        var am = AudioManager.Instance;
        if (am == null) yield break;

        // Boom inicial fuerte que marca el inicio del caos
        am.PlaySFXVaried(am.sfxBombExplosion, 0.95f, 1.05f, 1f);

        float elapsed = 0f;
        while (elapsed < bombSfxDuration)
        {
            float wait = Random.Range(bombSfxMinSpacing, bombSfxMaxSpacing);
            yield return new WaitForSecondsRealtime(wait);
            elapsed += wait;

            AudioClip clip = (Random.value < bombSfxBigChance && am.sfxBombExplosion != null)
                ? am.sfxBombExplosion
                : (am.sfxEnemyDeath != null ? am.sfxEnemyDeath : am.sfxBombExplosion);
            if (clip == null) continue;

            // Volumen y pitch decreciendo ligeramente hacia el final
            float fade = Mathf.Lerp(1f, 0.55f, elapsed / bombSfxDuration);
            am.PlaySFXVaried(clip, 0.78f, 1.22f, bombSfxVolumeScale * fade);
        }
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
