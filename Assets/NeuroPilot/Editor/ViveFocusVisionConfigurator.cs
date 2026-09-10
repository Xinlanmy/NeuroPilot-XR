#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace NeuroPilotXR.Editor
{
    /// <summary>Applies and validates the Android OpenXR profile used by VIVE Focus Vision.</summary>
    public static class ViveFocusVisionConfigurator
    {
        private static readonly HashSet<string> RequiredAndroidFeatures = new HashSet<string>(StringComparer.Ordinal)
        {
            "VIVE.OpenXR.VIVEFocus3Feature",
            "VIVE.OpenXR.VIVEFocus3Profile",
            "VIVE.OpenXR.EyeTracker.ViveEyeTracker",
            "UnityEngine.XR.OpenXR.Features.Interactions.EyeGazeInteraction"
        };

        [MenuItem("NeuroPilot/VIVE Focus Vision/Apply Project Profile")]
        public static void ApplyProjectProfile()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new InvalidOperationException("Android OpenXR settings are unavailable.");
            foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRFeature>())
            {
                string typeName = feature.GetType().FullName ?? "";
                if (RequiredAndroidFeatures.Contains(typeName)) feature.enabled = true;
                if (typeName.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("MetaQuest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("MockRuntime", StringComparison.OrdinalIgnoreCase) >= 0)
                    feature.enabled = false;
                // This group has no optional extensions selected; the dedicated Focus profile is used.
                if (typeName == "VIVE.OpenXR.Interaction.ViveInteractions") feature.enabled = false;
                EditorUtility.SetDirty(feature);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Focus Vision] Applied Android IL2CPP/ARM64/Vulkan and VIVE OpenXR profile.");
        }

        [MenuItem("NeuroPilot/VIVE Focus Vision/Validate Project")]
        public static void ValidateProject()
        {
            var problems = new List<string>();
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
                problems.Add("Android scripting backend is not IL2CPP");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                problems.Add("Android architecture is not ARM64-only");
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29)
                problems.Add("Android minimum SDK is below API 29");
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                problems.Add("color space is not Linear");
            var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (graphics.Length != 1 || graphics[0] != GraphicsDeviceType.Vulkan)
                problems.Add("Android graphics API is not Vulkan-only");

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) problems.Add("Android OpenXR settings are unavailable");
            else
            {
                var enabled = new HashSet<string>(StringComparer.Ordinal);
                foreach (OpenXRFeature feature in settings.GetFeatures<OpenXRFeature>())
                    if (feature.enabled) enabled.Add(feature.GetType().FullName ?? "");
                foreach (string typeName in RequiredAndroidFeatures)
                    if (!enabled.Contains(typeName)) problems.Add("required OpenXR feature disabled: " + typeName);
            }
            if (problems.Count > 0)
                throw new InvalidOperationException("Focus Vision validation failed:\n- " + string.Join("\n- ", problems));
            Debug.Log("[Focus Vision] Project validation passed.");
        }
    }
}
#endif
