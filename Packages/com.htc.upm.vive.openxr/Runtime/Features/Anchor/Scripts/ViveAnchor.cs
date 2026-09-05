// Copyright HTC Corporation All Rights Reserved.

// Remove FAKE_DATA if editor or windows is supported.
#if UNITY_EDITOR
#define FAKE_DATA
#endif

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.Feature
{
    using XrPersistedAnchorCollectionHTC = System.IntPtr;

#if UNITY_EDITOR
    [OpenXRFeature(UiName = "VIVE XR Anchor (Beta)",
        Desc = "VIVE's implementaion of the XR_HTC_anchor.",
        Company = "HTC",
        DocumentationLink = "..\\Documentation",
        OpenxrExtensionStrings = kOpenxrExtensionString,
        Version = "1.0.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        FeatureId = featureId
    )]
#endif
    public class ViveAnchor : OpenXRFeature
    {
        public const string kOpenxrExtensionString = "XR_HTC_anchor XR_EXT_future XR_HTC_anchor_persistence";

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "vive.openxr.feature.htcanchor";

        /// <summary>
        /// Enable or disable the persisted anchor feature.  Set it only valid in feature settings.
        /// </summary>
        public bool enablePersistedAnchor = true;
        private XrInstance m_XrInstance = 0;
        private XrSession session = 0;
        private XrSystemId m_XrSystemId = 0;
        private bool IsInited = false;
        private bool IsPAInited = false;
        private bool useFakeData = false;

        #region struct, enum, const of this extensions

        /// <summary>
        /// An application can inspect whether the system is capable of anchor functionality by 
        /// chaining an XrSystemAnchorPropertiesHTC structure to the XrSystemProperties when calling
        /// xrGetSystemProperties.The runtime must return XR_ERROR_FEATURE_UNSUPPORTED if
        /// XrSystemAnchorPropertiesHTC::supportsAnchor was XR_FALSE.
        /// supportsAnchor indicates if current system is capable of anchor functionality.
        /// </summary>
        public struct XrSystemAnchorPropertiesHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrBool32 supportsAnchor;
        }

        /// <summary>
        /// name is a null-terminated UTF-8 string whose length is less than or equal to XR_MAX_SPATIAL_ANCHOR_NAME_SIZE_HTC.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct XrSpatialAnchorNameHTC
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] name;

            public XrSpatialAnchorNameHTC(string anchorName)
            {
                name = new byte[256];
                byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(anchorName);
                Array.Copy(utf8Bytes, name, Math.Min(utf8Bytes.Length, 255));
                name[255] = 0;
            }

            public XrSpatialAnchorNameHTC(XrSpatialAnchorNameHTC anchorName)
            {
                name = new byte[256];
                Array.Copy(anchorName.name, name, 256);
                name[255] = 0;
            }

            public override readonly string ToString() {
                if (name == null)
                    return string.Empty;
                return System.Text.Encoding.UTF8.GetString(name).TrimEnd('\0');
            }
        }

        public struct XrSpatialAnchorCreateInfoHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrSpace space;
            public XrPosef poseInSpace;
            public XrSpatialAnchorNameHTC name;
        }

        public struct XrPersistedAnchorCollectionAcquireInfoHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
        }

        public struct XrPersistedAnchorCollectionAcquireCompletionHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrResult futureResult;
            public System.IntPtr persistedAnchorCollection;
        }

        public struct XrSpatialAnchorPersistInfoHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrSpace anchor;
            public XrSpatialAnchorNameHTC persistedAnchorName;
        }

        public struct XrSpatialAnchorFromPersistedAnchorCreateInfoHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public System.IntPtr persistedAnchorCollection;
            public XrSpatialAnchorNameHTC persistedAnchorName;
            public XrSpatialAnchorNameHTC spatialAnchorName;
        }

        public struct XrSpatialAnchorFromPersistedAnchorCreateCompletionHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public XrResult futureResult;
            public XrSpace anchor;
        }

        public struct XrPersistedAnchorPropertiesGetInfoHTC
        {
            public XrStructureType type;
            public System.IntPtr next;
            public uint maxPersistedAnchorCount;
        }

        #endregion

        #region delegates and delegate instances
        public delegate XrResult DelegateXrCreateSpatialAnchorHTC(XrSession session, ref XrSpatialAnchorCreateInfoHTC createInfo, ref XrSpace anchor);
        public delegate XrResult DelegateXrGetSpatialAnchorNameHTC(XrSpace anchor, ref XrSpatialAnchorNameHTC name);
        public delegate XrResult DelegateXrAcquirePersistedAnchorCollectionAsyncHTC(XrSession session, ref XrPersistedAnchorCollectionAcquireInfoHTC acquireInfo, out IntPtr future);
        public delegate XrResult DelegateXrAcquirePersistedAnchorCollectionCompleteHTC(IntPtr future, out XrPersistedAnchorCollectionAcquireCompletionHTC completion);
        public delegate XrResult DelegateXrReleasePersistedAnchorCollectionHTC(IntPtr persistedAnchorCollection);
        public delegate XrResult DelegateXrPersistSpatialAnchorAsyncHTC(XrPersistedAnchorCollectionHTC persistedAnchorCollection, ref XrSpatialAnchorPersistInfoHTC persistInfo, out IntPtr future);
        public delegate XrResult DelegateXrPersistSpatialAnchorCompleteHTC(IntPtr future, out FutureWrapper.XrFutureCompletionEXT completion);
        public delegate XrResult DelegateXrUnpersistSpatialAnchorHTC(IntPtr persistedAnchorCollection, ref XrSpatialAnchorNameHTC persistedAnchorName);
        public delegate XrResult DelegateXrEnumeratePersistedAnchorNamesHTC( IntPtr persistedAnchorCollection, uint persistedAnchorNameCapacityInput, ref uint persistedAnchorNameCountOutput, [Out] XrSpatialAnchorNameHTC[] persistedAnchorNames);
        public delegate XrResult DelegateXrCreateSpatialAnchorFromPersistedAnchorAsyncHTC(XrSession session, ref XrSpatialAnchorFromPersistedAnchorCreateInfoHTC spatialAnchorCreateInfo, out IntPtr future);
        public delegate XrResult DelegateXrCreateSpatialAnchorFromPersistedAnchorCompleteHTC(IntPtr future, out XrSpatialAnchorFromPersistedAnchorCreateCompletionHTC completion);
        public delegate XrResult DelegateXrClearPersistedAnchorsHTC(IntPtr persistedAnchorCollection);
        public delegate XrResult DelegateXrGetPersistedAnchorPropertiesHTC(IntPtr persistedAnchorCollection, ref XrPersistedAnchorPropertiesGetInfoHTC getInfo);
        public delegate XrResult DelegateXrExportPersistedAnchorHTC(IntPtr persistedAnchorCollection, ref XrSpatialAnchorNameHTC persistedAnchorName, uint dataCapacityInput, ref uint dataCountOutput, [Out] byte[] data);
        public delegate XrResult DelegateXrImportPersistedAnchorHTC(IntPtr persistedAnchorCollection, uint dataCount, [In] byte[] data);
        public delegate XrResult DelegateXrGetPersistedAnchorNameFromBufferHTC(IntPtr persistedAnchorCollection, uint bufferCount, byte[] buffer, ref XrSpatialAnchorNameHTC name);

        DelegateXrCreateSpatialAnchorHTC XrCreateSpatialAnchorHTC;
        DelegateXrGetSpatialAnchorNameHTC XrGetSpatialAnchorNameHTC;
        DelegateXrAcquirePersistedAnchorCollectionAsyncHTC XrAcquirePersistedAnchorCollectionAsyncHTC;
        DelegateXrAcquirePersistedAnchorCollectionCompleteHTC XrAcquirePersistedAnchorCollectionCompleteHTC;
        DelegateXrReleasePersistedAnchorCollectionHTC XrReleasePersistedAnchorCollectionHTC;
        DelegateXrPersistSpatialAnchorAsyncHTC XrPersistSpatialAnchorAsyncHTC;
        DelegateXrPersistSpatialAnchorCompleteHTC XrPersistSpatialAnchorCompleteHTC;
        DelegateXrUnpersistSpatialAnchorHTC XrUnpersistSpatialAnchorHTC;
        DelegateXrEnumeratePersistedAnchorNamesHTC XrEnumeratePersistedAnchorNamesHTC;
        DelegateXrCreateSpatialAnchorFromPersistedAnchorAsyncHTC XrCreateSpatialAnchorFromPersistedAnchorAsyncHTC;
        DelegateXrCreateSpatialAnchorFromPersistedAnchorCompleteHTC XrCreateSpatialAnchorFromPersistedAnchorCompleteHTC;
        DelegateXrClearPersistedAnchorsHTC XrClearPersistedAnchorsHTC;
        DelegateXrGetPersistedAnchorPropertiesHTC XrGetPersistedAnchorPropertiesHTC;
        DelegateXrExportPersistedAnchorHTC XrExportPersistedAnchorHTC;
        DelegateXrImportPersistedAnchorHTC XrImportPersistedAnchorHTC;
        DelegateXrGetPersistedAnchorNameFromBufferHTC XrGetPersistedAnchorNameFromBufferHTC;

        #endregion delegates and delegate instances

        #region override functions

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            // For LocateSpace, need WaitFrame's predictedDisplayTime.
            ViveInterceptors.Instance.AddRequiredFunction("xrWaitFrame");
            return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
        }

        /// <inheritdoc />
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
#if FAKE_DATA
            Debug.LogError("ViveAnchor OnInstanceCreate() Use FakeData");
            useFakeData = true;
