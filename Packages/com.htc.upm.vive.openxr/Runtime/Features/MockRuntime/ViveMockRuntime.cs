// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.Feature
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "VIVE XR MockRuntime",
        Desc = "VIVE's mock runtime.  Used with OpenXR MockRuntime to test features on Editor.",
        Company = "HTC",
        DocumentationLink = "..\\Documentation",
        OpenxrExtensionStrings = kOpenxrExtensionString,
        Version = "1.0.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone },
        FeatureId = featureId
    )]
#endif
    public class ViveMockRuntime : OpenXRFeature
    {
        public const string kOpenxrExtensionString = "";

        [DllImport("ViveMockRuntime", EntryPoint = "HookGetInstanceProcAddr")]
        public static extern IntPtr HookGetInstanceProcAddrFake(IntPtr func);

        //AddRequiredFeature
        [DllImport("ViveMockRuntime", EntryPoint = "AddRequiredFeature")]
        public static extern void AddRequiredFeature(string featureName);

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "vive.openxr.feature.mockruntime";

        public bool enableFuture = false;
        public bool enableAnchor = false;

        #region override functions

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            IntPtr nextProcAddr = func;
            if (Application.isEditor)
            {
                Debug.Log("ViveMockRuntime: HookGetInstanceProcAddr");
                try
                {
                    AddRequiredFeature("Future");
                    AddRequiredFeature("Anchor");
                    nextProcAddr = HookGetInstanceProcAddrFake(nextProcAddr);
                }
                catch (DllNotFoundException ex)
                {
                    Debug.LogError("ViveMockRuntime: DLL not found: " + ex.Message);
                }
                catch (EntryPointNotFoundException ex)
                {
                    Debug.LogError("ViveMockRuntime: Function not found in DLL: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.LogError("ViveMockRuntime: Unexpected error: " + ex.Message);
                }
            }
            return nextProcAddr;
        }
        #endregion override functions
    }
}
