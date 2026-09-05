using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
using VIVE.OpenXR;
using VIVE.OpenXR.Passthrough;

namespace NeuroPilotXR.Navigation
{
    public sealed class VivePassthroughManager : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float initializationTimeout = 8f;

        private XrPassthroughHTC passthroughHandle;
        private bool passthroughActive;

        public bool IsPassthroughActive => passthroughActive;

        private IEnumerator Start()
        {
            ConfigureCameraForUnderlay();

            float elapsed = 0f;
            while (!IsXrLoaderReady() && elapsed < initializationTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!IsXrLoaderReady())
            {
                Debug.Log("[NeuroPilot] Passthrough preview unavailable because no active XR loader was detected. Verify the result on VIVE Focus Vision or with VIVE Direct Preview.");
                yield break;
            }

            EnablePassthrough();
        }

        public bool EnablePassthrough()
        {
            if (passthroughActive)
                return true;

            XrResult result = PassthroughAPI.CreatePlanarPassthrough(
                out passthroughHandle,
                VIVE.OpenXR.CompositionLayer.LayerType.Underlay,
                null,
                1f,
                0);

            passthroughActive = result == XrResult.XR_SUCCESS;
            if (passthroughActive)
                Debug.Log("[NeuroPilot] VIVE full-field passthrough underlay enabled.");
            else
                Debug.LogWarning("[NeuroPilot] VIVE passthrough could not start in this runtime. Result: " + result);

            return passthroughActive;
        }

        public IEnumerator FadeOutAndDisable(float duration)
        {
            if (!passthroughActive)
                yield break;

            duration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < duration && passthroughActive)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                PassthroughAPI.SetPassthroughAlpha(passthroughHandle, alpha);
                yield return null;
            }

            DisablePassthrough();
        }

        public void DisablePassthrough()
        {
            if (!passthroughActive)
                return;

            PassthroughAPI.DestroyPassthrough(passthroughHandle);
            passthroughActive = false;
            passthroughHandle = 0;
            Debug.Log("[NeuroPilot] VIVE passthrough disabled.");
        }

        private static bool IsXrLoaderReady()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            XRManagerSettings manager = settings != null ? settings.Manager : null;
            return manager != null && manager.activeLoader != null;
        }

        private static void ConfigureCameraForUnderlay()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
        }

        private void OnDisable()
        {
            DisablePassthrough();
        }
    }
}
