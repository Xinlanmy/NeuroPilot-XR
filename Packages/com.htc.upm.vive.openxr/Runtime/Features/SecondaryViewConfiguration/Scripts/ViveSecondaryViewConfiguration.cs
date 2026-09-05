// Copyright HTC Corporation All Rights Reserved.

using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.SecondaryViewConfiguration
{
    /// <summary>
    /// Name: SecondaryViewConfiguration.cs
    /// Role: OpenXR SecondaryViewConfiguration Extension Class
    /// Responsibility: The OpenXR extension implementation and its lifecycles logic in OpenXR
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "VIVE XR Spectator Camera (Beta)",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "HTC",
        Desc = "Allows an application to enable support for one or more secondary view configurations.",
        DocumentationLink = "..\\Documentation",
        OpenxrExtensionStrings = OPEN_XR_EXTENSION_STRING,
        Version = "1.0.0",
        FeatureId = FeatureId)]
#endif
    public partial class ViveSecondaryViewConfiguration : OpenXRFeature
    {
        #region Varibles

        private static ViveSecondaryViewConfiguration _instance;

        /// <summary>
        /// ViveSecondaryViewConfiguration static instance (Singleton).
        /// </summary>
        public static ViveSecondaryViewConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance =
                        OpenXRSettings.Instance.GetFeature<ViveSecondaryViewConfiguration>();
                }

                return _instance;
            }
        }

        #region OpenXR variables related to definition

        /// <summary>
        /// The log identification.
        /// </summary>
        private const string LogTag = "VIVE.OpenXR.SecondaryViewConfiguration";

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string FeatureId = "vive.openxr.feature.secondaryviewconfiguration";

        /// <summary>
        /// OpenXR specification <a href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_MSFT_secondary_view_configuration">12.122. XR_MSFT_secondary_view_configuration</a>.
        /// </summary>
        public const string OPEN_XR_EXTENSION_STRING = "XR_MSFT_secondary_view_configuration";

        /// <summary>
        /// The extension library name.
        /// </summary>
        private const string ExtLib = "libviveopenxr";

        #endregion

        #region OpenXR variables related to its life-cycle

        /// <summary>
        /// The flag represents whether the OpenXR loader created an instance or not.
        /// </summary>
        private bool XrInstanceCreated { get; set; } = false;

        /// <summary>
        /// The flag represents whether the OpenXR loader created a session or not.
        /// </summary>
        private bool XrSessionCreated { get; set; } = false;

        /// <summary>
        /// The flag represents whether the OpenXR loader started a session or not.
        /// </summary>
        private bool XrSessionStarted { get; set; } = false;

        /// <summary>
        /// The instance created through xrCreateInstance.
        /// </summary>
        private XrInstance XrInstance { get; set; } = 0;

        /// <summary>
        /// An XrSystemId is an opaque atom used by the runtime to identify a system.
        /// </summary>
        private XrSystemId XrSystemId { get; set; } = 0;

        /// <summary>
        /// A session represents an application’s intention to display XR content to the user.
        /// </summary>
        private XrSession XrSession { get; set; } = 0;

        /// <summary>
        /// New possible session lifecycle states.
        /// </summary>
        private XrSessionState XrSessionNewState { get; set; } = XrSessionState.XR_SESSION_STATE_UNKNOWN;

        /// <summary>
        /// The previous state possible session lifecycle states.
        /// </summary>
        private XrSessionState XrSessionOldState { get; set; } = XrSessionState.XR_SESSION_STATE_UNKNOWN;

        /// <summary>
        /// The function delegate declaration of xrGetInstanceProcAddr.
        /// </summary>
        private OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr { get; set; }

        #endregion

        #region Variables related to handle agent functions

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes a boolean argument and returns void. This declaration should only be used in the function "SecondaryViewConfigurationInterceptOpenXRMethod".
        /// </summary>
        private delegate void SetSecondaryViewConfigurationStateDelegate(bool isEnabled);

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes a boolean argument and returns void. This declaration should only be used in the function "SecondaryViewConfigurationInterceptOpenXRMethod".
        /// </summary>
        private delegate void StopEnableSecondaryViewConfigurationDelegate(bool isStopped);

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes a boolean argument and returns void. This declaration should only be used in the function "SecondaryViewConfigurationInterceptOpenXRMethod".
        /// </summary>
        private delegate void SetTextureSizeDelegate(UInt32 width, UInt32 height);

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes a boolean argument and returns void. This declaration should only be used in the function "SecondaryViewConfigurationInterceptOpenXRMethod".
        /// </summary>
        private delegate void SetFovDelegate(XrFovf fov);

        #endregion

        #region Variables related to callback functions instantiation

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes a Vector2 argument and returns void.
        /// </summary>
        public delegate void OnTextureSizeUpdatedDelegate(Vector2 size);

        /// <summary>
        /// The instantiation of the delegate OnTextureSizeUpdatedDelegate. This will be called when the texture size coming from the native plugin is updated.
        /// </summary>
        public OnTextureSizeUpdatedDelegate onTextureSizeUpdated;

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes no argument and returns void.
        /// </summary>
        public delegate void OnTextureUpdatedDelegate();

        /// <summary>
        /// The instantiation of the delegate OnTextureUpdatedDelegate. This will be called when the texture coming from the native plugin is updated.
        /// </summary>
        public OnTextureUpdatedDelegate onTextureUpdated;

        /// <summary>
        /// A delegate declaration can encapsulate the method that takes four floating-point arguments, left, right, up, and down, respectively, and returns void.
        /// </summary>
        public delegate void OnFovUpdatedDelegate(float left, float right, float up, float down);

        /// <summary>
        /// The instantiation of the delegate OnFovUpdatedDelegate. This will be called when the fov setting coming from the native plugin is updated.
        /// </summary>
        public OnFovUpdatedDelegate onFovUpdated;

        #endregion

        #region Rendering and texture varibles

        /// <summary>
        /// The graphics backend that the current application is used.
        /// </summary>
        private static GraphicsAPI MyGraphicsAPI
        {
            get
            {
                return SystemInfo.graphicsDeviceType switch
                {
                    GraphicsDeviceType.OpenGLES3 => GraphicsAPI.GLES3,
                    GraphicsDeviceType.Vulkan => GraphicsAPI.Vulkan,
                    _ => GraphicsAPI.Unknown
                };
            }
        }

        private Vector2 _textureSize;

        /// <summary>
        /// The value of texture size coming from the native plugin.
        /// </summary>
        public Vector2 TextureSize
        {
            get => _textureSize;
            private set
            {
                _textureSize = value;
                onTextureSizeUpdated?.Invoke(value);
            }
        }

        private Texture _myTexture;

        /// <summary>
        /// The texture handler adopts the native 2D texture object coming from the native plugin.
        /// </summary>
        public Texture MyTexture
        {
            get => _myTexture;
            private set
            {
                _myTexture = value;
                onTextureUpdated?.Invoke();
            }
        }

        #endregion

        #region The variables (flag) represent the state related to this OpenXR extension

        /// <summary>
        /// The state of the second view configuration comes from runtime.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The flag represents to whether the second view configuration is disabled or not.
        /// </summary>
        public bool IsStopped { get; set; }

        /// <summary>
        /// The flag represents to whether the "SpectatorCameraBased" script exists in the Unity scene.
        /// </summary>
        private bool IsExistSpectatorCameraBased { get; set; }

        #endregion

        #endregion

        #region Function

        #region OpenXR life-cycle functions

        /// <summary>
        /// Called after xrCreateInstance.
        /// </summary>
        /// <param name="xrInstance">Handle of the xrInstance.</param>
        /// <returns>Returns true if successful. Returns false otherwise.</returns>
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!IsExtensionEnabled())
            {
                Warning("OnInstanceCreate() " + OPEN_XR_EXTENSION_STRING + " is NOT enabled.");
                return false;
            }

            var mySpectatorCameraBasedGameObject = GetSpectatorCameraBased();
            IsExistSpectatorCameraBased = mySpectatorCameraBasedGameObject != null;

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

        /// <summary>
        /// Called after xrGetSystem
        /// </summary>
        /// <param name="xrSystem">Handle of the xrSystemId</param>
        protected override void OnSystemChange(ulong xrSystem)
        {
            XrSystemId = xrSystem;
            Debug("OnSystemChange() " + XrSystemId);

            base.OnSystemChange(xrSystem);
        }

        /// <summary>
        /// Called after xrCreateSession.
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession.</param>
        protected override void OnSessionCreate(ulong xrSession)
        {
            XrSessionCreated = true;
            XrSession = xrSession;
            Debug("OnSessionCreate() " + XrSession);

            base.OnSessionCreate(xrSession);
        }

        /// <summary>
        /// Called after xrSessionBegin.
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession.</param>
        protected override void OnSessionBegin(ulong xrSession)
        {
            XrSessionStarted = true;
            Debug("OnSessionBegin() " + XrSessionStarted);

            base.OnSessionBegin(xrSession);
        }

        /// <summary>
        /// Called when the OpenXR loader receives the XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED event from the runtime signaling that the XrSessionState has changed.
        /// </summary>
        /// <param name="oldState">Previous state.</param>
        /// <param name="newState">New state.</param>
        protected override void OnSessionStateChange(int oldState, int newState)
        {
            Debug("OnSessionStateChange() oldState: " + oldState + " newState:" + newState);

            if (Enum.IsDefined(typeof(XrSessionState), oldState))
            {
                XrSessionOldState = (XrSessionState)oldState;
            }
            else
            {
                Warning("OnSessionStateChange() oldState undefined");
            }

            if (Enum.IsDefined(typeof(XrSessionState), newState))
            {
                XrSessionNewState = (XrSessionState)newState;
            }
            else
            {
                Warning("OnSessionStateChange() newState undefined");
            }

            base.OnSessionStateChange(oldState, newState);
        }

        /// <summary>
        /// Called to hook xrGetInstanceProcAddr. Returning a different function pointer allows intercepting any OpenXR method.
        /// </summary>
        /// <param name="func">xrGetInstanceProcAddr native function pointer.</param>
        /// <returns>Function pointer that Unity will use to look up OpenXR native functions.</returns>
        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            Debug("HookGetInstanceProcAddr Start");
            if (MyGraphicsAPI is GraphicsAPI.GLES3 or GraphicsAPI.Vulkan)
            {
                Debug($"The app graphics API is {MyGraphicsAPI}");
                return SecondaryViewConfigurationInterceptOpenXRMethod
                (
                    MyGraphicsAPI,
                    SystemInfo.graphicsUVStartsAtTop,
                    func,
                    SetSecondaryViewConfigurationState,
                    StopEnableSecondaryViewConfiguration,
                    SetTextureSize,
                    SetFov
                );
            }

            Error(
                "The render backend is not supported. Requires OpenGL or Vulkan backend for secondary view configuration feature.");
            return base.HookGetInstanceProcAddr(func);
        }

        /// <summary>
        /// Called before xrEndSession.
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession.</param>
        protected override void OnSessionEnd(ulong xrSession)
        {
            XrSessionStarted = false;
            Debug("OnSessionEnd() " + XrSession);

            base.OnSessionEnd(xrSession);
        }

        /// <summary>
        /// Called before xrDestroySession.
        /// </summary>
        /// <param name="xrSession">Handle of the xrSession.</param>
        protected override void OnSessionDestroy(ulong xrSession)
        {
            XrSessionCreated = false;
            Debug("OnSessionDestroy() " + xrSession);

            base.OnSessionDestroy(xrSession);
        }

        /// <summary>
        /// Called before xrDestroyInstance.
        /// </summary>
        /// <param name="xrInstance">Handle of the xrInstance.</param>
        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            XrInstanceCreated = false;
            XrInstance = 0;
            Debug("OnInstanceDestroy() " + xrInstance);

            base.OnInstanceDestroy(xrInstance);
        }

        #endregion

        #region Handle agent functions

        /// <summary>
        /// This function is defined as the "SetSecondaryViewConfigurationStateDelegate" delegate function.
        /// <b>Please be careful that this function should ONLY be called by native plug-ins.
        /// THIS FUNCTION IS NOT DESIGNED FOR CALLING FROM THE UNITY ENGINE SIDE.</b>
        /// </summary>
        /// <param name="isEnabled">The state of the second view configuration comes from runtime. True if enabled. False otherwise.</param>
        [MonoPInvokeCallback(typeof(SetSecondaryViewConfigurationStateDelegate))]
        private static void SetSecondaryViewConfigurationState(bool isEnabled)
        {
            Instance.IsEnabled = isEnabled;

            if (Instance.IsEnableDebugLog)
            {
                Debug($"SetSecondaryViewConfigurationState: Instance.IsEnabled set as {Instance.IsEnabled}");
            }
        }

        /// <summary>
        /// This function is defined as the "StopEnableSecondaryViewConfigurationDelegate" delegate function.
        /// <b>Please be careful that this function should ONLY be called by native plug-ins.
        /// THIS FUNCTION IS NOT DESIGNED FOR CALLING FROM THE UNITY ENGINE SIDE.</b>
        /// </summary>
        /// <param name="isStopped">The flag refers to whether the second view configuration is disabled or not. True if the second view configuration is disabled. False otherwise.</param>
        [MonoPInvokeCallback(typeof(StopEnableSecondaryViewConfigurationDelegate))]
        private static void StopEnableSecondaryViewConfiguration(bool isStopped)
        {
            Instance.IsStopped = isStopped;

            if (Instance.IsEnableDebugLog)
            {
                Debug($"StopEnableSecondaryViewConfiguration: Instance.IsStopped set as {Instance.IsStopped}");
            }
        }

        /// <summary>
        /// This function is defined as the "SetTextureSizeDelegate" delegate function.
        /// <b>Please be careful that this function should ONLY be called by native plug-ins.
        /// THIS FUNCTION IS NOT DESIGNED FOR CALLING FROM THE UNITY ENGINE SIDE.</b>
        /// </summary>
        /// <param name="width">The texture width comes from runtime.</param>
        /// <param name="height">The texture height comes from runtime.</param>
        [MonoPInvokeCallback(typeof(SetTextureSizeDelegate))]
        private static void SetTextureSize(uint width, uint height)
        {
            if (!Instance.IsExistSpectatorCameraBased)
            {
                CreateSpectatorCameraBased();
            }

            Instance.TextureSize = new Vector2(width, height);
            if (Instance.IsEnableDebugLog)
            {
                Debug($"SetTextureSize width: {Instance.TextureSize.x}, height: {Instance.TextureSize.y}");
            }

            IntPtr texPtr = GetSecondaryViewTextureId(out uint imageIndex);
            if (Instance.IsEnableDebugLog)
            {
                Debug($"SetTextureSize texPtr: {texPtr}, imageIndex: {imageIndex}");
            }

            if (texPtr == IntPtr.Zero)
            {
                Error($"SetTextureSize texPtr is invalid: {texPtr}");
                return;
            }

            if (Instance.IsEnableDebugLog)
            {
                Debug("Get ptr successfully");
            }

            Instance.MyTexture = Texture2D.CreateExternalTexture(
                (int)Instance.TextureSize.x,
                (int)Instance.TextureSize.y,
                TextureFormat.RGBA32,
                false,
                false,
                texPtr);

            #region For development usage (Just for reference)

            /*
            if (Instance.IsEnableDebugLog)
            {
                Debug("Create texture successfully");
                Debug($"Instance.MyTexture.height: {Instance.MyTexture.height}");
                Debug($"Instance.MyTexture.width: {Instance.MyTexture.width}");
                Debug($"Instance.MyTexture.dimension: {Instance.MyTexture.dimension}");
                Debug($"Instance.MyTexture.anisoLevel: {Instance.MyTexture.anisoLevel}");
                Debug($"Instance.MyTexture.filterMode: {Instance.MyTexture.filterMode}");
                Debug($"Instance.MyTexture.wrapMode: {Instance.MyTexture.wrapMode}");
                Debug($"Instance.MyTexture.graphicsFormat: {Instance.MyTexture.graphicsFormat}");
                Debug($"Instance.MyTexture.isReadable: {Instance.MyTexture.isReadable}");
                Debug($"Instance.MyTexture.texelSize: {Instance.MyTexture.texelSize}");
                Debug($"Instance.MyTexture.mipmapCount: {Instance.MyTexture.mipmapCount}");
                Debug($"Instance.MyTexture.updateCount: {Instance.MyTexture.updateCount}");
                Debug($"Instance.MyTexture.mipMapBias: {Instance.MyTexture.mipMapBias}");
                Debug($"Instance.MyTexture.wrapModeU: {Instance.MyTexture.wrapModeU}");
                Debug($"Instance.MyTexture.wrapModeV: {Instance.MyTexture.wrapModeV}");
                Debug($"Instance.MyTexture.wrapModeW: {Instance.MyTexture.wrapModeW}");
                Debug($"Instance.MyTexture.filterMode: {Instance.MyTexture.name}");
                Debug($"Instance.MyTexture.hideFlags: {Instance.MyTexture.hideFlags}");
                Debug($"Instance.MyTexture.GetInstanceID(): {Instance.MyTexture.GetInstanceID()}");
                Debug($"Instance.MyTexture.GetType(): {Instance.MyTexture.GetType()}");
                Debug($"Instance.MyTexture.GetNativeTexturePtr(): {Instance.MyTexture.GetNativeTexturePtr()}");
                // Print imageContentsHash will cause an error
                // Debug($"Instance.MyTexture.imageContentsHash: {Instance.MyTexture.imageContentsHash}");
            }
            */

            #endregion
        }

        /// <summary>
        /// This function is defined as the "SetFovDelegate" delegate function.
        /// <b>Please be careful that this function should ONLY be called by native plug-ins.
        /// THIS FUNCTION IS NOT DESIGNED FOR CALLING FROM THE UNITY ENGINE SIDE.</b>
        /// </summary>
        /// <param name="fov">The fov value comes from runtime.</param>
        [MonoPInvokeCallback(typeof(SetFovDelegate))]
        private static void SetFov(XrFovf fov)
        {
            if (Instance.IsEnableDebugLog)
            {
                Debug($"fov.AngleDown {fov.angleDown}");
                Debug($"fov.AngleLeft {fov.angleLeft}");
                Debug($"fov.AngleRight {fov.angleRight}");
                Debug($"fov.AngleUp {fov.angleUp}");
            }

            Instance.onFovUpdated?.Invoke(fov.angleLeft, fov.angleRight, fov.angleUp, fov.angleDown);
        }

        #endregion

        #region C++ interop functions

        /// <summary>
        /// Call this function to trigger the native plug-in that gets a specific OpenXR function for services to the
        /// Unity engine, such as dispatching the Unity data to runtime and returning the data from runtime to the
        /// Unity engine.
        /// </summary>
        /// <param name="xrInstance">The XrInstance is provided by the Unity OpenXR Plugin.</param>
        /// <param name="xrGetInstanceProcAddrFuncPtr">Accessor for xrGetInstanceProcAddr function pointer.</param>
        /// <returns>Return true if get successfully. False otherwise.</returns>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "secondary_view_configuration_get_function_address")]
        private static extern bool SecondaryViewConfigurationGetFunctionAddress
        (
            XrInstance xrInstance,
            IntPtr xrGetInstanceProcAddrFuncPtr
        );

        /// <summary>
        /// Call this function to dispatch/hook all OpenXR functions to native plug-ins.
        /// </summary>
        /// <param name="graphicsAPI">The graphics backend adopted in the Unity engine.</param>
        /// <param name="graphicsUVStartsAtTop">The bool value represents whether the texture UV coordinate convention for this platform has Y starting at the top of the image.</param>
        /// <param name="func">xrGetInstanceProcAddr native function pointer.</param>
        /// <param name="setSecondaryViewConfigurationStateDelegate">The delegate function pointer that functions types as "SetSecondaryViewConfigurationStateDelegate".</param>
        /// <param name="stopEnableSecondaryViewConfigurationDelegate">The delegate function pointer that functions types as "StopEnableSecondaryViewConfigurationDelegate".</param>
        /// <param name="setTextureSizeDelegate">The delegate function pointer that functions types as "SetTextureSizeDelegate".</param>
        /// <param name="setFovDelegate">The delegate function pointer that functions types as "SetFovDelegate".</param>
        /// <returns></returns>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "secondary_view_configuration_intercept_openxr_method")]
        private static extern IntPtr SecondaryViewConfigurationInterceptOpenXRMethod
        (
            GraphicsAPI graphicsAPI,
            bool graphicsUVStartsAtTop,
            IntPtr func,
            [MarshalAs(UnmanagedType.FunctionPtr)]
            SetSecondaryViewConfigurationStateDelegate setSecondaryViewConfigurationStateDelegate,
            [MarshalAs(UnmanagedType.FunctionPtr)]
            StopEnableSecondaryViewConfigurationDelegate stopEnableSecondaryViewConfigurationDelegate,
            [MarshalAs(UnmanagedType.FunctionPtr)] SetTextureSizeDelegate setTextureSizeDelegate,
            [MarshalAs(UnmanagedType.FunctionPtr)] SetFovDelegate setFovDelegate
        );

        /// <summary>
        /// Call this function to get the current swapchain image handler (its ID and memory address).
        /// </summary>
        /// <param name="imageIndex">The current handler index.</param>
        /// <returns>The current handler memory address.</returns>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "get_secondary_view_texture_id")]
        private static extern IntPtr GetSecondaryViewTextureId
        (
            out UInt32 imageIndex
        );

        /// <summary>
        /// Call this function to tell native plug-in submit the swapchain image immediately.
        /// </summary>
        /// <returns>Return true if submit the swapchain image successfully. False otherwise.</returns>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "release_secondary_view_texture")]
        public static extern bool ReleaseSecondaryViewTexture();

        /// <summary>
        /// Call this function to release all resources in native plug-in. Please be careful that this function should
        /// ONLY call in the Unity "OnDestroy" lifecycle event in the class "SpectatorCameraBased".
        /// </summary>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "release_all_resources")]
        public static extern void ReleaseAllResources();

        /// <summary>
        /// Call this function if requiring swapchain re-initialization. The native plug-in will set a re-initialization
        /// flag. Once the secondary view is enabled after that, the swapchain will re-init immediately.
        /// </summary>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "require_reinit_swapchain")]
        public static extern void RequireReinitSwapchain();

        /// <summary>
        /// Call this function to tell the native plug-in where the current spectator camera source comes from.
        /// </summary>
        /// <param name="isViewFromHmd">Please set true if the source comes from hmd. Otherwise, please set false.</param>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "set_view_from_hmd")]
        public static extern void SetViewFromHmd(bool isViewFromHmd);

        /// <summary>
        /// Call this function to tell the non-hmd pose to the native plug-in.
        /// </summary>
        /// <param name="pose">The current non-hmd pose</param>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "set_non_hmd_view_pose")]
        public static extern void SetNonHmdViewPose(XrPosef pose);
        
        /// <summary>
        /// Call this function to tell the native plug-in whether the texture data is ready or not.
        /// </summary>
        /// <param name="isReady">The texture data written by Unity Engine is ready or not.</param>
        [DllImport(
            dllName: ExtLib,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "set_state_secondary_view_image_data_ready")]
        public static extern void SetStateSecondaryViewImageDataReady(bool isReady);

        #endregion

        #region Utilities functions

        /// <summary>
        /// Check ViveSecondaryViewConfiguration extension is enabled or not. 
        /// </summary>
        /// <returns>Return true if enabled. False otherwise.</returns>
        public static bool IsExtensionEnabled()
        {
#if UNITY_2022_1_OR_NEWER
            return OpenXRRuntime.IsExtensionEnabled(OPEN_XR_EXTENSION_STRING);
#else
            // Does not support 2021 or lower
            return false;
#endif
        }

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

            Debug("Try to get the function pointer for XR_MSFT_secondary_view_configuration.");

            return SecondaryViewConfigurationGetFunctionAddress(xrInstance, xrGetInstanceProcAddr);

            #region Get function in C# (Just for reference)

            /* if (GetOpenXRDelegateFunction(
                    XrGetInstanceProcAddr,
                    xrInstance,
                    "xrEnumerateViewConfigurations",
                    out _xrEnumerateViewConfigurations) is false)
            {
                Error("Get delegate function of XrEnumerateViewConfigurations failed.");
                return false;
            } */

            #endregion
        }

        /// <summary>
        /// Get the specific OpenXR function.
        /// </summary>
        /// <param name="openXRFunctionPointerAccessor">The function pointer accessor provide by OpenXR.</param>
        /// <param name="openXRInstance">The XrInstance is provided by the Unity OpenXR Plugin.</param>
        /// <param name="functionName">The specific OpenXR function.</param>
        /// <param name="delegateFunction">Override value. The specific OpenXR function.</param>
        /// <typeparam name="T">The class of the delegate function.</typeparam>
        /// <returns>Return true if get successfully. False otherwise.</returns>
        private static bool GetOpenXRDelegateFunction<T>
        (
            in OpenXRHelper.xrGetInstanceProcAddrDelegate openXRFunctionPointerAccessor,
            in XrInstance openXRInstance,
            in string functionName,
            out T delegateFunction
        ) where T : class
        {
            delegateFunction = default(T);

            if (openXRFunctionPointerAccessor == null || openXRInstance == 0 || string.IsNullOrEmpty(functionName))
            {
                Error($"Get OpenXR delegate function, {functionName}, failed due to the invalid parameter(s).");
                return false;
            }

            XrResult getFunctionState = openXRFunctionPointerAccessor(openXRInstance, functionName, out IntPtr funcPtr);
            bool funcPtrIsNull = funcPtr == IntPtr.Zero;

            Debug("Get OpenXR delegate function, " + functionName + ", state: " + getFunctionState);
            Debug("Get OpenXR delegate function, " + functionName + ", funcPtrIsNull: " + funcPtrIsNull);

            if (getFunctionState != XrResult.XR_SUCCESS || funcPtrIsNull)
            {
                Error(
                    $"Get OpenXR delegate function, {functionName}, failed due to the native error or invalid return.");
                return false;
            }

            try
            {
                delegateFunction = Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(T)) as T;
            }
            catch (Exception e)
            {
                Error($"Get OpenXR delegate function, {functionName}, failed due to the exception: {e.Message}");
                return false;
            }

            Debug($"Get OpenXR delegate function, {functionName}, succeed.");

            return true;
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

        /// <summary>
        /// Get the SpectatorCameraBased component in the current Unity scene.
        /// </summary>
        /// <returns>SpectatorCameraBased array if there are any SpectatorCameraBased components. Otherwise, return null.</returns>
        private static SpectatorCameraBased[] GetSpectatorCameraBased()
        {
            var spectatorCameraBasedArray = (SpectatorCameraBased[])FindObjectsOfType(typeof(SpectatorCameraBased));
            return (spectatorCameraBasedArray != null && spectatorCameraBasedArray.Length > 0)
                ? spectatorCameraBasedArray
                : null;
        }

        /// <summary>
        /// Create a GameObject that includes SpectatorCameraBased script in Unity scene for cooperation with extension native plugins.
        /// </summary>
        private static void CreateSpectatorCameraBased()
        {
            if (IsExtensionEnabled())
            {
                Debug($"Instance.IsExistSpectatorCameraBased = {Instance.IsExistSpectatorCameraBased}");

                Instance.IsExistSpectatorCameraBased = true;

                if (GetSpectatorCameraBased() != null)
                {
                    Debug("No need to add SpectatorCameraBased because the scene already exist.");
                    return;
                }

                Debug("Start to add SpectatorCameraBased.");

                var spectatorCameraBase =
                    new GameObject("Spectator Camera Base", typeof(SpectatorCameraBased))
                    {
                        transform =
                        {
                            position = Vector3.zero,
                            rotation = Quaternion.identity
                        }
                    };

                Debug($"Create Spectator Camera Base GameObject successfully: {spectatorCameraBase != null}");
                Debug(
                    $"Included SpectatorCameraBased component: {spectatorCameraBase.GetComponent<SpectatorCameraBased>() != null}");
            }
            else
            {
                Debug("Create Spectator Camera Base GameObject failed because the related extensions are not enabled.");
            }
        }

        #endregion

        #endregion

        #region Enum definition

        /// <summary>
        /// The enum definition of supporting rendering backend.
        /// </summary>
        private enum GraphicsAPI
        {
            Unknown = 0,
            GLES3 = 1,
            Vulkan = 2
        }

        #endregion
    }
}