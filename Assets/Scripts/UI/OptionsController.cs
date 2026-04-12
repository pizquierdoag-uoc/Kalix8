using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsController : MonoBehaviour
{
    [Header("Iconos de vidas (5 sprites de nave)")]
    [Tooltip("Arrastra aquí los 5 Image con el sprite de la nave del HUD.")]
    public Image[] lifeIcons;

    public Color iconActiveColor   = Color.white;
    public Color iconInactiveColor = new Color(0.25f, 0.25f, 0.3f, 0.35f);

    [Header("Dificultad")]
    public TextMeshProUGUI txtDifficulty;
    public TextMeshProUGUI txtDescription; // Opcional

    public Color colorEasy   = new Color(0.40f, 1.00f, 0.45f); // verde
    public Color colorNormal = new Color(0.95f, 0.90f, 0.35f); // amarillo
    public Color colorHard   = new Color(1.00f, 0.32f, 0.22f); // rojo

    [Header("Audio")]
    public AudioClip buttonSound;

    int                      _lives;
    GameSettings.Difficulty  _diff;

    void OnEnable()
    {
        _lives = GameSettings.StartingLives;
        _diff  = GameSettings.CurrentDifficulty;
        RefreshUI();
    }

    public void OnLivesLeft()
    {
        _lives = _lives > 3 ? _lives - 1 : 3;
        GameSettings.SetLives(_lives);
        PlaySound();
        RefreshUI();
    }

    public void OnLivesRight()
    {
        _lives = _lives < 5 ? _lives + 1 : 5;
        GameSettings.SetLives(_lives);
        PlaySound();
        RefreshUI();
    }

    public void OnDiffLeft()
    {
        int d = (int)_diff - 1;
        if (d < 0) d = 2;
        _diff = (GameSettings.Difficulty)d;
        GameSettings.SetDifficulty(_diff);
        PlaySound();
        RefreshUI();
    }

    public void OnDiffRight()
    {
        int d = (int)_diff + 1;
        if (d > 2) d = 0;
        _diff = (GameSettings.Difficulty)d;
        GameSettings.SetDifficulty(_diff);
        PlaySound();
        RefreshUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void RefreshUI()
    {
        // Iconos de vida: activos los primeros _lives, el resto apagados
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            bool active = (i < _lives);
            lifeIcons[i].color = active ? iconActiveColor : iconInactiveColor;

            // Pequeño punch de escala en el icono que cambia
            if (active && i == _lives - 1)
                StartCoroutine(PunchScale(lifeIcons[i].transform));
        }

        // Texto de dificultad
        if (txtDifficulty != null)
        {
            txtDifficulty.text = GameSettings.DifficultyName;
            switch (_diff)
            {
                case GameSettings.Difficulty.Easy:   txtDifficulty.color = colorEasy;   break;
                case GameSettings.Difficulty.Hard:   txtDifficulty.color = colorHard;   break;
                default:                             txtDifficulty.color = colorNormal;  break;
            }
        }

        // Descripción corta (opcional)
        if (txtDescription != null)
            txtDescription.text = GameSettings.DifficultyDescription;
    }

    System.Collections.IEnumerator PunchScale(Transform t)
    {
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            float s = 1f + Mathf.Sin(elapsed / 0.18f * Mathf.PI) * 0.28f;
            t.localScale = Vector3.one * s;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void PlaySound()
    {
        if (buttonSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(buttonSound, Camera.main.transform.position);
    }
}
