using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEngineAnim : MonoBehaviour
{
    [Header("Frames de animación")]
    public Sprite[] idleFrames;
    public Sprite[] thrustFrames;

    [Header("Velocidad (fps) y umbral de input")]
    public float fps             = 14f;
    public float thrustThreshold = 0.15f;

    FrameAnimator _anim;
    bool          _thrusting;

    void Start()
    {
        _anim = GetComponent<FrameAnimator>();
        if (_anim == null)
        {
            Debug.LogWarning("PlayerEngineAnim: no FrameAnimator found on this GameObject.");
            return;
        }
        _anim.fps = fps;
        _anim.frames = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : thrustFrames;
    }

    void Update()
    {
        if (_anim == null) return;
        HandleInput();
    }

    void HandleInput()
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

        bool nowThrusting = new Vector2(h, v).magnitude >= thrustThreshold;
        if (nowThrusting == _thrusting) return;

        _thrusting  = nowThrusting;
        _anim.frames = _thrusting && thrustFrames != null && thrustFrames.Length > 0
                       ? thrustFrames : idleFrames;
    }
}
