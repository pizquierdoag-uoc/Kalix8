using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("Bombas")]
    public TextMeshProUGUI txtBombCount;

    [Header("Continues")]
    public TextMeshProUGUI txtContinueCount;

    [Header("Dificultad")]
    public TextMeshProUGUI txtDifficultyName;

    [Header("Botón Exit (para resaltarlo con mando)")]
    public Button closeButton;

    // Filas navegables con mando
    enum Row { Music, SFX, Lives, Bombs, Continues, Difficulty, Close }
    const int ROW_COUNT = 7;

    int   _selectedRow;
    float _navCooldown;

    int _lives;
    int _bombs;
    int _continues;
    int _musicPct;
    int _sfxPct;
    GameSettings.Difficulty _difficulty;

    void OnEnable()
    {
        _selectedRow = 0;
        _navCooldown = 0f;
        _lives      = GameSettings.StartingLives;
        _bombs      = GameSettings.StartingBombs;
        _continues  = GameSettings.StartingContinues;
        _difficulty = GameSettings.CurrentDifficulty;
        float mv    = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : 0.6f;
        float sv    = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume   : 0.8f;
        _musicPct   = Mathf.RoundToInt(mv * 10f) * 10;
        _sfxPct     = Mathf.RoundToInt(sv * 10f) * 10;
        RefreshUI();
        HighlightSelected();
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
        RefreshUI(true);
    }

    public void OnLivesRight()
    {
        _lives = _lives < 5 ? _lives + 1 : 5;
        GameSettings.SetLives(_lives);
        PlaySound();
        RefreshUI(true);
    }

    public void OnBombsLeft()
    {
        _bombs = _bombs > 0 ? _bombs - 1 : 0;
        GameSettings.SetBombs(_bombs);
        PlaySound();
        RefreshUI();
    }

    public void OnBombsRight()
    {
        _bombs = _bombs < 9 ? _bombs + 1 : 9;
        GameSettings.SetBombs(_bombs);
        PlaySound();
        RefreshUI();
    }

    public void OnContinuesLeft()
    {
        _continues = _continues > 0 ? _continues - 1 : 0;
        GameSettings.SetContinues(_continues);
        PlaySound();
        RefreshUI();
    }

    public void OnContinuesRight()
    {
        _continues = _continues < 9 ? _continues + 1 : 9;
        GameSettings.SetContinues(_continues);
        PlaySound();
        RefreshUI();
    }

    public void OnDifficultyLeft()
    {
        int d = (int)_difficulty;
        _difficulty = (GameSettings.Difficulty)Mathf.Max(0, d - 1);
        GameSettings.SetDifficulty(_difficulty);
        PlaySound();
        RefreshUI();
    }

    public void OnDifficultyRight()
    {
        int d = (int)_difficulty;
        _difficulty = (GameSettings.Difficulty)Mathf.Min(2, d + 1);
        GameSettings.SetDifficulty(_difficulty);
        PlaySound();
        RefreshUI();
    }

    void Update()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        _navCooldown -= Time.unscaledDeltaTime;

        // Arriba / Abajo → cambiar fila seleccionada
        bool navUp   = gp.dpad.up.isPressed   || gp.leftStick.ReadValue().y >  0.5f;
        bool navDown = gp.dpad.down.isPressed  || gp.leftStick.ReadValue().y < -0.5f;
        bool navLeft = gp.dpad.left.isPressed  || gp.leftStick.ReadValue().x < -0.5f;
        bool navRight= gp.dpad.right.isPressed || gp.leftStick.ReadValue().x >  0.5f;
        bool confirm = gp.buttonSouth.wasPressedThisFrame;
        bool back    = gp.buttonEast.wasPressedThisFrame || gp.startButton.wasPressedThisFrame;

        if (_navCooldown > 0f) return;

        if (navUp)
        {
            _selectedRow = (_selectedRow - 1 + ROW_COUNT) % ROW_COUNT;
            _navCooldown = 0.18f;
            AudioManager.Instance?.PlaySFX("menu_select");
            HighlightSelected();
        }
        else if (navDown)
        {
            _selectedRow = (_selectedRow + 1) % ROW_COUNT;
            _navCooldown = 0.18f;
            AudioManager.Instance?.PlaySFX("menu_select");
            HighlightSelected();
        }
        else if (navLeft)
        {
            _navCooldown = 0.18f;
            switch ((Row)_selectedRow)
            {
                case Row.Music:      OnMusicDown();      break;
                case Row.SFX:        OnSFXDown();        break;
                case Row.Difficulty: OnDifficultyLeft(); break;
                case Row.Lives:      OnLivesLeft();      break;
                case Row.Bombs:      OnBombsLeft();      break;
                case Row.Continues:  OnContinuesLeft();  break;
                case Row.Close:      Close();            break;
            }
        }
        else if (navRight)
        {
            _navCooldown = 0.18f;
            switch ((Row)_selectedRow)
            {
                case Row.Music:      OnMusicUp();        break;
                case Row.SFX:        OnSFXUp();          break;
                case Row.Difficulty: OnDifficultyRight();break;
                case Row.Lives:      OnLivesRight();     break;
                case Row.Bombs:      OnBombsRight();     break;
                case Row.Continues:  OnContinuesRight(); break;
                case Row.Close:      Close();            break;
            }
        }
        else if (confirm || back)
        {
            if ((Row)_selectedRow == Row.Close || back)
                Close();
        }
    }

    public void Close()
    {
        // Restaurar color negro del EXIT antes de cerrar
        if (closeButton != null)
        {
            var txt = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = new Color(0.1f, 0.1f, 0.1f);
        }

        // Invocar el onClick del botón EXIT para que MainMenuController.CloseOptions()
        // reactive los botones del menú principal (SetMainButtonsInteractable(true)).
        // Si no hay botón asignado, desactivamos directamente como fallback.
        if (closeButton != null)
            closeButton.onClick.Invoke();
        else
            gameObject.SetActive(false);
    }

    void RefreshUI(bool punchLives = false)
    {
        if (txtMusicVolume   != null) txtMusicVolume.text    = _musicPct + "%";
        if (txtSFXVolume     != null) txtSFXVolume.text      = _sfxPct   + "%";
        if (txtBombCount     != null) txtBombCount.text      = _bombs.ToString();
        if (txtContinueCount != null) txtContinueCount.text  = _continues.ToString();
        if (txtDifficultyName != null) txtDifficultyName.text = GameSettings.DifficultyName;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            bool active = i < _lives;
            lifeIcons[i].gameObject.SetActive(active);
            if (active)
            {
                lifeIcons[i].color          = iconActiveColor;
                lifeIcons[i].transform.localScale = Vector3.one;
                if (punchLives && i == _lives - 1)
                    StartCoroutine(PunchScale(lifeIcons[i].transform));
            }
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

    // Resalta la fila seleccionada en amarillo
    void HighlightSelected()
    {
        Color selected = new Color(1f, 0.88f, 0.2f);   // amarillo arcade
        Color normal   = new Color(1f, 1f,    1f, 0.9f);

        // Textos de valor (null = fila sin texto de valor)
        TextMeshProUGUI[] map = new TextMeshProUGUI[ROW_COUNT]
        {
            txtMusicVolume,    // 0 Music
            txtSFXVolume,      // 1 SFX
            null,              // 2 Lives (iconos)
            txtBombCount,      // 3 Bombs
            txtContinueCount,  // 4 Continues
            txtDifficultyName, // 5 Difficulty
            null,              // 6 Close — se gestiona abajo con closeButton
        };

        for (int i = 0; i < map.Length; i++)
            if (map[i] != null)
                map[i].color = (i == _selectedRow) ? selected : normal;

        // Resaltar el botón EXIT — fondo blanco, así usamos colores distintos:
        // normal = negro (legible sobre blanco), seleccionado = naranja oscuro
        if (closeButton != null)
        {
            var txt = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = (_selectedRow == (int)Row.Close)
                    ? new Color(0.85f, 0.35f, 0f)   // naranja oscuro — visible sobre blanco
                    : new Color(0.1f,  0.1f,  0.1f); // negro — legible sobre blanco
        }
    }

    void PlaySound() => AudioManager.Instance?.PlaySFX("menu_select");
}
