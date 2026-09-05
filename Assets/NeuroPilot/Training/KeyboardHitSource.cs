using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 键盘模拟命中：空格 = 模拟「EEG 识别到当前频率」。
    /// 开发/演示模式用，跑通全流程与调演示；项目为纯 Input System 模式，用 Keyboard.current。
    /// </summary>
    public sealed class KeyboardHitSource : MonoBehaviour, IHitSource
    {
        public bool HitPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        public void ResetHit()
        {
        }
    }
}
