using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Navegación por teclado para el menú principal.
// Flechas Arriba/Abajo para moverse, Enter/Espacio para confirmar.

public class MenuKeyboardNav : MonoBehaviour
{
    [Header("Botones en orden (Jugar, Opciones, Salir)")]
    public Button[] buttons;

    [Header("Panel de opciones (bloquea la navegación mientras está abierto)")]
    public GameObject optionsPanel;

    [Header("Colores")]
    public Color colorSelected = new Color(0f, 0f, 0f, 1f);     // Negro sólido
    public Color colorNormal   = new Color(0f, 0f, 0f, 0.4f);   // Negro semitransparente

    [Header("Prefijo del botón seleccionado")]
    public string prefix = "► ";

    int   _index;
    float _optionsClosedAt = -99f;  // unscaledTime cuando se cerró el panel de opciones
    string[] _originalTexts;

    void OnEnable()
    {
        CacheTexts();
        _index = 0;
        Refresh();
    }

    void OnDisable()
    {
        RestoreTexts();
    }

    void CacheTexts()
    {
        if (buttons == null) { _originalTexts = new string[0]; return; }
        _originalTexts = new string[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) _originalTexts[i] = tmp.text;
        }
    }

    void RestoreTexts()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || _originalTexts == null) continue;
            var tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text  = _originalTexts[i];
                tmp.color = colorNormal;
            }
        }
    }

    void Update()
    {
        bool panelActive = optionsPanel != null && optionsPanel.activeSelf;

        if (panelActive)
        {
            // Mientras el panel está abierto actualizamos el timestamp de cierre.
            // Así, cuando se cierre, el cooldown empieza desde este momento.
            _optionsClosedAt = Time.unscaledTime;
            return;
        }

        // Cooldown de 0.3 s tras cerrar opciones para evitar que el botón
        // que cerró el panel también dispare el menú principal.
        if (Time.unscaledTime - _optionsClosedAt < 0.3f) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool down  = (kb != null && (kb.downArrowKey.wasPressedThisFrame  || kb.sKey.wasPressedThisFrame))
                  || (gp != null && (gp.dpad.down.wasPressedThisFrame     || gp.leftStick.down.wasPressedThisFrame));
        bool up    = (kb != null && (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame))
                  || (gp != null && (gp.dpad.up.wasPressedThisFrame       || gp.leftStick.up.wasPressedThisFrame));
        bool enter = (kb != null && (kb.enterKey.wasPressedThisFrame      || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                  || (gp != null && (gp.buttonSouth.wasPressedThisFrame   || gp.startButton.wasPressedThisFrame));

        if (down)
        {
            _index = (_index + 1) % buttons.Length;
            AudioManager.Instance?.PlaySFX("menu_select");
            Refresh();
        }
        else if (up)
        {
            _index = (_index - 1 + buttons.Length) % buttons.Length;
            AudioManager.Instance?.PlaySFX("menu_select");
            Refresh();
        }
        else if (enter)
        {
            var btn = buttons[_index];
            if (btn != null && btn.interactable)
                btn.onClick.Invoke();
        }
    }

    void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null || _originalTexts == null) continue;

            if (i == _index)
            {
                tmp.text  = prefix + _originalTexts[i];
                tmp.color = colorSelected;
            }
            else
            {
                tmp.text  = _originalTexts[i];
                tmp.color = colorNormal;
            }
        }
    }
}
