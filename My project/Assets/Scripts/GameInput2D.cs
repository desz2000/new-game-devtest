using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Camada unificada de entrada para Teclado + Controle Wireless (Xbox, PlayStation, Switch e genéricos),
/// incluindo deadzone no analógico, detecção de borda para menus e vibração (rumble).
/// </summary>
public static class GameInput2D
{
    private const float StickDeadzone = 0.22f;
    private const float MenuStickThreshold = 0.55f;
    private const float TriggerThreshold = 0.50f;

    private static float prevStickX;
    private static float prevRightTrigger;
    private static float rumbleTimer;

    public static bool HasGamepad
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null;
#else
            return false;
#endif
        }
    }

    public static string ConnectedGamepadName
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null ? Gamepad.current.displayName : string.Empty;
#else
            return string.Empty;
#endif
        }
    }

    public static void Tick(float deltaTime)
    {
#if ENABLE_INPUT_SYSTEM
        if (rumbleTimer > 0f)
        {
            rumbleTimer -= deltaTime;
            if (rumbleTimer <= 0f)
            {
                StopRumble();
            }
        }
#endif
    }

    public static void LateTickState()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            prevStickX = pad.leftStick.x.ReadValue();
            prevRightTrigger = pad.rightTrigger.ReadValue();
        }
        else
        {
            prevStickX = 0f;
            prevRightTrigger = 0f;
        }
#endif
    }

    public static void TriggerRumble(float lowFrequency, float highFrequency, float duration)
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        pad.SetMotorSpeeds(Mathf.Clamp01(lowFrequency), Mathf.Clamp01(highFrequency));
        rumbleTimer = Mathf.Max(rumbleTimer, duration);
#endif
    }

    public static void StopRumble()
    {
#if ENABLE_INPUT_SYSTEM
        rumbleTimer = 0f;
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            pad.SetMotorSpeeds(0f, 0f);
        }
#endif
    }

    public static float ReadHorizontalMovement()
    {
        float kb = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kb -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kb += 1f;

        float padX = 0f;
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            float rawStick = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(rawStick) > StickDeadzone)
            {
                float sign = Mathf.Sign(rawStick);
                padX = sign * Mathf.InverseLerp(StickDeadzone, 1f, Mathf.Abs(rawStick));
            }

            float dpadX = pad.dpad.x.ReadValue();
            if (Mathf.Abs(dpadX) > Mathf.Abs(padX))
            {
                padX = dpadX;
            }
        }
#endif
        return Mathf.Clamp(kb + padX, -1f, 1f);
    }

    public static bool JumpPressedThisFrame()
    {
        bool kb = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padJump = pad != null && pad.buttonSouth.wasPressedThisFrame;
        return kb || padJump;
#else
        return kb;
#endif
    }

    public static bool JumpReleasedThisFrame()
    {
        bool kb = Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padRelease = pad != null && pad.buttonSouth.wasReleasedThisFrame;
        return kb || padRelease;
#else
        return kb;
#endif
    }

    public static bool FastFallHeld()
    {
        bool kb = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padDown = pad != null && (pad.dpad.down.isPressed || pad.leftStick.y.ReadValue() < -0.55f);
        return kb || padDown;
#else
        return kb;
#endif
    }

    public static bool FirePressedThisFrame()
    {
        bool kb = Input.GetKeyDown(KeyCode.Q);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad == null) return kb;

        bool rtPressed = pad.rightTrigger.ReadValue() > TriggerThreshold && prevRightTrigger <= TriggerThreshold;
        bool padFire = pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || rtPressed;
        return kb || padFire;
#else
        return kb;
#endif
    }

    public static bool MenuLeftPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Alpha1);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad == null) return kb;

        bool stickEdge = pad.leftStick.x.ReadValue() < -MenuStickThreshold && prevStickX >= -MenuStickThreshold;
        bool padLeft = pad.dpad.left.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || stickEdge;
        return kb || padLeft;
#else
        return kb;
#endif
    }

    public static bool MenuRightPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Alpha2);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad == null) return kb;

        bool stickEdge = pad.leftStick.x.ReadValue() > MenuStickThreshold && prevStickX <= MenuStickThreshold;
        bool padRight = pad.dpad.right.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || stickEdge;
        return kb || padRight;
#else
        return kb;
#endif
    }

    public static bool MenuConfirmPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padConfirm = pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        return kb || padConfirm;
#else
        return kb;
#endif
    }

    public static bool ModalRestartPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padRestart = pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        return kb || padRestart;
#else
        return kb;
#endif
    }

    public static bool SwitchCharacterPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.T);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padSwitch = pad != null && pad.buttonNorth.wasPressedThisFrame;
        return kb || padSwitch;
#else
        return kb;
#endif
    }

    public static bool GameplayMenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        return pad != null && pad.startButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    public static bool BackOrQuitPressed()
    {
        bool kb = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        bool padBack = pad != null && (pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame);
        return kb || padBack;
#else
        return kb;
#endif
    }
}
