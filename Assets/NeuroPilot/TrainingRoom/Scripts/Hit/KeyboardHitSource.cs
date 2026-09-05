using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 手柄扳机或键盘空格模拟命中，不代表真实 EEG 识别。
/// 使用按下沿，配合 Reset 保证每球只计一次，持续按住不会跨球连击。
/// </summary>
public class KeyboardHitSource : MonoBehaviour, IHitSource
{
    private bool _armed = true;
    private bool _triggerHeld;
    private readonly List<UnityEngine.XR.InputDevice> _controllers = new List<UnityEngine.XR.InputDevice>();

    private bool ReadControllerTrigger()
    {
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, _controllers);
        foreach (var device in _controllers)
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool held) && held)
                return true;
        return false;
    }

    public bool HitPressed()
    {
        if (!_armed) return false;
        bool held = ReadControllerTrigger();
        bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (held && !_triggerHeld);
        _triggerHeld = held;
        if (pressed) _armed = false; // 消费掉，防连按
        return pressed;
    }

    public void Reset()
    {
        _armed = true;
        _triggerHeld = ReadControllerTrigger();
    }
}
