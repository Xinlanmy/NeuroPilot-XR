// Copyright HTC Corporation All Rights Reserved.

using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VIVE.OpenXR.Models
{
    public class ControllerModelActions : MonoBehaviour
    {
        #region Log
        const string LOG_TAG = "VIVE.OpenXR.Models.ControllerModelActions";
        StringBuilder m_sb = null;
        StringBuilder sb {
            get {
                if (m_sb == null) { m_sb = new StringBuilder(); }
                return m_sb;
            }
        }
        void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
        void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
        #endregion

        #region Inspector
        public bool isLeft = false;
        public GameObject triggerButton = null;
        public GameObject gripButton = null;
        public GameObject thumbstickButton = null;
        public GameObject primaryButton = null;
        public GameObject secondaryButton = null;

        public InputAction trigger = null;
        public InputAction grip = null;
        public InputAction thumbstick = null;
        public InputAction primaryClick = null;
        public InputAction secondaryClick = null;
        #endregion

        private Quaternion triggerRot = Quaternion.identity;
        private Vector3 gripButtonPos = Vector3.zero;
        private Vector3 primaryButtonPos = Vector3.zero;
        private Vector3 secondaryButtonPos = Vector3.zero;

        void Start()
        {
            if (triggerButton != null) { triggerRot = triggerButton.transform.localRotation; }
            if (gripButton != null) { gripButtonPos = gripButton.transform.localPosition; }
            if (primaryButton != null) { primaryButtonPos = primaryButton.transform.localPosition; }
            if (secondaryButton != null) { secondaryButtonPos = secondaryButton.transform.localPosition; }

            if (trigger != null) { trigger.Enable(); }
            if (grip != null) { grip.Enable(); }
            if (thumbstick != null) { thumbstick.Enable(); }
            if (primaryClick != null) { primaryClick.Enable(); }
            if (secondaryClick != null) { secondaryClick.Enable(); }
        }

        void Update()
        {
            OnTrigger();
            OnGrip();
            OnThumbstick();
            OnPrimaryClick();
            OnSecondaryClick();
        }

        void OnTrigger()
        {
            if (OpenXRHelper.GetAnalog(trigger, out float value, out string msg))
            {
                triggerButton.transform.localRotation = Quaternion.Euler(value * 15f, 0, 0);
			}
			else
			{
                triggerButton.transform.localRotation = triggerRot;
            }
        }
        void OnGrip()
        {
            if (OpenXRHelper.GetAnalog(grip, out float value, out string msg))
            {
                if (isLeft)
                    gripButton.transform.localPosition = gripButtonPos + Vector3.right * value * -0.002f;
                else
                    gripButton.transform.localPosition = gripButtonPos + Vector3.left * value * -0.002f;
            }
        }
        void OnThumbstick()
        {
            if (OpenXRHelper.GetVector2(thumbstick, out Vector2 value, out string msg))
            {
                thumbstickButton.transform.localRotation = Quaternion.Euler(value.y * 25f, 0, value.x * -25f);
            }
        }
        void OnPrimaryClick()
        {
            if (OpenXRHelper.GetAnalog(primaryClick, out float value, out string msg))
            {
                primaryButton.transform.localPosition = primaryButtonPos + Vector3.down * (value > 0.5f ? 0.00125f : 0);
			}
			else
			{
                primaryButton.transform.localPosition = primaryButtonPos;
            }
        }
        void OnSecondaryClick()
        {
            if (OpenXRHelper.GetAnalog(secondaryClick, out float value, out string msg))
            {
                secondaryButton.transform.localPosition = secondaryButtonPos + Vector3.down * (value > 0.5f ? 0.00125f : 0);
			}
			else
			{
                secondaryButton.transform.localPosition = secondaryButtonPos;
			}
        }
    }
}