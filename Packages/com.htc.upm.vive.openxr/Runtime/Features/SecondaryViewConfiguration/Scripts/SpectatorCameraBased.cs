// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VIVE.OpenXR.FirstPersonObserver;
using VIVE.OpenXR.SecondaryViewConfiguration;

namespace VIVE.OpenXR.SecondaryViewConfiguration
{
    /// <summary>
    /// Name: SpectatorCameraBased.cs
    /// Role: The base class cooperating with OpenXR SecondaryViewConfiguration Extension in Unity MonoBehaviour lifecycle (Singleton)
    /// Responsibility: The handler responsible for the cooperation between the Unity MonoBehaviour lifecycle and OpenXR framework lifecycle
    /// </summary>
    public partial class SpectatorCameraBased : MonoBehaviour
    {
        private static SpectatorCameraBased _instance;

        /// <summary>
        /// SpectatorCameraBased static instance (Singleton)
        /// </summary>
        public static SpectatorCameraBased Instance => _instance;

        #region Default value definition

        /// <summary>
        /// Camera texture width
        /// </summary>
        private const int TextureWidthDefault = 1920;

        /// <summary>
        /// Camera texture height
        /// </summary>
        private const int TextureHeightDefault = 1080;

        /// <summary>
        /// Camera GameObject Based Name
        /// </summary>
        private const string CameraGameObjectBasedName = "Spectator Camera Based Object";

        /// <summary>
        /// To define how long of the time (second) that the recording state is changed
        /// </summary>
        public const float RECORDING_STATE_CHANGE_THRESHOLD_IN_SECOND = 1f;

        #endregion

#if !UNITY_EDITOR && UNITY_ANDROID
        #region OpenXR Extension

        /// <summary>
        /// ViveFirstPersonObserver OpenXR extension
        /// </summary>
        private static ViveFirstPersonObserver FirstPersonObserver => ViveFirstPersonObserver.Instance;

        /// <summary>
        /// ViveSecondaryViewConfiguration OpenXR extension
        /// </summary>
        private static ViveSecondaryViewConfiguration SecondaryViewConfiguration => ViveSecondaryViewConfiguration.Instance;

        #endregion

        #region Locker and flag for multithread safety of texture updating

        /// <summary>
        /// Locker of NeedReInitTexture variables
        /// </summary>
        private readonly object _needReInitTextureLock = new object();

        /// <summary>
        /// State of whether re-init is needed for camera texture
        /// </summary>
        private bool NeedReInitTexture { get; set; }

        /// <summary>
        /// Locker of NeedUpdateTexture variables
        /// </summary>
        private readonly object _needUpdateTextureLock = new object();

        /// <summary>
        /// State of whether updated camera texture is needed
        /// </summary>
        private bool NeedUpdateTexture { get; set; }

        #endregion
#endif

        #region Spectator camera texture variables

        /// <summary>
        /// Camera texture size
        /// </summary>
        private Vector2 CameraTargetTextureSize { get; set; }

        /// <summary>
        /// Camera texture
        /// </summary>
        private RenderTexture CameraTargetTexture { get; set; }

        #endregion

        /// <summary>
        /// GameObject of the spectator camera
        /// </summary>
        public GameObject SpectatorCameraGameObject { get; private set; }

        /// <summary>
        /// Camera component of the spectator camera
        /// </summary>
        public Camera SpectatorCamera { get; private set; }
        
        public Camera MainCamera { get; private set; }

        #region Debug Variables

        [SerializeField] private Material spectatorCameraViewMaterial;

        /// <summary>
        /// Material that show the spectator camera view
        /// </summary>
        public Material SpectatorCameraViewMaterial
        {
            get => spectatorCameraViewMaterial;
            set
            {
                spectatorCameraViewMaterial = value;
                if (spectatorCameraViewMaterial && SpectatorCamera)
                {
                    spectatorCameraViewMaterial.mainTexture = SpectatorCamera.targetTexture;
                }
            }
        }

        #endregion

        private bool _followHmd;