#endif
            IsInited = false;
            bool ret = true;
            ret &= CommonWrapper.Instance.OnInstanceCreate(xrInstance, xrGetInstanceProcAddr);
            ret &= SpaceWrapper.Instance.OnInstanceCreate(xrInstance, xrGetInstanceProcAddr);

            if (!ret)
            {
                Debug.LogError("ViveAnchor OnInstanceCreate() failed.");
                return false;
            }

            //Debug.Log("VIVEAnchor OnInstanceCreate() ");
            if (!OpenXRRuntime.IsExtensionEnabled("XR_HTC_anchor") && !useFakeData)
            {
                Debug.LogWarning("ViveAnchor OnInstanceCreate() XR_HTC_anchor is NOT enabled.");
                return false;
            }

            IsInited = GetXrFunctionDelegates(xrInstance);

            if (!IsInited)
            {
                Debug.LogError("ViveAnchor OnInstanceCreate() failed to get function delegates.");
                return false;
            }

            m_XrInstance = xrInstance;

            bool hasFuture = FutureWrapper.Instance.OnInstanceCreate(xrInstance, xrGetInstanceProcAddr);
            // No error log because future will print.
#if FAKE_DATA
            hasFuture = true;
#endif
            IsPAInited = false;
            bool hasPersistedAnchor = false;
            do
            {
                if (!hasFuture)
                {
                    Debug.LogWarning("ViveAnchor OnInstanceCreate() XR_HTC_anchor_persistence is NOT enabled because no XR_EXT_future.");
                    hasPersistedAnchor = false;
                    break;
                }

                hasPersistedAnchor = enablePersistedAnchor && OpenXRRuntime.IsExtensionEnabled("XR_HTC_anchor_persistence");
#if FAKE_DATA
                hasPersistedAnchor = enablePersistedAnchor;
#endif
            } while(false);

            //Debug.Log("OnInstanceCreate() " + m_XrInstance);
            if (hasPersistedAnchor)
                IsPAInited = GetXrFunctionDelegatesPersistance(xrInstance);
            if (!IsPAInited)
                Debug.LogWarning("ViveAnchor OnInstanceCreate() XR_HTC_anchor_persistence is NOT enabled.");

            return IsInited;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            m_XrInstance = 0;

            IsInited = false;
            IsPAInited = false;

            CommonWrapper.Instance.OnInstanceDestroy();
            SpaceWrapper.Instance.OnInstanceDestroy();
            FutureWrapper.Instance.OnInstanceDestroy();
            Debug.Log("ViveAnchor: OnInstanceDestroy()");
        }

        /// <inheritdoc />
        protected override void OnSessionCreate(ulong xrSession)
        {
            //Debug.Log("ViveAnchor OnSessionCreate() ");
            session = xrSession;
        }

        /// <inheritdoc />
        protected override void OnSessionDestroy(ulong xrSession)
        {
            //Debug.Log("ViveAnchor OnSessionDestroy() ");
            session = 0;
        }

        // XXX Every millisecond the AppSpace switched from one space to another space. I don't know what is going on.
        //private ulong appSpace;
        //protected override void OnAppSpaceChange(ulong space)
        //{
        //    //Debug.Log($"VIVEAnchor OnAppSpaceChange({appSpace} -> {space})");
        //    appSpace = space;
        //}

        /// <inheritdoc />
        protected override void OnSystemChange(ulong xrSystem)
        {
            m_XrSystemId = xrSystem;
            //Debug.Log("ViveAnchor OnSystemChange() " + m_XrSystemId);
        }

        #endregion override functions

        private bool GetXrFunctionDelegates(XrInstance inst)
        {
            Debug.Log("ViveAnchor GetXrFunctionDelegates() ");

            bool ret = true;
            OpenXRHelper.xrGetInstanceProcAddrDelegate GetAddr = CommonWrapper.Instance.GetInstanceProcAddr;  // shorter name
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrCreateSpatialAnchorHTC", out XrCreateSpatialAnchorHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrGetSpatialAnchorNameHTC", out XrGetSpatialAnchorNameHTC);

            return ret;
        }

        private bool GetXrFunctionDelegatesPersistance(XrInstance inst)
        {
            Debug.Log("ViveAnchor GetXrFunctionDelegatesPersistance() ");
            bool ret = true;
            OpenXRHelper.xrGetInstanceProcAddrDelegate GetAddr = CommonWrapper.Instance.GetInstanceProcAddr;  // shorter name
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrAcquirePersistedAnchorCollectionAsyncHTC", out XrAcquirePersistedAnchorCollectionAsyncHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrAcquirePersistedAnchorCollectionCompleteHTC", out XrAcquirePersistedAnchorCollectionCompleteHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrReleasePersistedAnchorCollectionHTC", out XrReleasePersistedAnchorCollectionHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrPersistSpatialAnchorAsyncHTC", out XrPersistSpatialAnchorAsyncHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrPersistSpatialAnchorCompleteHTC", out XrPersistSpatialAnchorCompleteHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrUnpersistSpatialAnchorHTC", out XrUnpersistSpatialAnchorHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrEnumeratePersistedAnchorNamesHTC", out XrEnumeratePersistedAnchorNamesHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrCreateSpatialAnchorFromPersistedAnchorAsyncHTC", out XrCreateSpatialAnchorFromPersistedAnchorAsyncHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrCreateSpatialAnchorFromPersistedAnchorCompleteHTC", out XrCreateSpatialAnchorFromPersistedAnchorCompleteHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrClearPersistedAnchorsHTC", out XrClearPersistedAnchorsHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrGetPersistedAnchorPropertiesHTC", out XrGetPersistedAnchorPropertiesHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrExportPersistedAnchorHTC", out XrExportPersistedAnchorHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrImportPersistedAnchorHTC", out XrImportPersistedAnchorHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(GetAddr, inst, "xrGetPersistedAnchorNameFromBufferHTC", out XrGetPersistedAnchorNameFromBufferHTC);

            return ret;
        }

        #region functions of extension
        /// <summary>
        /// Helper function to get this feature's properties.
        /// See <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrGetSystemProperties">xrGetSystemProperties</see>
        /// </summary>
        /// <param name="anchorProperties">Output parameter to hold anchor properties.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult GetProperties(out XrSystemAnchorPropertiesHTC anchorProperties)
        {
            anchorProperties = new XrSystemAnchorPropertiesHTC();
            anchorProperties.type = XrStructureType.XR_TYPE_SYSTEM_ANCHOR_PROPERTIES_HTC;

#if FAKE_DATA
            if (Application.isEditor)
            {
                anchorProperties.type = XrStructureType.XR_TYPE_SYSTEM_ANCHOR_PROPERTIES_HTC;
                anchorProperties.supportsAnchor = true;
                return XrResult.XR_SUCCESS;
            }
#endif
            return CommonWrapper.Instance.GetProperties(m_XrInstance, m_XrSystemId, ref anchorProperties);
        }

        /// <summary>
        /// The CreateSpatialAnchor function creates a spatial anchor with specified base space and pose in the space.
        /// The anchor is represented by an XrSpace and its pose can be tracked via xrLocateSpace.
        /// Once the anchor is no longer needed, call xrDestroySpace to erase the anchor.
        /// </summary>
        /// <param name="createInfo">Information required to create the spatial anchor.</param>
        /// <param name="anchor">Output parameter to hold the created anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult CreateSpatialAnchor(XrSpatialAnchorCreateInfoHTC createInfo, out XrSpace anchor)
        {
            anchor = default;
            if (!IsInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            if (session == 0)
                return XrResult.XR_ERROR_SESSION_LOST;

            var ret = XrCreateSpatialAnchorHTC(session, ref createInfo, ref anchor);
            //Debug.Log("ViveAnchor CreateSpatialAnchor() r=" + ret + ", a=" + anchor + ", bs=" + createInfo.space +
            //    ", pos=(" + createInfo.poseInSpace.position.x + "," + createInfo.poseInSpace.position.y + "," + createInfo.poseInSpace.position.z +
            //    "), rot=(" + createInfo.poseInSpace.orientation.x + "," + createInfo.poseInSpace.orientation.y + "," + createInfo.poseInSpace.orientation.z + "," + createInfo.poseInSpace.orientation.w +
            //    "), n=" + createInfo.name.name);
            return ret;
        }

        /// <summary>
        /// The GetSpatialAnchorName function retrieves the name of the spatial anchor.
        /// </summary>
        /// <param name="anchor">The XrSpace representing the anchor.</param>
        /// <param name="name">Output parameter to hold the name of the anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult GetSpatialAnchorName(XrSpace anchor, out XrSpatialAnchorNameHTC name)
        {
            name = new XrSpatialAnchorNameHTC();
            if (!IsInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            return XrGetSpatialAnchorNameHTC(anchor, ref name);
        }

        /// <summary>
        /// If the extension is supported and enabled, return true.
        /// </summary>
        /// <returns>True if persisted anchor extension is supported, false otherwise.</returns>
        public bool IsPersistedAnchorSupported()
        {
            return IsPAInited;
        }

        /// <summary>
        /// Creates a persisted anchor collection.  This collection can be used to persist spatial anchors across sessions.
        /// Many persisted anchor APIs need a persisted anchor collection to operate.
        /// </summary>
        /// <param name="future">Output the async future handle.  Check the future to get the PersitedAnchorCollection handle.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult AcquirePersistedAnchorCollectionAsync(out IntPtr future)
        {
            future = IntPtr.Zero;
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            if (session == 0)
                return XrResult.XR_ERROR_SESSION_LOST;

            XrPersistedAnchorCollectionAcquireInfoHTC acquireInfo = new XrPersistedAnchorCollectionAcquireInfoHTC
            {
                type = XrStructureType.XR_TYPE_PERSISTED_ANCHOR_COLLECTION_ACQUIRE_INFO_HTC,
                next = IntPtr.Zero,
            };

            return XrAcquirePersistedAnchorCollectionAsyncHTC(session, ref acquireInfo, out future);
        }

        public XrResult AcquirePersistedAnchorCollectionComplete(IntPtr future, out XrPersistedAnchorCollectionAcquireCompletionHTC completion)
        {
            completion = new XrPersistedAnchorCollectionAcquireCompletionHTC();
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrAcquirePersistedAnchorCollectionCompleteHTC(future, out completion);
        }

            

        /// <summary>
        /// Destroys the persisted anchor collection.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to be destroyed.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult ReleasePersistedAnchorCollection(IntPtr persistedAnchorCollection)
        {
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrReleasePersistedAnchorCollectionHTC(persistedAnchorCollection);
        }

        /// <summary>
        /// Persists a spatial anchor with the given name.  The name should be unique.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="anchor">The spatial anchor to be persisted.</param>
        /// <param name="name">The name of the persisted anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult PersistSpatialAnchorAsync(IntPtr persistedAnchorCollection, XrSpace anchor, XrSpatialAnchorNameHTC name, out IntPtr future)
        {
            future = IntPtr.Zero;
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            XrSpatialAnchorPersistInfoHTC persistInfo = new XrSpatialAnchorPersistInfoHTC
            {
                type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_PERSIST_INFO_HTC,
                anchor = anchor,
                persistedAnchorName = name
            };
            return XrPersistSpatialAnchorAsyncHTC(persistedAnchorCollection, ref persistInfo, out future);
        }

        public XrResult PersistSpatialAnchorComplete(IntPtr future, out FutureWrapper.XrFutureCompletionEXT completion)
        {
            completion = new FutureWrapper.XrFutureCompletionEXT() {
                type = XrStructureType.XR_TYPE_FUTURE_COMPLETION_EXT,
                next = IntPtr.Zero,
                futureResult = XrResult.XR_SUCCESS
            };
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrPersistSpatialAnchorCompleteHTC(future, out completion);
        }

        /// <summary>
        /// Unpersists the anchor with the given name.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="name">The name of the anchor to be unpersisted.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult UnpersistSpatialAnchor(IntPtr persistedAnchorCollection, XrSpatialAnchorNameHTC name)
        {
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrUnpersistSpatialAnchorHTC(persistedAnchorCollection, ref name);
        }

        /// <summary>
        /// Enumerates all persisted anchor names.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="persistedAnchorNameCapacityInput">The capacity of the input buffer.</param>
        /// <param name="persistedAnchorNameCountOutput">Output parameter to hold the count of persisted anchor names.</param>
        /// <param name="persistedAnchorNames">Output parameter to hold the names of persisted anchors.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult EnumeratePersistedAnchorNames(IntPtr persistedAnchorCollection, uint persistedAnchorNameCapacityInput,
            ref uint persistedAnchorNameCountOutput, ref XrSpatialAnchorNameHTC[] persistedAnchorNames)
        {
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrEnumeratePersistedAnchorNamesHTC(persistedAnchorCollection, persistedAnchorNameCapacityInput, ref persistedAnchorNameCountOutput, persistedAnchorNames);
        }

        /// <summary>
        /// Creates a spatial anchor from a persisted anchor.
        /// </summary>
        /// <param name="spatialAnchorCreateInfo">Information required to create the spatial anchor from persisted anchor.</param>
        /// <param name="anchor">Output parameter to hold the created spatial anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult CreateSpatialAnchorFromPersistedAnchorAsync(XrSpatialAnchorFromPersistedAnchorCreateInfoHTC spatialAnchorCreateInfo, out IntPtr future)
        {
            future = IntPtr.Zero;
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            if (session == 0)
                return XrResult.XR_ERROR_SESSION_LOST;
            return XrCreateSpatialAnchorFromPersistedAnchorAsyncHTC(session, ref spatialAnchorCreateInfo, out future);
        }

        /// <summary>
        /// When the future is ready, call this function to get the result.
        /// </summary>
        /// <param name="future"></param>
        /// <param name="completion"></param>
        /// <returns></returns>
        public XrResult CreateSpatialAnchorFromPersistedAnchorComplete(IntPtr future, out XrSpatialAnchorFromPersistedAnchorCreateCompletionHTC completion)
        {
            completion = new XrSpatialAnchorFromPersistedAnchorCreateCompletionHTC()
            {
                type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_FROM_PERSISTED_ANCHOR_CREATE_COMPLETION_HTC,
                next = IntPtr.Zero,
                futureResult = XrResult.XR_SUCCESS,
                anchor = 0
            };

            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            return XrCreateSpatialAnchorFromPersistedAnchorCompleteHTC(future, out completion);
        }

        /// <summary>
        /// Clears all persisted anchors.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult ClearPersistedAnchors(IntPtr persistedAnchorCollection)
        {
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            return XrClearPersistedAnchorsHTC(persistedAnchorCollection);
        }

        /// <summary>
        /// Gets the properties of the persisted anchor.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="getInfo">Output parameter to hold the properties of the persisted anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult GetPersistedAnchorProperties(IntPtr persistedAnchorCollection, out XrPersistedAnchorPropertiesGetInfoHTC getInfo)
        {
            getInfo = new XrPersistedAnchorPropertiesGetInfoHTC
            {
                type = XrStructureType.XR_TYPE_PERSISTED_ANCHOR_PROPERTIES_GET_INFO_HTC
            };
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            return XrGetPersistedAnchorPropertiesHTC(persistedAnchorCollection, ref getInfo);
        }

        /// <summary>
        /// Exports the persisted anchor to a buffer. The buffer can be used to import the anchor later or save to a file.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="persistedAnchorName">The name of the persisted anchor to be exported.</param>
        /// <param name="data">Output parameter to hold the buffer containing the exported anchor.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult ExportPersistedAnchor(IntPtr persistedAnchorCollection, XrSpatialAnchorNameHTC persistedAnchorName, out byte[] data)
        {
            data = null;
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;
            uint dataCountOutput = 0;
            uint dataCapacityInput = 0;
            XrResult ret = XrExportPersistedAnchorHTC(persistedAnchorCollection, ref persistedAnchorName, dataCapacityInput, ref dataCountOutput, null);
            if (ret != XrResult.XR_SUCCESS)
            {
                Debug.LogError("ExportPersistedAnchor failed to get data size. ret=" + ret);
                data = null;
                return ret;
            }

            dataCapacityInput = dataCountOutput;
            data = new byte[dataCountOutput];
            ret = XrExportPersistedAnchorHTC(persistedAnchorCollection, ref persistedAnchorName, dataCapacityInput, ref dataCountOutput, data);
            return ret;
        }

        /// <summary>
        /// Imports the persisted anchor from a buffer. The buffer should be created by ExportPersistedAnchor.
        /// </summary>
        /// <param name="persistedAnchorCollection">The persisted anchor collection to operate.</param>
        /// <param name="data">The buffer containing the persisted anchor data.</param>
        /// <returns>XrResult indicating success or failure.</returns>
        public XrResult ImportPersistedAnchor(IntPtr persistedAnchorCollection, byte[] data)
        {
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            return XrImportPersistedAnchorHTC(persistedAnchorCollection, (uint)data.Length, data);
        }

        /// <summary>
        /// Gets the name of the persisted anchor from a buffer. The buffer should be created by ExportPersistedAnchor.
        /// </summary>
        /// <param name="persistedAnchorCollection"></param>
        /// <param name="buffer"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public XrResult GetPersistedAnchorNameFromBuffer(IntPtr persistedAnchorCollection, byte[] buffer, out XrSpatialAnchorNameHTC name)
        {
            name = new XrSpatialAnchorNameHTC();
            if (!IsPAInited)
                return XrResult.XR_ERROR_EXTENSION_NOT_PRESENT;

            if (buffer == null)
                return XrResult.XR_ERROR_VALIDATION_FAILURE;

            return XrGetPersistedAnchorNameFromBufferHTC(persistedAnchorCollection, (uint)buffer.Length, buffer, ref name);
        }

        #endregion

        #region tools for user

        /// <summary>
        /// According to XRInputSubsystem's tracking origin mode, return the corresponding XrSpace.
        /// </summary>
        /// <returns></returns>
        public XrSpace GetTrackingSpace()
        {
            var s = GetCurrentAppSpace();
            //Debug.Log("ViveAnchor GetTrackingSpace() s=" + s);
            return s;
        }
        #endregion
    }
}
