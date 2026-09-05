// Copyright HTC Corporation All Rights Reserved.
using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Feature;
using static VIVE.OpenXR.Feature.ViveAnchor;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace VIVE.OpenXR.Toolkits.Anchor
{
    public static class AnchorManager
    {
        static ViveAnchor feature = null;
        static bool isSupported = false;
        static bool isPersistedAnchorSupported = false;

        static void EnsureFeature()
        {
            if (feature != null) return;

            feature = OpenXRSettings.Instance.GetFeature<ViveAnchor>();
            if (feature == null)
                throw new NotSupportedException("ViveAnchor feature is not enabled");
        }

        static void EnsureCollection()
        {
            if (taskAcquirePAC != null)
            {
                Debug.Log("AnchorManager: Wait for AcquirePersistedAnchorCollection task.");
                taskAcquirePAC.Wait();
            }
            if (persistedAnchorCollection == IntPtr.Zero)
                throw new Exception("Should create Persisted Anchor Collection first.");
        }

        /// <summary>
        /// Helper to get the extension feature instance.
        /// </summary>
        /// <returns>Instance of ViveAnchor feature.</returns>
        public static ViveAnchor GetFeature()
        {
            if (feature != null) return feature;
            feature = OpenXRSettings.Instance.GetFeature<ViveAnchor>();
            return feature;
        }

        /// <summary>
        /// Check if the extensions are supported. Should always check this before using the other functions.
        /// </summary>
        /// <returns>True if the extension is supported, false otherwise.</returns>
        public static bool IsSupported()
        {
            if (GetFeature() == null) return false;
            if (isSupported) return true;

            var ret = false;
            if (feature.GetProperties(out XrSystemAnchorPropertiesHTC properties) == XrResult.XR_SUCCESS)
            {
                Debug.Log("ViveAnchor: IsSupported() properties.supportedFeatures: " + properties.supportsAnchor);
                ret = properties.supportsAnchor > 0;
                isSupported = ret;
            }
            else
            {
                Debug.Log("ViveAnchor: IsSupported() GetSystemProperties failed.");
            }

            return ret;
        }

        /// <summary>
        /// Check if the persisted anchor extension is supported and enabled.
        /// Should always check this before using the other persistance function.
        /// </summary>
        /// <returns>True if persisted anchor extension is supported, false otherwise.</returns>
        public static bool IsPersistedAnchorSupported()
        {
            if (GetFeature() == null) return false;
            if (isPersistedAnchorSupported) return true;
            else
                isPersistedAnchorSupported = feature.IsPersistedAnchorSupported();
            return isPersistedAnchorSupported;
        }

        /// <summary>
        /// Create a spatial anchor at tracking space (Camera Rig).
        /// </summary>
        /// <param name="pose">The related pose to the tracking space (Camera Rig)</param>
        /// <returns>Anchor container</returns>
        public static Anchor CreateAnchor(Pose pose, string name)
        {
            EnsureFeature();

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("The name should not be empty.");

            XrSpace baseSpace = feature.GetTrackingSpace();
            XrSpatialAnchorCreateInfoHTC createInfo = new XrSpatialAnchorCreateInfoHTC();
            createInfo.type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_HTC;
            createInfo.poseInSpace = new XrPosef();
            createInfo.poseInSpace.position = pose.position.ToOpenXRVector();
            createInfo.poseInSpace.orientation = pose.rotation.ToOpenXRQuaternion();
            createInfo.name = new XrSpatialAnchorNameHTC(name);
            createInfo.space = baseSpace;

            if (feature.CreateSpatialAnchor(createInfo, out XrSpace anchor) == XrResult.XR_SUCCESS)
            {
                return new Anchor(anchor, name);
            }
            return null;
        }

        /// <summary>
        /// Get the name of the spatial anchor.
        /// </summary>
        /// <param name="anchor">The anchor instance.</param>
        /// <param name="name">Output parameter to hold the name of the anchor.</param>
        /// <returns>True if the name is successfully retrieved, false otherwise.</returns>
        public static bool GetSpatialAnchorName(Anchor anchor, out string name)
        {
            return GetSpatialAnchorName(anchor.GetXrSpace(), out name);
        }

        /// <summary>
        /// Get the name of the spatial anchor.
        /// </summary>
        /// <param name="anchor">The XrSpace representing the anchor.</param>
        /// <param name="name">Output parameter to hold the name of the anchor.</param>
        /// <returns>True if the name is successfully retrieved, false otherwise.</returns>
        public static bool GetSpatialAnchorName(XrSpace anchor, out string name)
        {
            name = "";
            EnsureFeature();
            XrResult ret = feature.GetSpatialAnchorName(anchor, out XrSpatialAnchorNameHTC xrName);
            if (ret == XrResult.XR_SUCCESS)
                name = xrName.ToString();
            return ret == XrResult.XR_SUCCESS;
        }

        /// <summary>
        /// Get the XrSpace stand for current tracking space.
        /// </summary>
        /// <returns></returns>
        public static XrSpace GetTrackingSpace()
        {
            EnsureFeature();
            return feature.GetTrackingSpace();
        }

        /// <summary>
        /// Get the pose related to current tracking space.  Only when position and orientation are both valid, the pose is valid.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="pose"></param>
        /// <returns>true if both position and rotation are valid.</returns>
        public static bool GetTrackingSpacePose(Anchor anchor, out Pose pose)
        {
            var sw = SpaceWrapper.Instance;
            return anchor.GetRelatedPose(feature.GetTrackingSpace(), ViveInterceptors.Instance.GetPredictTime(), out pose);
        }

        // Use SemaphoreSlim to make sure only one anchor's task is running at the same time.
        static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        // Use lock to make sure taskAcquirePAC and persistedAnchorCollection assignment is atomic.
        static readonly object asyncLock = new object();
        static FutureTask<(XrResult, IntPtr)> taskAcquirePAC = null;
        static IntPtr persistedAnchorCollection = System.IntPtr.Zero;

        private static (XrResult, IntPtr) CompletePAC(IntPtr future)
        {
            Debug.Log("AnchorManager: AcquirePersistedAnchorCollectionComplete");
            var ret = feature.AcquirePersistedAnchorCollectionComplete(future, out var completion);
            lock (asyncLock)
            {
                taskAcquirePAC = null;
                if (ret == XrResult.XR_SUCCESS)
                {
                    ret = completion.futureResult;
                    Debug.Log("AnchorManager: AcquirePersistedAnchorCollection: Complete");
                    persistedAnchorCollection = completion.persistedAnchorCollection;
                    return (ret, persistedAnchorCollection);
                }
                else
                {
                    //Debug.LogError("AcquirePersistedAnchorCollection: Complete: PersistedAnchorCollection=" + completion.persistedAnchorCollection);
                    persistedAnchorCollection = System.IntPtr.Zero;
                    return (ret, persistedAnchorCollection);
                }
            }
        }

        /// <summary>
        /// Enable the persistance anchor feature.  It will acquire a persisted anchor collection.
        /// The first time PAC's acquiration may take time.  You can to cancel the process by calling <see cref="ReleasePersistedAnchorCollection"/>.
        /// You can wait for the returned task to complete, or by calling <see cref="IsPersistedAnchorCollectionAcquired"/> to check if the collection is ready.
        /// Use <see cref="ReleasePersistedAnchorCollection"/> to free resource when no any persisted anchor operations are needed.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static FutureTask<(XrResult, IntPtr)> AcquirePersistedAnchorCollection()
        {
            EnsureFeature();
            if (!feature.IsPersistedAnchorSupported())
                return FutureTask<(XrResult, IntPtr)>.FromResult((XrResult.XR_ERROR_EXTENSION_NOT_PRESENT, IntPtr.Zero));
            lock (asyncLock)
            {
                if (persistedAnchorCollection != System.IntPtr.Zero)
                    return FutureTask<(XrResult, IntPtr)>.FromResult((XrResult.XR_SUCCESS, persistedAnchorCollection));

                // If the persistedAnchorCollection is not ready, and the task is started, wait for it.
                if (taskAcquirePAC != null)
                    return taskAcquirePAC;
            }

            Debug.Log("ViveAnchor: AcquirePersistedAnchorCollectionAsync");
            var ret = feature.AcquirePersistedAnchorCollectionAsync(out IntPtr future);
            if (ret != XrResult.XR_SUCCESS)
            {
                Debug.LogError("AcquirePersistedAnchorCollection failed: " + ret);
                return FutureTask<(XrResult, IntPtr)>.FromResult((ret, IntPtr.Zero));
            }
            else
            {
                var task = new FutureTask<(XrResult, IntPtr)>(future, CompletePAC, 10, autoComplete: true);
                lock (asyncLock)
                {
                    taskAcquirePAC = task;
                }
                return task;
            }
        }

        /// <summary>
        /// Check if the persisted anchor collection is acquired.
        /// </summary>
        /// <returns>True if the persisted anchor collection is acquired, false otherwise.</returns>
        public static bool IsPersistedAnchorCollectionAcquired()
        {
            return persistedAnchorCollection != System.IntPtr.Zero;
        }

        /// <summary>
        /// Call this function when no any persisted anchor operations are needed. 
        /// Destroy the persisted anchor collection.  If task is running, the task will be canceled.
        /// </summary>
        public static void ReleasePersistedAnchorCollection()
        {
            IntPtr tmp;
            if (taskAcquirePAC != null)
            {
                taskAcquirePAC.Cancel();
                taskAcquirePAC.Dispose();
                taskAcquirePAC = null;
            }

            lock (asyncLock)
            {
                if (persistedAnchorCollection == System.IntPtr.Zero) return;
                tmp = persistedAnchorCollection;
                persistedAnchorCollection = System.IntPtr.Zero;
            }

            EnsureFeature();

            Task.Run(async () =>
            {
                Debug.Log("ViveAnchor: ReleasePersistedAnchorCollection task is started.");
                await semaphoreSlim.WaitAsync();
                try
                {
                    feature?.ReleasePersistedAnchorCollection(tmp);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
                Debug.Log("ViveAnchor: ReleasePersistedAnchorCollection task is done.");
            });
        }

        private static XrResult CompletePA(IntPtr future) {
            Debug.Log("AnchorManager: CompletePA");
            var ret = feature.PersistSpatialAnchorComplete(future, out var completion);
            if (ret == XrResult.XR_SUCCESS)
            {
                return completion.futureResult;
            }
            else
            {
                Debug.LogError("AcquirePersistedAnchorCollection failed: " + ret);
            }
            return ret;
        }

        /// <summary>
        /// Persist an anchor with the given name.  The persistanceAnchorName should be unique.
        /// The persistance might fail if the anchor is not trackable.  Check the result from the task.
        /// </summary>
        /// <param name="anchor">The anchor instance.</param>
        /// <param name="persistanceAnchorName">The name of the persisted anchor.</param>
        /// <param name="cts">PersistAnchor may take time. If you want to cancel it, use cts.</param>
        /// <returns>The task to get persisted anchor's result.</returns>
        public static FutureTask<XrResult> PersistAnchor(Anchor anchor, string persistanceAnchorName)
        {
            EnsureFeature();
            EnsureCollection();

            if (string.IsNullOrEmpty(persistanceAnchorName))
                throw new ArgumentException("The persistanceAnchorName should not be empty.");

            var name = new XrSpatialAnchorNameHTC(persistanceAnchorName);

            var ret = feature.PersistSpatialAnchorAsync(persistedAnchorCollection, anchor.GetXrSpace(), name, out IntPtr future);
            if (ret == XrResult.XR_SUCCESS)
            {
                // If no auto complete, you can cancel the task and no need to free resouce.
                // Once it completed, you need handle the result.
                return new FutureTask<XrResult>(future, CompletePA, 10, autoComplete: false);
            }

            return FutureTask<XrResult>.FromResult(ret);
        }

        /// <summary>
        /// Unpersist the anchor by the name.  The anchor created from persisted anchor will still be trackable.
        /// </summary>
        /// <param name="persistanceAnchorName">The name of the persisted anchor to be removed.</param>
        /// <returns>The result of the operation.</returns>
        public static XrResult UnpersistAnchor(string persistanceAnchorName)
        {
            EnsureFeature();
            EnsureCollection();

            if (string.IsNullOrEmpty(persistanceAnchorName))
                throw new ArgumentException("The persistanceAnchorName should not be empty.");

            var name = new XrSpatialAnchorNameHTC(persistanceAnchorName);

            var ret = feature.UnpersistSpatialAnchor(persistedAnchorCollection, name);

            return ret;
        }

        /// <summary>
        /// Get the number of persisted anchors.
        /// </summary>
        /// <param name="count">Output parameter to hold the number of persisted anchors.</param>
        /// <returns>The result of the operation.</returns>
        public static XrResult GetNumberOfPersistedAnchors(out int count)
        {
            EnsureFeature();
            EnsureCollection();

            XrSpatialAnchorNameHTC[] xrNames = null;
            uint xrCount = 0;

            XrResult ret = feature.EnumeratePersistedAnchorNames(persistedAnchorCollection, 0, ref xrCount, ref xrNames);
            if (ret != XrResult.XR_SUCCESS)
                count = 0;
            else
                count = (int)xrCount;
            return ret;
        }

        /// <summary>
        /// List all persisted anchors.
        /// </summary>
        /// <param name="names">Output parameter to hold the names of the persisted anchors.</param>
        /// <returns>The result of the operation.</returns>
        public static XrResult EnumeratePersistedAnchorNames(out string[] names)
        {
            EnsureFeature();
            EnsureCollection();

            XrSpatialAnchorNameHTC[] xrNames = null;
            uint countOut = 0;
            uint countIn = 0;

            XrResult ret = feature.EnumeratePersistedAnchorNames(persistedAnchorCollection, countIn, ref countOut, ref xrNames);
            if (ret != XrResult.XR_SUCCESS)
            {
                names = null;
                return ret;
            }

            // If Insufficient size, try again.
            do
            {
                countIn = countOut;
                xrNames = new XrSpatialAnchorNameHTC[countIn];
                ret = feature.EnumeratePersistedAnchorNames(persistedAnchorCollection, countIn, ref countOut, ref xrNames);
            }
            while (ret == XrResult.XR_ERROR_SIZE_INSUFFICIENT);
            if (ret != XrResult.XR_SUCCESS)
            {
                names = null;
                return ret;
            }

            names = new string[countIn];
            for (int i = 0; i < countIn; i++)
            {
                string v = xrNames[i].ToString();
                names[i] = v;
            }
            return ret;
        }

        private static (XrResult, Anchor) CompleteCreateSAfromPA(IntPtr future)
        {
            Debug.Log("AnchorManager: CompleteCreateSAfromPA");
            var ret = feature.CreateSpatialAnchorFromPersistedAnchorComplete(future, out var completion);
            if (ret == XrResult.XR_SUCCESS)
            {
                var anchor = new Anchor(completion.anchor);
                anchor.isTrackable = true;
                return (completion.futureResult, anchor);
            }
            else
            {
                Debug.LogError("CreateSpatialAnchorFromPersistedAnchor failed: " + ret);
                return (ret, new Anchor(0));
            }
        }

        /// <summary>
        /// Create a spatial anchor from a persisted anchor. This will also mark the anchor as trackable.
        /// </summary>
        /// <param name="persistanceAnchorName">The name of the persisted anchor.</param>
        /// <param name="spatialAnchorName">The name of the new spatial anchor.</param>
        /// <param name="anchor">Output parameter to hold the new anchor instance.</param>
        /// <returns>The result of the operation.</returns>
        public static FutureTask<(XrResult, Anchor)> CreateSpatialAnchorFromPersistedAnchor(string persistanceAnchorName, string spatialAnchorName)
        {
            EnsureFeature();
            EnsureCollection();
            Debug.Log("AnchorManager: CreateSpatialAnchorFromPersistedAnchor: " + persistanceAnchorName + " -> " + spatialAnchorName);

            if (string.IsNullOrEmpty(persistanceAnchorName) || string.IsNullOrEmpty(spatialAnchorName))
                throw new ArgumentException("The persistanceAnchorName and spatialAnchorName should not be empty.");

            var createInfo = new XrSpatialAnchorFromPersistedAnchorCreateInfoHTC() {
                type = XrStructureType.XR_TYPE_SPATIAL_ANCHOR_FROM_PERSISTED_ANCHOR_CREATE_INFO_HTC,
                persistedAnchorCollection = persistedAnchorCollection,
                persistedAnchorName = new XrSpatialAnchorNameHTC(persistanceAnchorName),
                spatialAnchorName = new XrSpatialAnchorNameHTC(spatialAnchorName)
            };

            var ret = feature.CreateSpatialAnchorFromPersistedAnchorAsync(createInfo, out var future);
            if (ret == XrResult.XR_SUCCESS)
            {
                // If no auto complete, you can cancel the task and no need to free resouce.
                // Once it completed, you need handle the result.
                return new FutureTask<(XrResult, Anchor)>(future, CompleteCreateSAfromPA, 10, autoComplete: false);
            }
            else
            {
                return FutureTask<(XrResult, Anchor)>.FromResult((ret, new Anchor(0)));
            }
        }

        /// <summary>
        /// Clear all persisted anchors. Those anchors created from or to the persisted anchor will still be trackable.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public static XrResult ClearPersistedAnchors()
        {
            EnsureFeature();
            EnsureCollection();
            return feature.ClearPersistedAnchors(persistedAnchorCollection);
        }

        /// <summary>
        /// Get the properties of the persisted anchor.
        /// maxPersistedAnchorCount in XrPersistedAnchorPropertiesGetInfoHTC will be set to the max count of the persisted anchor.
        /// </summary>
        /// <param name="properties">Output parameter to hold the properties of the persisted anchor.</param>
        /// <returns>The result of the operation.</returns>
        public static XrResult GetPersistedAnchorProperties(out XrPersistedAnchorPropertiesGetInfoHTC properties)
        {
            EnsureFeature();
            EnsureCollection();
            return feature.GetPersistedAnchorProperties(persistedAnchorCollection, out properties);
        }

        /// <summary>
        /// Export the persisted anchor to a buffer. The buffer can be used to import the anchor later or save it to a file.
        /// Export takes time, so it is an async function. The buffer will be null if the export failed.
        /// </summary>
        /// <param name="persistanceAnchorName">The name of the persisted anchor to be exported.</param>
        /// <param name="buffer">Output parameter to hold the buffer containing the exported anchor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task<(XrResult, string, byte[])> ExportPersistedAnchor(string persistanceAnchorName)
        {
            EnsureFeature();
            EnsureCollection();

            if (string.IsNullOrEmpty(persistanceAnchorName))
                return Task.FromResult<(XrResult, string, byte[])>((XrResult.XR_ERROR_HANDLE_INVALID, "", null));

            var name = new XrSpatialAnchorNameHTC(persistanceAnchorName);

            return Task.Run(async () =>
            {
                Debug.Log($"ExportPersistedAnchor({persistanceAnchorName}) task is started.");
                XrResult ret = XrResult.XR_ERROR_VALIDATION_FAILURE;
                await semaphoreSlim.WaitAsync();
                try
                {
                    lock (asyncLock)
                    {
                        if (persistedAnchorCollection == System.IntPtr.Zero)
                        {
                            return (XrResult.XR_ERROR_HANDLE_INVALID, "", null);
                        }
                    }

                    ret = feature.ExportPersistedAnchor(persistedAnchorCollection, name, out var buffer);
                    Debug.Log($"ExportPersistedAnchor({persistanceAnchorName}) task is done. ret=" + ret);
                    lock (asyncLock)
                    {
                        if (ret != XrResult.XR_SUCCESS)
                        {
                            buffer = null;
                            return (ret, "", null);
                        }
                        return (ret, persistanceAnchorName, buffer);
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });
        }

        /// <summary>
        /// Import the persisted anchor from a buffer. The buffer should be created by ExportPersistedAnchor.
        /// Import takes time, so it is an async function. Check imported anchor by EnumeratePersistedAnchorNames.
        /// </summary>
        /// <param name="buffer">The buffer containing the persisted anchor data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task<XrResult> ImportPersistedAnchor(byte[] buffer) {
            EnsureFeature();
            EnsureCollection();

            return Task.Run(async () =>
            {
                Debug.Log($"ImportPersistedAnchor task is started.");
                XrResult ret = XrResult.XR_ERROR_VALIDATION_FAILURE;
                await semaphoreSlim.WaitAsync();
                try
                {
                    lock (asyncLock)
                    {
                        if (persistedAnchorCollection == System.IntPtr.Zero)
                            return XrResult.XR_ERROR_HANDLE_INVALID;
                        ret = feature.ImportPersistedAnchor(persistedAnchorCollection, buffer);
                        return ret;
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                    Debug.Log($"ImportPersistedAnchor task is done. ret=" + ret);
                }
            });
        }

        /// <summary>
        /// Get the persisted anchor name from the buffer. The buffer should be created by ExportPersistedAnchor.
        /// </summary>
        /// <returns>True if the name is successfully retrieved, false otherwise.</returns>
        public static bool GetPersistedAnchorNameFromBuffer(byte[] buffer, out string name)
        {
            EnsureFeature();
            EnsureCollection();
            var ret = feature.GetPersistedAnchorNameFromBuffer(persistedAnchorCollection, buffer, out var xrName);
            if (ret == XrResult.XR_SUCCESS)
                name = xrName.ToString();
            else
                name = "";
            return ret == XrResult.XR_SUCCESS;
        }

        /// <summary>
        /// Anchor is a named Space. It can be used to create a spatial anchor, or get the anchor's name.
        /// After use, you should call Dispose() to release the anchor.
        /// IsTrackable is true if the anchor is created persisted anchor or created from persisted anchor.
        /// IsPersisted is true if the anchor is ever persisted.
        /// </summary>
        public class Anchor : VIVE.OpenXR.Feature.Space
        {
            /// <summary>
            /// The anchor's name
            /// </summary>
            string name;

            /// <summary>
            /// The anchor's name
            /// </summary>
            public string Name
            {
                get
                {
                    if (string.IsNullOrEmpty(name))
                        name = GetSpatialAnchorName();
                    return name;
                }
            }

            internal bool isTrackable = false;

            /// <summary>
            /// If the anchor is created persisted anchor or created from persisted anchor, it will be trackable.
            /// </summary>
            public bool IsTrackable => isTrackable;

            internal bool isPersisted = false;

            /// <summary>
            /// If the anchor is ever persisted, it will be true.
            /// </summary>
            public bool IsPersisted => isPersisted;

            internal Anchor(XrSpace anchor, string name) : base(anchor)
            {
                Debug.Log($"Anchor: new Anchor({anchor}, {name})");  // Remove this line later.
                // Get the current tracking space.
                this.name = name;
            }

            internal Anchor(XrSpace anchor) : base(anchor)
            {
                Debug.Log($"Anchor: new Anchor({anchor})");  // Remove this line later.
                // Get the current tracking space.
                name = GetSpatialAnchorName();
            }

            internal Anchor(Anchor other) : base(other.space)
            {
                // Get the current tracking space.
                name = other.name;
                isTrackable = other.isTrackable;
                isPersisted = other.isPersisted;
            }

            /// <summary>
            /// Get the anchor's name by using this anchor's handle, instead of the anchor's Name.  This will update the anchor's Name.
            /// </summary>
            /// <returns>Anchor's name. Always return non null string.</returns>
            public string GetSpatialAnchorName()
            {
                if (space == 0)
                {
                    Debug.LogError("Anchor: GetSpatialAnchorName: The anchor is invalid.");
                    return "";
                }
                AnchorManager.EnsureFeature();
                if (AnchorManager.GetSpatialAnchorName(this, out string name))
                    return name;

                Debug.LogError("Anchor: GetSpatialAnchorName: Failed to get Anchor name.");
                return "";
            }
        }
    }
}
