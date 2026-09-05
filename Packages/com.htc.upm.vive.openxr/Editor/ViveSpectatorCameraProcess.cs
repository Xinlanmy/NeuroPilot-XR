using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;
using static VIVE.OpenXR.VIVEFocus3Feature;

namespace VIVE.OpenXR.Editor
{
    public class ViveSpectatorCameraProcess : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 1;

        public override Type featureType => typeof(VIVEFocus3Feature);

        /// <summary>
        /// Enable or disable the "First Person Observer" extension according to the Spectator Camera Feature.
        /// </summary>
        /// <param name="enable">Type True if Spectator Camera Feature is enabled. Otherwise, type False.</param>
        private static void SetFirstPersonObserver(in bool enable)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);

            foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRFeature>())
            {
                FieldInfo fieldInfoOpenXrExtensionStrings = typeof(OpenXRFeature).GetField(
                    "openxrExtensionStrings",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfoOpenXrExtensionStrings != null)
                {
                    var openXrExtensionStringsArray =
                        ((string)fieldInfoOpenXrExtensionStrings.GetValue(feature)).Split(' ');

                    foreach (var stringItem in openXrExtensionStringsArray)
                    {
                        if (string.IsNullOrEmpty(stringItem))
                        {
                            continue;
                        }

                        if (!string.Equals(stringItem, FirstPersonObserver.ViveFirstPersonObserver.OPEN_XR_EXTENSION_STRING))
                        {
                            continue;
                        }

                        feature.enabled = enable;
                        return;
                    }
                }
            }
        }

        #region The callbacks during the build process when your OpenXR Extension is enabled.

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
            if (IsViveSpectatorCameraEnabled())
            {
                SetFirstPersonObserver(true);
                UnityEngine.Debug.Log("Enable \"First Person Observer\" extension due to the Spectator Camera Feature.");
            }
            else
            {
                SetFirstPersonObserver(false);
                UnityEngine.Debug.Log("Disable \"First Person Observer\" extension because Spectator Camera Feature is closed.");
            }
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        #endregion
    }
}