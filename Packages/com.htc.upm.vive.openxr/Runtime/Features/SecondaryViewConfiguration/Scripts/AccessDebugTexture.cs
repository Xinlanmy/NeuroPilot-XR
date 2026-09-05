// Copyright HTC Corporation All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace VIVE.OpenXR.SecondaryViewConfiguration
{
    /// <summary>
    /// Name: AccessDebugTexture.cs
    /// Role: General script
    /// Responsibility: To assess the debug texture from SpectatorCameraBased.cs
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class AccessDebugTexture : MonoBehaviour
    {
        private static SpectatorCameraBased SpectatorCameraBased => SpectatorCameraBased.Instance;

        // Some variables related to time definition for access SpectatorCameraBased class resources
        private const float WaitSpectatorCameraBasedInitTime = 1.5f;
        private const float WaitSpectatorCameraBasedPeriodTime = .5f;
        private const float WaitSpectatorCameraBasedMaxTime = 10f;
        private const int QuadrupleCheckIsRecordingCount = 4;

        /// <summary>
        /// The GameObject Renderer component
        /// </summary>
        private Renderer Renderer { get; set; }

        /// <summary>
        /// The default value of material in Renderer component
        /// </summary>
        private Material DefaultMaterial { get; set; }

        /// <summary>
        /// Set the Renderer material as debug material
        /// </summary>
        private void SetDebugMaterial()
        {
            Debug.Log("SetDebugMaterial");

            if (SpectatorCameraBased)
            {
                if (SpectatorCameraBased.SpectatorCameraViewMaterial)
                {
                    Renderer.material = SpectatorCameraBased.SpectatorCameraViewMaterial;
                }
                else
                {
                    Debug.Log("No debug material set on SpectatorCameraBased.");
                }
            }
        }

        /// <summary>
        /// Set the Renderer material as default material
        /// </summary>
        private void SetDefaultMaterial()
        {
            Debug.Log("SetDefaultMaterial");
            Renderer.material = DefaultMaterial ? DefaultMaterial : null;
        }

        private IEnumerator Start()
        {
            float waitingTime = WaitSpectatorCameraBasedMaxTime;
            bool getSpectatorCameraBased = false;

            yield return new WaitForSeconds(WaitSpectatorCameraBasedInitTime);

            do
            {
                if (!SpectatorCameraBased)
                {
                    yield return new WaitForSeconds(WaitSpectatorCameraBasedPeriodTime);
                    waitingTime -= WaitSpectatorCameraBasedPeriodTime;
                    continue;
                }

                // Set -1 if accessed SpectatorCameraBased so we can break the while loop
                waitingTime = -1;
                getSpectatorCameraBased = true;

                Renderer = GetComponent<Renderer>();
                DefaultMaterial = Renderer.material;
                SpectatorCameraBased.OnSpectatorStart += SetDebugMaterial;
                SpectatorCameraBased.OnSpectatorStop += SetDefaultMaterial;
            } while (waitingTime > 0);

            if (!getSpectatorCameraBased)
            {
                Debug.Log($"Try to get SpectatorCameraBased " +
                          $"{WaitSpectatorCameraBasedMaxTime / WaitSpectatorCameraBasedPeriodTime} times but fail.");

                Debug.Log("Destroy AccessDebugTexture now.");
                Destroy(this);
                yield break;
            }

            int quadrupleCheckCount = QuadrupleCheckIsRecordingCount;
            while (quadrupleCheckCount > 0)
            {
                if (SpectatorCameraBased.IsRecording)
                {
                    Debug.Log("Recording. Set debug material.");
                    SpectatorCameraBased.OnSpectatorStart?.Invoke();
                    break;
                }

                quadrupleCheckCount--;
                yield return null;
                Debug.Log("No recording. Keep default material.");
            }
        }

        private void OnDestroy()
        {
            Renderer.material = DefaultMaterial ? DefaultMaterial : null;

            if (SpectatorCameraBased)
            {
                SpectatorCameraBased.OnSpectatorStart -= SetDebugMaterial;
                SpectatorCameraBased.OnSpectatorStop -= SetDefaultMaterial;
            }
        }
    }
}