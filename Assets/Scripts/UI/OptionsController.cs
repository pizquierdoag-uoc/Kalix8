using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsController : MonoBehaviour
{
    [Header("Música")]
    public TextMeshProUGUI txtMusicVolume;

    [Header("Sonido")]
    public TextMeshProUGUI txtSFXVolume;

    [Header("Iconos de vidas (5 sprites)")]
    public Image[] lifeIcons;
    public Color iconActiveColor   = Color.white;
    public Color iconInactiveColor = new Color(0.25f, 0.25f, 0.3f, 0.35f);


    int _lives;
    int _musicPct;
    int _sfxPct;

    void OnEnable()
    {
        _lives    = GameSettings.StartingLives;
        float mv  = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : 0.6f;
        float sv  = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume   : 0.8f;
        _musicPct = Mathf.RoundToInt(mv * 10f) * 10;
        _sfxPct   = Mathf.RoundToInt(sv * 10f) * 10;
        RefreshUI();
    }

    public void OnMusicDown()
    {
        _musicPct = Mathf.Max(0, _musicPct - 10);
        AudioManager.Instance?.SetMusicVolume(_musicPct / 100f);
        PlaySound();
        RefreshUI();
    }

    public void OnMusicUp()
    {
        _musicPct = Mathf.Min(100, _musicPct + 10);
        AudioManager.Instance?.SetMusicVolume(_musicPct / 100f);
        PlaySound();
        RefreshUI();
    }

    public void OnSFXDown()
    {
        _sfxPct = Mathf.Max(0, _sfxPct - 10);
        AudioManager.Instance?.SetSFXVolume(_sfxPct / 100f);
        PlaySound();
        RefreshUI();
    }

    public void OnSFXUp()
    {
        _sfxPct = Mathf.Min(100, _sfxPct + 10);
        AudioManager.Instance?.SetSFXVolume(_sfxPct / 100f);
        PlaySound();
        RefreshUI();
    }

    public void OnLivesLeft()
    {
        _lives = _lives > 2 ? _lives - 1 : 2;
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

    public void Close() => gameObject.SetActive(false);

    void RefreshUI()
    {
        if (txtMusicVolume != null) txtMusicVolume.text = _musicPct + "%";
        if (txtSFXVolume   != null) txtSFXVolume.text   = _sfxPct   + "%";

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            bool active = i < _lives;
            lifeIcons[i].color = active ? iconActiveColor : iconInactiveColor;
            if (active && i == _lives - 1)
                StartCoroutine(PunchScale(lifeIcons[i].transform));
        }
    }

    System.Collections.IEnumerator PunchScale(Transform t)
    {
        float e = 0f;
        while (e < 0.18f)
        {
            float s = 1f + Mathf.Sin(e / 0.18f * Mathf.PI) * 0.28f;
            t.localScale = Vector3.one * s;
            e += Time.unscaledDeltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void PlaySound() => AudioManager.Instance?.PlaySFX("menu_select");
}
