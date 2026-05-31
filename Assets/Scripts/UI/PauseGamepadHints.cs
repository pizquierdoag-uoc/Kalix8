using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using TMPro;

/// <summary>
/// Añade los botones del mando inline a cada línea del TxtControls,
/// separados por " / ", igual que los controles de teclado y ratón.
/// Ejemplo: CTRL  /  Clic Izquierdo  /  RT · A
/// </summary>
public class PauseGamepadHints : MonoBehaviour
{
    TextMeshProUGUI _txtControls;
    string          _originalText;

    // ── Texto con mando Xbox inline ─────────────────────────────────────────
    // <pos=62%> da margen suficiente para que "CAMBIAR ARMA" no solape la 2ª columna
    const string TEXT_XBOX =
        "<align=center><size=36><b>CONTROLES</b></size></align>\n\n"
      + "    <b>MOVER</b><pos=62%>Flechas / WASD / Stick\n"
      + "    <b>DISPARO</b><pos=62%>CTRL / Clic Izq. / RT · A\n"
      + "    <b>CAMBIAR ARMA</b><pos=62%>ALT / Clic Der. / LT · LB\n"
      + "    <b>BOMBA</b><pos=62%>ESPACIO / Y\n"
      + "    <b>PAUSA</b><pos=62%>ESC / START";

    // ── Texto con mando PlayStation inline ─────────────────────────────────
    const string TEXT_PS =
        "<align=center><size=36><b>CONTROLES</b></size></align>\n\n"
      + "    <b>MOVER</b><pos=62%>Flechas / WASD / Stick\n"
      + "    <b>DISPARO</b><pos=62%>CTRL / Clic Izq. / R2 · ×\n"
      + "    <b>CAMBIAR ARMA</b><pos=62%>ALT / Clic Der. / L2 · L1\n"
      + "    <b>BOMBA</b><pos=62%>ESPACIO / △\n"
      + "    <b>PAUSA</b><pos=62%>ESC / OPTIONS";

    // ── Ciclo de vida ───────────────────────────────────────────────────────

    void Awake()
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.text.Contains("CONTROLES"))
            {
                _txtControls  = tmp;
                _originalText = tmp.text;
                break;
            }
        }

        if (_txtControls == null)
            Debug.LogWarning("[PauseGamepadHints] No se encontró TxtControls en el PausePanel.");

        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        Restore();
    }

    // ── API pública ─────────────────────────────────────────────────────────

    public void Show() => Refresh();
    public void Hide() => Restore();

    // ── Lógica ─────────────────────────────────────────────────────────────

    void OnDeviceChange(InputDevice device, InputDeviceChange change) => Refresh();

    void Refresh()
    {
        if (_txtControls == null) return;

        var gp = Gamepad.current;
        if (gp == null)
            Restore();
        else
            _txtControls.text = (gp is DualShockGamepad) ? TEXT_PS : TEXT_XBOX;
    }

    void Restore()
    {
        if (_txtControls != null && _originalText != null)
            _txtControls.text = _originalText;
    }
}