        /// <summary>
        /// Is the spectator camera following the HMD or not
        /// </summary>
        public bool FollowHmd
        {
            get => _followHmd;
            set
            {
                _followHmd = value;
                
                if (SpectatorCamera.transform.parent != null &&
                    (SpectatorCamera.transform.localPosition != Vector3.zero ||
                     SpectatorCamera.transform.localRotation != Quaternion.identity))
                {
                    Debug.Log("The local position or rotation should not be modified. Will reset the SpectatorCamera transform.");
                    
                    SpectatorCamera.transform.localPosition = Vector3.zero;
                    SpectatorCamera.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// State of allowing capture the 360 image or not
        /// </summary>
        public static bool IsAllowSpectatorCameraCapture360Image =>
#if !UNITY_EDITOR && UNITY_ANDROID
            SecondaryViewConfiguration.IsAllowSpectatorCameraCapture360Image
#else
            true
#endif
        ;

        /// <summary>
        /// SpectatorCameraBased init success or not
        /// </summary>
        private bool InitSuccess { get; set; }

        /// <summary>
        /// State of whether the app is not be focusing by the user
        /// </summary>
        private bool IsInBackground { get; set; }

        [SerializeField, Tooltip("State of whether the spectator camera is recording currently")]
        private bool isRecording;

        /// <summary>
        /// State of whether the spectator camera is recording currently
        /// </summary>
        public bool IsRecording
        {
            get => isRecording;
            set
            {
                isRecording = value;

                if (value)
                {
                    if (IsPerformedStartRecordingCallback)
                    {
                        return;
                    }

                    IsPerformedStartRecordingCallback = true;
                    IsPerformedCloseRecordingCallback = false;
                    OnSpectatorStart?.Invoke();
                }
                else
                {
                    if (IsPerformedCloseRecordingCallback ||
                        /* Because OpenXR periodically changes the spectator enabled flag, we need
                         to consider checking the state with a time delay so that we can make sure
                         it is changing for a long while or just periodically. */
                        Math.Abs(LastRecordingStateIsDisableTime - LastRecordingStateIsActiveTime) <
                        RECORDING_STATE_CHANGE_THRESHOLD_IN_SECOND)
                    {
                        return;
                    }

                    IsPerformedCloseRecordingCallback = true;
                    IsPerformedStartRecordingCallback = false;
                    OnSpectatorStop?.Invoke();
                }
            }
        }

        /// <summary>
        /// The last time of the recording state that is active.
        /// </summary>
        public float LastRecordingStateIsActiveTime { get; private set; }

        /// <summary>
        /// The last time of the recording state that is disable.
        /// </summary>
        public float LastRecordingStateIsDisableTime { get; private set; }

        /// <summary>
        /// Flag denotes the callback is performed when the recording state changes to active
        /// </summary>
        private bool IsPerformedStartRecordingCallback { get; set; }

        /// <summary>
        /// Flag denotes the callback is performed when the recording state changes to disable
        /// </summary>
        private bool IsPerformedCloseRecordingCallback { get; set; }

        #region Public variables for register the delegate callback functions

        /// <summary>
        /// Delegate type for spectator camera callbacks.
        /// A delegate declaration that can encapsulate a method that takes no argument and returns void.
        /// </summary>
        public delegate void SpectatorCameraCallback();

        /// <summary>
        /// Delegate that custom code is executed when the spectator camera state changes to active.
        /// </summary>
        public SpectatorCameraCallback OnSpectatorStart;

        /// <summary>
        /// Delegate that custom code is executed when the spectator camera state changes to disable.
        /// </summary>
        public SpectatorCameraCallback OnSpectatorStop;

        #endregion

#if !UNITY_EDITOR && UNITY_ANDROID
        /// <summary>
        /// Set the flag NeedReInitTexture as true
        /// </summary>
        /// <param name="size">The re-init texture size</param>
        private void OnTextureSizeUpdated(Vector2 size)
        {
            lock (_needReInitTextureLock)
            {
                NeedReInitTexture = true;
                CameraTargetTextureSize = size;
            }
        }

        /// <summary>
        /// Set the flag NeedUpdateTexture as true
        /// </summary>
        private void OnTextureUpdated()
        {
            lock (_needUpdateTextureLock)
            {
                NeedUpdateTexture = true;
            }
        }

        /// <summary>
        /// Init the projection matrix of spectator camera
        /// </summary>
        /// <param name="left">The position of the left vertical plane of the viewing frustum</param>
        /// <param name="right">The position of the right vertical plane of the viewing frustum</param>
        /// <param name="top">The position of the top horizontal plane of the viewing frustum</param>
        /// <param name="bottom">The position of the bottom horizontal plane of the viewing frustum</param>
        private void OnFovUpdated(float left, float right, float top, float bottom)
        {
            #region Modify the camera projection matrix (No need, just for reference)
            
            /*
            if (SpectatorCamera)
            {
                float far = SpectatorCamera.farClipPlane;
                float near = SpectatorCamera.nearClipPlane;
                SpectatorCamera.projectionMatrix = new Matrix4x4()
                {
                    [0, 0] = 2f / (right - left),
                    [0, 1] = 0,
                    [0, 2] = (right + left) / (right - left),
                    [0, 3] = 0,
                    [1, 0] = 0,
                    [1, 1] = 2f / (top - bottom),
                    [1, 2] = (top + bottom) / (top - bottom),
                    [1, 3] = 0,
                    [2, 0] = 0,
                    [2, 1] = 0,
                    [2, 2] = -(far + near) / (far - near),
                    [2, 3] = -(2f * far * near) / (far - near),
                    [3, 0] = 0,
                    [3, 1] = 0,
                    [3, 2] = -1f,
                    [3, 3] = 0,
                };
            }
            */
            
            #endregion
        }
#endif

        /// <summary>
        /// Init the camera texture
        /// </summary>
        private void InitCameraTargetTexture()
        {
            if (CameraTargetTextureSize.x == 0 || CameraTargetTextureSize.y == 0)
            {
#if !UNITY_EDITOR && UNITY_ANDROID
                if (SecondaryViewConfiguration.TextureSize.x == 0 || SecondaryViewConfiguration.TextureSize.y == 0)
                {
                    CameraTargetTextureSize = new Vector2(TextureWidthDefault, TextureHeightDefault);
                }
                else
                {
                    CameraTargetTextureSize = SecondaryViewConfiguration.TextureSize;
                }
#else
                CameraTargetTextureSize = new Vector2(TextureWidthDefault, TextureHeightDefault);
#endif
            }

            if (!CameraTargetTexture)
            {
                // Texture is not create yet. Create it.
                CameraTargetTexture = new RenderTexture
                (
                    (int)CameraTargetTextureSize.x,
                    (int)CameraTargetTextureSize.y,
                    24,
                    RenderTextureFormat.ARGB32
                );

                InitPostProcessing();
                return;
            }

            if (CameraTargetTexture.width == (int)CameraTargetTextureSize.x &&
                CameraTargetTexture.height == (int)CameraTargetTextureSize.y)
            {
                // Texture size is same, just return.
                return;
            }

            // Release the last time resource
            SpectatorCamera.targetTexture = null;
            if (SpectatorCameraViewMaterial)
            {
                SpectatorCameraViewMaterial.mainTexture = null;
            }

            CameraTargetTexture.Release();

            // Re-init
            CameraTargetTexture.width = (int)CameraTargetTextureSize.x;
            CameraTargetTexture.height = (int)CameraTargetTextureSize.y;
            CameraTargetTexture.depth = 24;
            CameraTargetTexture.format = RenderTextureFormat.ARGB32;

            InitPostProcessing();
            return;

            void InitPostProcessing()
            {
                if (!CameraTargetTexture.IsCreated())
                {
                    Debug.Log("The RenderTexture is not create yet. Will create it.");

                    bool created = CameraTargetTexture.Create();

                    Debug.Log($"Try to create RenderTexture: {created}");

                    if (created)
                    {
                        SpectatorCamera.targetTexture = CameraTargetTexture;
                        if (SpectatorCameraViewMaterial)
                        {
                            SpectatorCameraViewMaterial.mainTexture = SpectatorCamera.targetTexture;
                        }
                    }
                }
                else
                {
                    Debug.Log("The RenderTexture is already created.");
                }
            }
        }

#if !UNITY_EDITOR && UNITY_ANDROID
        /// <summary>
        /// Update camera texture and then copy data of the camera texture to native texture space
        /// </summary>
        private void SecondViewTextureUpdate()
        {
            if (SecondaryViewConfiguration.MyTexture)
            {
                SpectatorCamera.enabled = true;
                SpectatorCamera.Render();
                SpectatorCamera.enabled = false;

                if (SpectatorCamera.targetTexture)
                {
                    // Copy Unity texture data to native texture
                    Graphics.CopyTexture(
                        SpectatorCamera.targetTexture,
                        0,
                        0,
                        SecondaryViewConfiguration.MyTexture,
                        0,
                        0);
                }
                else
                {
                    Debug.LogError("Cannot copy the rendering data because the camera target texture is null!");
                }

                // Call native function that finishes the texture update
                ViveSecondaryViewConfiguration.ReleaseSecondaryViewTexture();
            }
            else
            {
                Debug.LogError("Cannot copy the rendering data because SecondaryViewConfiguration.MyTexture is null!");
            }
        }
#endif

        /// <summary>
        /// Set the main texture of SpectatorCameraViewMaterial material as spectator camera texture
        /// </summary>
        private void SetCameraBasedTargetTexture2SpectatorCameraViewMaterial()
        {
            if (SpectatorCameraViewMaterial)
            {
                SpectatorCameraViewMaterial.mainTexture = SpectatorCamera.targetTexture;
            }
        }

        /// <summary>
        /// Set the main texture of SpectatorCameraViewMaterial material as NULL value
        /// </summary>
        private void SetNull2SpectatorCameraViewMaterial()
        {
            if (SpectatorCameraViewMaterial)
            {
                SpectatorCameraViewMaterial.mainTexture = null;
            }
        }

        /// <summary>
        /// Set whether the current camera viewpoint comes from HMD or not
        /// </summary>
        /// <param name="isViewFromHmd">The bool value represents the current view of whether the spectator camera is coming from hmd or not.</param>
        public void SetViewFromHmd(bool isViewFromHmd)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            ViveSecondaryViewConfiguration.SetViewFromHmd(isViewFromHmd);
#endif
            FollowHmd = isViewFromHmd;
        }

        /// <summary>
        /// Get MainCamera in the current scene.
        /// </summary>
        /// <returns>The Camera component with MainCamera tag in the current scene</returns>
        public static Camera GetMainCamera()
        {
            return Camera.main;
        }

        #region Unity life-cycle event

        private void Start()
        {
            InitSuccess = false;

            if (_instance != null && _instance != this)
            {
                Debug.Log("Destroy the SpectatorCameraBased");
                if (SpectatorCameraViewMaterial)
                {
                    Debug.Log("Copy SpectatorCameraBased material setting before destroy.");
                    _instance.SpectatorCameraViewMaterial = SpectatorCameraViewMaterial;
                }

                DestroyImmediate(this);
                return;
            }
            else
            {
                _instance = this;

                // To prevent this from being destroyed on load, check whether this gameObject has a parent;
                // if so, set it to no game parent.
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            if (SecondaryViewConfiguration && FirstPersonObserver)
            {
                // To check, "XR_MSFT_first_person_observer" is enough because it
                // requires "XR_MSFT_secondary_view_configuration" to be enabled also.
                if (!ViveFirstPersonObserver.IsExtensionEnabled())
                {
                    Debug.LogWarning(
                        $"The OpenXR extension, {ViveSecondaryViewConfiguration.OPEN_XR_EXTENSION_STRING} " +
                        $"or {ViveFirstPersonObserver.OPEN_XR_EXTENSION_STRING}, is disabled. " +
                        "Please enable the extension before building the app.");
                    Debug.Log("Destroy the SpectatorCameraBased");
                    DestroyImmediate(this);
                    return;
                }

                SecondaryViewConfiguration.onTextureSizeUpdated += OnTextureSizeUpdated;
                SecondaryViewConfiguration.onTextureUpdated += OnTextureUpdated;
                SecondaryViewConfiguration.onFovUpdated += OnFovUpdated;
            }
            else
            {
                Debug.LogError(
                    "Cannot find the static instance of ViveSecondaryViewConfiguration or ViveFirstPersonObserver," +
                    " pls reopen the app later.");
                Debug.Log("Destroy the SpectatorCameraBased");
                DestroyImmediate(this);
                return;
            }

            bool isSecondaryViewAlreadyEnabled = SecondaryViewConfiguration.IsEnabled;
            Debug.Log(
                $"The state of ViveSecondaryViewConfiguration.IsEnabled is {isSecondaryViewAlreadyEnabled}");
            lock (_needReInitTextureLock)
            {
                NeedReInitTexture = isSecondaryViewAlreadyEnabled;
            }

            lock (_needUpdateTextureLock)
            {
                NeedUpdateTexture = isSecondaryViewAlreadyEnabled;
            }

            IsRecording = isSecondaryViewAlreadyEnabled;
#endif

            SpectatorCameraGameObject = new GameObject(CameraGameObjectBasedName)
            {
                transform = { position = Vector3.zero, rotation = Quaternion.identity }
            };
            DontDestroyOnLoad(SpectatorCameraGameObject);

            SpectatorCamera = SpectatorCameraGameObject.AddComponent<Camera>();
            SpectatorCamera.stereoTargetEye = StereoTargetEyeMask.None;
            MainCamera = GetMainCamera();
            if (MainCamera != null)
            {
                // Set spectator camera to render after the main camera
                SpectatorCamera.depth = MainCamera.depth + 1;
            }
            // Manually call Render() function once time at Start()
            // because it can reduce the performance impact of first-time calls at SecondViewTextureUpdate
            SpectatorCamera.Render();
            SpectatorCamera.enabled = false;

            FollowHmd = true;
            IsInBackground = false;
            IsPerformedStartRecordingCallback = false;
            IsPerformedCloseRecordingCallback = false;
            LastRecordingStateIsActiveTime = 0f;
            LastRecordingStateIsDisableTime = 0f;

            OnSpectatorStart += SetCameraBasedTargetTexture2SpectatorCameraViewMaterial;
            OnSpectatorStop += SetNull2SpectatorCameraViewMaterial;
            SceneManager.sceneLoaded += OnSceneLoaded;

#if !UNITY_EDITOR && UNITY_ANDROID
            if (isSecondaryViewAlreadyEnabled)
            {
                OnSpectatorStart?.Invoke();
            }
#endif

#if UNITY_EDITOR
            OnSpectatorStart += () => { SpectatorCamera.enabled = true; };
            OnSpectatorStop += () => { SpectatorCamera.enabled = false; };

            CameraTargetTextureSize = new Vector2
            (
                TextureWidthDefault,
                TextureHeightDefault
            );
            InitCameraTargetTexture();
            SpectatorCamera.enabled = IsDebugSpectatorCamera && IsRecording;
#endif

            InitSuccess = true;
        }

        private void LateUpdate()
        {
            if (!InitSuccess)
            {
                return;
            }

            if (IsInBackground)
            {
                return;
            }
            
            if (SpectatorCamera.transform.parent != null &&
                (SpectatorCamera.transform.localPosition != Vector3.zero ||
                 SpectatorCamera.transform.localRotation != Quaternion.identity))
            {
                Debug.Log("The local position or rotation should not be modified. Will reset the SpectatorCamera transform.");
                
                SpectatorCamera.transform.localPosition = Vector3.zero;
                SpectatorCamera.transform.localRotation = Quaternion.identity;
            }

            if (FollowHmd)
            {
                if (MainCamera != null || (MainCamera = GetMainCamera()) != null)
                {
                    Transform spectatorCameraTransform = SpectatorCamera.transform;
                    Transform hmdCameraTransform = MainCamera.transform;
                    spectatorCameraTransform.position = hmdCameraTransform.position;
                    spectatorCameraTransform.rotation = hmdCameraTransform.rotation;
                }
            }
            else
            {
#if !UNITY_EDITOR && UNITY_ANDROID
                if (!SecondaryViewConfiguration.IsStopped)
                {
                    Transform referenceTransform = SpectatorCamera.transform;

                    // Left-handed coordinate system (Unity) -> right-handed coordinate system (OpenXR)
                    var spectatorCameraPositionInOpenXRSpace = new XrVector3f
                    (
                        referenceTransform.position.x,
                        referenceTransform.position.y,
                        -referenceTransform.position.z
                    );

                    var spectatorCameraQuaternionInOpenXRSpace = new XrQuaternionf
                    (
                        referenceTransform.rotation.x,
                        referenceTransform.rotation.y,
                        -referenceTransform.rotation.z,
                        -referenceTransform.rotation.w
                    );

                    var spectatorCameraPose = new XrPosef
                    (
                        spectatorCameraQuaternionInOpenXRSpace,
                        spectatorCameraPositionInOpenXRSpace
                    );
                    
                    ViveSecondaryViewConfiguration.SetNonHmdViewPose(spectatorCameraPose);
                }
#endif
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            IsRecording = SecondaryViewConfiguration.IsEnabled;
#endif
            if (IsRecording)
            {
                LastRecordingStateIsActiveTime = Time.unscaledTime;
            }
            else
            {
                LastRecordingStateIsDisableTime = Time.unscaledTime;

                if (!IsPerformedCloseRecordingCallback &&
                    /* Because OpenXR periodically changes the spectator enabled flag, we need
                     to consider checking the state with a time delay so that we can make sure
                     it is changing for a long while or just periodically. */
                    Math.Abs(LastRecordingStateIsDisableTime - LastRecordingStateIsActiveTime) >
                    RECORDING_STATE_CHANGE_THRESHOLD_IN_SECOND)
                {
                    IsPerformedCloseRecordingCallback = true;
                    IsPerformedStartRecordingCallback = false;
                    OnSpectatorStop?.Invoke();
                }

                return;
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            lock (_needReInitTextureLock)
            {
                if (NeedReInitTexture)
                {
                    NeedReInitTexture = false;
                    InitCameraTargetTexture();
                }
            }

            lock (_needUpdateTextureLock)
            {
                if (NeedUpdateTexture)
                {
                    NeedUpdateTexture = false;
                    ViveSecondaryViewConfiguration.SetStateSecondaryViewImageDataReady(false);
                    SecondViewTextureUpdate();
                    ViveSecondaryViewConfiguration.SetStateSecondaryViewImageDataReady(true);
                }
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!InitSuccess)
            {
                Debug.Log("Init unsuccessfully, just return from SpectatorCameraBased.OnApplicationFocus.");
                return;
            }

            Debug.Log($"SpectatorCameraBased.OnApplicationFocus: {hasFocus}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!InitSuccess)
            {
                Debug.Log("Init unsuccessfully, just return from SpectatorCameraBased.OnApplicationPause.");
                return;
            }

            Debug.Log($"SpectatorCameraBased.OnApplicationPause: {pauseStatus}");

#if !UNITY_EDITOR && UNITY_ANDROID
            // Need to re-create the swapchain when recording is active and Unity app is resumed
            if (SecondaryViewConfiguration.IsEnabled && !pauseStatus)
            {
                ViveSecondaryViewConfiguration.RequireReinitSwapchain();
            }
#endif

            IsInBackground = pauseStatus;
        }

        private void OnDestroy()
        {
            if (!InitSuccess)
            {
                Debug.Log("Init unsuccessfully, just return from SpectatorCameraBased.OnDestroy.");
                return;
            }

            Debug.Log("SpectatorCameraBased.OnDestroy");

#if !UNITY_EDITOR && UNITY_ANDROID
            SecondaryViewConfiguration.onTextureSizeUpdated -= OnTextureSizeUpdated;
            SecondaryViewConfiguration.onTextureUpdated -= OnTextureUpdated;
#endif

            if (SpectatorCamera)
            {
                SpectatorCamera.targetTexture = null;
            }

            if (SpectatorCameraViewMaterial)
            {
                SpectatorCameraViewMaterial.mainTexture = null;
            }

            if (CameraTargetTexture)
            {
                Destroy(CameraTargetTexture);
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            ViveSecondaryViewConfiguration.ReleaseAllResources();
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!InitSuccess)
            {
                Debug.Log("Init unsuccessfully, just return from SpectatorCameraBased.OnSceneLoaded.");
                return;
            }

            Debug.Log($"SpectatorCameraBased.OnSceneLoaded: {scene.name}");
            
            MainCamera = GetMainCamera();

#if !UNITY_EDITOR && UNITY_ANDROID
            if (!SecondaryViewConfiguration.IsStopped)
            {
                // Need to re-init the swapchain when recording is active and new Unity scene is loaded
                ViveSecondaryViewConfiguration.RequireReinitSwapchain();
            }
#endif
        }

        #endregion
    }
}