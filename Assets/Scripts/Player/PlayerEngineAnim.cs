using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FrameAnimator))]
public class PlayerEngineAnim : MonoBehaviour
{
    [Header("Frames de animación")]
    public Sprite[] idleFrames;
    public Sprite[] thrustFrames;

    [Header("Umbral de velocidad para activar Thrust")]
    [Tooltip("Magnitud mínima del input para considerar que hay movimiento")]
    public float thrustThreshold = 0.15f;

    FrameAnimator _anim;
    bool          _thrusting;

    void Awake()
    {
        _anim = GetComponent<FrameAnimator>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        float h = 0f, v = 0f;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) h -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h += 1f;
            if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) v -= 1f;
            if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) v += 1f;
        }
        float inputMag = new Vector2(h, v).magnitude;

        bool nowThrusting = inputMag >= thrustThreshold;
        if (nowThrusting == _thrusting) return;

        _thrusting = nowThrusting;

        if (_thrusting && thrustFrames != null && thrustFrames.Length > 0)
        {
            _anim.frames = thrustFrames;
            _anim.Play();
        }
        else if (!_thrusting && idleFrames != null && idleFrames.Length > 0)
        {
            _anim.frames = idleFrames;
            _anim.Play();
        }
    }
}
