// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
using VIVE.OpenXR.SecondaryViewConfiguration;

namespace VIVE.OpenXR.FirstPersonObserver
{
    /// <summary>
    /// Name: FirstPersonObserver.cs
    /// Role: OpenXR FirstPersonObserver Extension Class 
    /// Responsibility: The OpenXR extension implementation and its lifecycles logic in OpenXR
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "XR MSFT First Person Observer",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "HTC",
        Desc = "Request the application to render an additional first-person view of the scene.",
        DocumentationLink = "..\\Documentation",
        OpenxrExtensionStrings = OPEN_XR_EXTENSION_STRING,
        Version = "1.0.0",
        FeatureId = FeatureId,
        Hidden = true)]
#endif
    public class ViveFirstPersonObserver : OpenXRFeature
    {
        private static ViveFirstPersonObserver _instance;

        /// <summary>
        /// ViveFirstPersonObserver static instance (Singleton).
        /// </summary>
        public static ViveFirstPersonObserver Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance =
                        OpenXRSettings.Instance.GetFeature<ViveFirstPersonObserver>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// The log identification.
        /// </summary>
        private const string LogTag = "VIVE.OpenXR.FirstPersonObserver";

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>  
        public const string FeatureId = "vive.openxr.feature.firstpersonobserver";

        /// <summary>
        /// OpenXR specification <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_MSFT_first_person_observer">12.114. XR_MSFT_first_person_observer</see>.
        /// </summary>
        public const string OPEN_XR_EXTENSION_STRING = "XR_MSFT_first_person_observer";

        /// <summary>
        /// The flag represents whether the OpenXR loader created an instance or not.
        /// </summary>
        private bool XrInstanceCreated { get; set; } = false;

        /// <summary>
        /// The instance created through xrCreateInstance.
        /// </summary>
        private XrInstance XrInstance { get; set; } = 0;

        /// <summary>
        /// The function delegate declaration of xrGetInstanceProcAddr.
        /// </summary>
        private OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr { get; set; }

        #region OpenXR life-cycle events

        /// <summary>
        /// Called after xrCreateInstance.
        /// </summary>
        /// <param name="xrInstance">Handle of the xrInstance.</param>
        /// <returns>Returns true if successful. Returns false otherwise.</returns>
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!IsExtensionEnabled())
            {
                Warning($"OnInstanceCreate() {OPEN_XR_EXTENSION_STRING} or " +
                        $"{ViveSecondaryViewConfiguration.OPEN_XR_EXTENSION_STRING} is NOT enabled.");
                return false;
            }

            XrInstanceCreated = true;
            XrInstance = xrInstance;
            Debug("OnInstanceCreate() " + XrInstance);

            if (!GetXrFunctionDelegates(XrInstance))
            {
                Error("Get function pointer of OpenXRFunctionPointerAccessor failed.");
                return false;
            }

            Debug("Get function pointer of OpenXRFunctionPointerAccessor succeed.");
            return base.OnInstanceCreate(xrInstance);
        }

        #endregion

        /// <summary>
        /// Get the OpenXR function via XrInstance.
        /// </summary>
        /// <param name="xrInstance">The XrInstance is provided by the Unity OpenXR Plugin.</param>
        /// <returns>Return true if get successfully. False otherwise.</returns>
        private bool GetXrFunctionDelegates(XrInstance xrInstance)
        {
            if (xrGetInstanceProcAddr != IntPtr.Zero)
            {
                Debug("Get function pointer of openXRFunctionPointerAccessor.");
                XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(xrGetInstanceProcAddr,
                    typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;

                if (XrGetInstanceProcAddr == null)
                {
                    Error(
                        "Get function pointer of openXRFunctionPointerAccessor failed due to the XrGetInstanceProcAddr is null.");
                    return false;
                }
            }
            else
            {
                Error(
                    "Get function pointer of openXRFunctionPointerAccessor failed due to the xrGetInstanceProcAddr is null.");
                return false;
            }

            return true;
        }

        #region Utilities functions

        /// <summary>
        /// Check ViveFirstPersonObserver extension is enabled or not. 
        /// </summary>
        /// <returns>Return true if enabled. False otherwise.</returns>
        public static bool IsExtensionEnabled()
        {
            return OpenXRRuntime.IsExtensionEnabled(OPEN_XR_EXTENSION_STRING) &&
                   ViveSecondaryViewConfiguration.IsExtensionEnabled();
        }

        /// <summary>
        /// Print log with tag "VIVE.OpenXR.SecondaryViewConfiguration".
        /// </summary>
        /// <param name="msg">The log you want to print.</param>
        private static void Debug(string msg)
        {
            UnityEngine.Debug.Log(LogTag + " " + msg);
        }

        /// <summary>
        /// Print warning message with tag "VIVE.OpenXR.SecondaryViewConfiguration".
        /// </summary>
        /// <param name="msg">The warning message you want to print.</param>
        private static void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning(LogTag + " " + msg);
        }

        /// <summary>
        /// Print an error message with the tag "VIVE.OpenXR.SecondaryViewConfiguration."
        /// </summary>
        /// <param name="msg">The error message you want to print.</param>
        private static void Error(string msg)
        {
            UnityEngine.Debug.LogError(LogTag + " " + msg);
        }

        #endregion
    }
}