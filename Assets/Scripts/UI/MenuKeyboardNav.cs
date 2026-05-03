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

    int _index;
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
        // Para no navegar si el panel de opciones está abierto
        if (optionsPanel != null && optionsPanel.activeSelf) return;
        if (Keyboard.current == null) return;

        bool down  = Keyboard.current.downArrowKey.wasPressedThisFrame
                  || Keyboard.current.sKey.wasPressedThisFrame;
        bool up    = Keyboard.current.upArrowKey.wasPressedThisFrame
                  || Keyboard.current.wKey.wasPressedThisFrame;
        bool enter = Keyboard.current.enterKey.wasPressedThisFrame
                  || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                  || Keyboard.current.spaceKey.wasPressedThisFrame;

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
