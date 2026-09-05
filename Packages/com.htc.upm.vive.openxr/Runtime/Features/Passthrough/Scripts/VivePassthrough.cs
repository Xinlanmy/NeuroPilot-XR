// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR;
using VIVE.OpenXR.CompositionLayer;
using UnityEngine.Profiling;
using VIVE.OpenXR.Feature;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.Passthrough
{
	public class PassthroughLayer : IDisposable
	{
		private XrPassthroughHTC xrHandle = 0;
		private XrCompositionLayerPassthroughHTC layer;
		private IntPtr layerPtr = IntPtr.Zero;
		private XrPassthroughMeshTransformInfoHTC meshTransform;
		private bool needMesh = false;
		private IntPtr meshPtr = IntPtr.Zero;
		private CompositionLayer.LayerType layerType = CompositionLayer.LayerType.Underlay;
		private bool disposedValue = false;
		private IntPtr verticesPtr = IntPtr.Zero;
		private IntPtr indicesPtr = IntPtr.Zero;
		private int depth = 0;

		public bool NeedMesh { get => needMesh; set => needMesh = value; }
		public CompositionLayer.LayerType LayerType { get => layerType; set => layerType = value; }
		public int Depth { get => depth; set => depth = value; }

		public PassthroughLayer(XrPassthroughHTC xrHandle, CompositionLayer.LayerType layerType)
		{
			this.xrHandle = xrHandle;
			this.layerType = layerType;

			layer = new XrCompositionLayerPassthroughHTC();
			layerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrCompositionLayerPassthroughHTC)));

			meshTransform = new XrPassthroughMeshTransformInfoHTC();
			meshPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrPassthroughMeshTransformInfoHTC)));
		}

		public PassthroughLayer()
		{
			layer = new XrCompositionLayerPassthroughHTC();
			layerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrCompositionLayerPassthroughHTC)));

			meshTransform = new XrPassthroughMeshTransformInfoHTC();
			meshPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrPassthroughMeshTransformInfoHTC)));
		}

		public void SetLayer(XrCompositionLayerPassthroughHTC layer)
		{
			this.layer = layer;
		}

		public static XrCompositionLayerPassthroughHTC MakeEmptyLayer()
		{
			XrPassthroughColorHTC passthroughColor = new XrPassthroughColorHTC(in_alpha: 0);
			XrCompositionLayerPassthroughHTC compositionLayerPassthrough = new XrCompositionLayerPassthroughHTC(
					in_layerFlags: (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT,
					in_space: 0,
					in_passthrough: 0,
					in_color: passthroughColor);

			return compositionLayerPassthrough;
		}

		public XrCompositionLayerPassthroughHTC GetLayer()
		{
			return layer;
		}

		/// <summary>
		/// Never null unless disposed.
		/// </summary>
		/// <returns></returns>
		public IntPtr GetLayerPtr()
		{
			return layerPtr;
		}

		/// <summary>
		/// Before SetMeshTransform, you should call SetLayer first.
		/// </summary>
		/// <param name="meshTransform"></param>
		/// <param name="needMesh"></param>
		public void SetMeshTransform(XrPassthroughMeshTransformInfoHTC meshTransform, bool needMesh = true)
		{
			this.meshTransform = meshTransform;
			NeedMesh = needMesh;
		}

		public static XrPassthroughMeshTransformInfoHTC MakeMeshTransform()
		{
			return new XrPassthroughMeshTransformInfoHTC()
			{
				type = XrStructureType.XR_TYPE_PASSTHROUGH_MESH_TRANSFORM_INFO_HTC,
				next = IntPtr.Zero,
				vertexCount = 0,
				vertices = IntPtr.Zero,
				indexCount = 0,
				indices = IntPtr.Zero,
				baseSpace = 0,
				time = 0,
				pose = XrPosef.Identity,
				scale = XrVector3f.One,
			};
		}

		public bool SetMeshData(ref XrPassthroughMeshTransformInfoHTC mesh, Vector3[] vertices, int[] indices, bool convertFromUnityToOpenXR = true)
		{
			if (vertices.Length < 3 || indices.Length % 3 != 0) //Must have at least 3 vertices and complete triangles
			{
				Debug.LogError("PassthroughLayer: Mesh data invalid.");
				return false;
			}

			// check our vertex buffer and index buffer
			if (verticesPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(verticesPtr);
				verticesPtr = IntPtr.Zero;
			}

			if (indicesPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(indicesPtr);
				indicesPtr = IntPtr.Zero;
			}

			verticesPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrVector3f)) * vertices.Length);
			indicesPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)) * indices.Length);

			XrVector3f[] xrVertices = new XrVector3f[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				xrVertices[i] = OpenXRHelper.ToOpenXRVector(vertices[i], convertFromUnityToOpenXR);
			}

			uint[] indicesUint = new uint[indices.Length];
			// Unity is contrary to OpenXR in the order of the vertices in the triangle.
			for (int i = 0; i < indices.Length; i += 3)
			{
				indicesUint[i] = (uint)indices[i];
				indicesUint[i + 1] = (uint)indices[i + 2];
				indicesUint[i + 2] = (uint)indices[i + 1];
			}

			MemoryTools.CopyToRawMemory(verticesPtr, xrVertices);
			MemoryTools.CopyToRawMemory(indicesPtr, indicesUint);

			mesh.vertexCount = (uint)vertices.Length;
			mesh.vertices = verticesPtr;

			mesh.indexCount = (uint)indices.Length;
			mesh.indices = indicesPtr;

			return true;
		}

		/// <summary>
		/// Copy to native buffer
		/// </summary>
		public void ToNativeBuffer()
		{
			if (layerPtr == IntPtr.Zero)
				return;

			if (NeedMesh)
			{
				layer.next = meshPtr;
				MemoryTools.StructureToPtr(meshTransform, meshPtr);
				MemoryTools.StructureToPtr(layer, layerPtr);
			}
			else
			{
				if (layer.next != IntPtr.Zero)
					layer.next = IntPtr.Zero;
				MemoryTools.StructureToPtr(layer, layerPtr);
			}
		}

		public XrPassthroughMeshTransformInfoHTC GetMesh()
		{
			return meshTransform;
		}

		public IntPtr GetMeshTransformPtr()
		{
			return meshPtr;
		}

		public bool IsOverlay()
		{
			return layerType == CompositionLayer.LayerType.Overlay;
		}

		public bool IsUnderlay()
		{
			return layerType == CompositionLayer.LayerType.Underlay;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					xrHandle = 0;
				}

				if (layerPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(layerPtr);
					layerPtr = IntPtr.Zero;
				}

				if (meshPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(meshPtr);
					meshPtr = IntPtr.Zero;
				}

				if (verticesPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(verticesPtr);
					verticesPtr = IntPtr.Zero;
				}

				if (indicesPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(indicesPtr);
					indicesPtr = IntPtr.Zero;
				}

				disposedValue = true;
			}
		}

		~PassthroughLayer()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		internal void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Passthrough",
		Desc = "Enable this feature to use the VIVE OpenXR Passthrough feature.",
		Company = "HTC",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenxrExtensionStrings,
		Version = "1.0.0",
		BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
		FeatureId = featureId
	)]
#endif
	public class VivePassthrough : OpenXRFeature
	{
		#region LOG
		const string TAG = "VivePassthrough";
		StringBuilder sb = new StringBuilder();

		StringBuilder CSB {
			get {
				return sb.Clear();
			}
		}

		#endregion

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.passthrough";

		/// <summary>
		/// The extension string.
		/// </summary>
		public const string kOpenxrExtensionStrings = "XR_HTC_passthrough XR_HTC_passthrough_configuration";

		private static IntPtr[] layersOrigin = null;
		private static IntPtr[] layersModified = null;
		private static int sizeOfIntPtr = Marshal.SizeOf(typeof(IntPtr));
		private static IntPtr layersModifiedPtr = Marshal.AllocHGlobal(sizeOfIntPtr * 30); //Preallocate a layer buffer with sufficient size and reuse it for each frame.

		protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		{
			var interceptors = ViveInterceptors.Instance;
			interceptors.AddRequiredFunction("xrWaitFrame");
			interceptors.AddRequiredFunction("xrEndFrame");

			return interceptors.HookGetInstanceProcAddr(func);
		}

		struct XrCompositionLayerProjection
		{
			public XrStructureType type;
			public IntPtr next;
			public XrCompositionLayerFlags layerFlags;
			public XrSpace space;
			public uint viewCount;
			public IntPtr views;
		}


		private void ForceProjectionLayerTransparent(IntPtr[] layersPtr)
		{
			// Find the projection layer in freameEndInfo
			int projIndex = -1;
			IntPtr projLayerPtr = IntPtr.Zero;
			for (int i = 0; i < layersPtr.Length; i++)
			{
				projLayerPtr = layersPtr[i];
				if (MemoryTools.GetType(projLayerPtr) == XrStructureType.XR_TYPE_COMPOSITION_LAYER_PROJECTION)
				{
					projIndex = i;
					break;
				}
			}

			// No projection layer found.
			if (projIndex == -1)
				Debug.Log("No projection layer");

			// Force projection layer accept transparent
			XrCompositionLayerProjection xrProjLayer = default;
			MemoryTools.PtrToStructure(projLayerPtr, ref xrProjLayer);
			xrProjLayer.layerFlags |= ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT;
			MemoryTools.StructureToPtr(xrProjLayer, projLayerPtr);
		}

		private bool OnBeforeEndFrame(XrSession session, ref ViveInterceptors.XrFrameEndInfo frameEndInfo, ref XrResult result)
		{
			// It is possible that the layerCount is 0, so we need to check it.
			if (frameEndInfo.layerCount == 0 || ptLayers == null || ptLayers.Count == 0)
				return true;

			uint layerCount = frameEndInfo.layerCount;

			Profiler.BeginSample("Pt");

			// Make all layers to array
			if (layersOrigin == null || layersOrigin.Length != layerCount)
				layersOrigin = new IntPtr[layerCount];
			MemoryTools.CopyAllFromRawMemory(layersOrigin, frameEndInfo.layers);

			// Insert our layers into layersOrigin
			// Total passthough layers count
			var finalCount = ptLayers.Count + layerCount;
			if (layersModified == null || layersModified.Length != finalCount)
				layersModified = new IntPtr[finalCount];

			int j = 0;
			bool hasUnderlay = false;
			// Insert underlay
			for (int i = 0; i < ptLayers.Count; i++)
			{
				if (ptLayers[i].IsUnderlay())
				{
					ptLayers[i].ToNativeBuffer();
					var ptr = ptLayers[i].GetLayerPtr();
					if (ptr != IntPtr.Zero)
						layersModified[j++] = ptr;
					hasUnderlay = true;
				}
			}
			if (hasUnderlay)
				ForceProjectionLayerTransparent(layersOrigin);

			// put original layers into layersModified
			for (int i = 0; i < layersOrigin.Length; i++)
			{
				layersModified[j++] = layersOrigin[i];
			}

			// Append overlay
			for (int i = 0; i < ptLayers.Count; i++)
			{
				if (ptLayers[i].IsOverlay())
				{
					ptLayers[i].ToNativeBuffer();
					var ptr = ptLayers[i].GetLayerPtr();
					if (ptr != IntPtr.Zero)
						layersModified[j++] = ptr;
				}
			}

			MemoryTools.CopyToRawMemory(layersModifiedPtr, layersModified);

			// Change original layers
			frameEndInfo.layers = layersModifiedPtr;
			frameEndInfo.layerCount = (uint)layersModified.Length;

			Profiler.EndSample();

			return true;
		}

		List<PassthroughLayer> ptLayers = null;

		/// <summary>
		/// Call this function in game thread
		/// </summary>
		/// <param name="layers">passthrough layers</param>
		public void SubmitLayers(List<PassthroughLayer> layers)
		{
			ptLayers = layers;
		}

		public XrFrameState GetFrameState()
		{
			var frameState = ViveInterceptors.Instance.GetCurrentFrameState();
			return new XrFrameState() {
					predictedDisplayTime = frameState.predictedDisplayTime,
					predictedDisplayPeriod = frameState.predictedDisplayPeriod,
					shouldRender = frameState.shouldRender
				};
		}

		#region OpenXR Life Cycle
		private bool m_XrInstanceCreated = false;
		/// <summary>
		/// The XR instance is created or not.
		/// </summary>
		public bool XrInstanceCreated
		{
			get { return m_XrInstanceCreated; }
		}
		private XrInstance m_XrInstance = 0;
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			Log.D(TAG, "OnInstanceCreate() " + m_XrInstance);
			foreach (string kOpenxrExtensionString in kOpenxrExtensionStrings.Split(' '))
			{
				if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
				{
					Log.W(TAG, "OnInstanceCreate() " + kOpenxrExtensionString + " is NOT enabled.");
				}
				else
				{
					Log.I(TAG, "OnInstanceCreate() " + kOpenxrExtensionString + " is enabled.");
				}
			}

			SpaceWrapper.Instance.OnInstanceCreate(xrInstance, xrGetInstanceProcAddr);
			CommonWrapper.Instance.OnInstanceCreate(xrInstance, xrGetInstanceProcAddr);
			bool ret = GetXrFunctionDelegates(xrInstance);
			if (!ret)
				return ret;

			m_XrInstanceCreated = true;
			m_XrInstance = xrInstance;
			ViveInterceptors.Instance.BeforeOriginalEndFrame += OnBeforeEndFrame;

			return ret;
		}
		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			if (m_XrInstanceCreated)
			{
				ViveInterceptors.Instance.BeforeOriginalEndFrame -= OnBeforeEndFrame;

				CommonWrapper.Instance.OnInstanceDestroy();
				SpaceWrapper.Instance.OnInstanceDestroy();
			}

			m_XrInstanceCreated = false;
			Log.D(TAG, "OnInstanceDestroy() " + m_XrInstance);
		}

		private XrSystemId m_XrSystemId = 0;
		protected override void OnSystemChange(ulong xrSystem)
		{
			m_XrSystemId = xrSystem;
			Log.D(TAG, "OnSystemChange() " + m_XrSystemId);
		}

		private bool m_XrSessionCreated = false;
		/// <summary>
		/// The XR session is created or not.
		/// </summary>
		public bool XrSessionCreated
		{
			get { return m_XrSessionCreated; }
		}
		private XrSession m_XrSession = 0;
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			Log.I(TAG, "OnSessionCreate() " + m_XrSession);
			CheckConfigurationSupport();
		}

		private XrSpace m_WorldLockSpaceOriginOnHead = 0, m_WorldLockSpaceOriginOnFloor = 0, m_HeadLockSpace = 0;
		private XrSpace WorldLockSpaceOriginOnHead
		{
			get { return m_WorldLockSpaceOriginOnHead; }
		}
		private XrSpace WorldLockSpaceOriginOnFloor
		{
			get { return m_WorldLockSpaceOriginOnFloor; }
		}
		private XrSpace HeadLockSpace
		{
			get { return m_HeadLockSpace; }
		}

		private bool m_XrSessionEnding = false;
		/// <summary>
		/// The XR session is ending or not.
		/// </summary>
		public bool XrSessionEnding
		{
			get { return m_XrSessionEnding; }
		}
		protected override void OnSessionBegin(ulong xrSession)
		{
			m_XrSessionEnding = false;
			Log.D(TAG, "OnSessionBegin() " + m_XrSession);

			// Enumerate supported reference space types and create the XrSpace.
			XrReferenceSpaceType[] spaces = null;// new XrReferenceSpaceType[Enum.GetNames(typeof(XrReferenceSpaceType)).Count()];
			int spaceCountOutput = 0;
			if (SpaceWrapper.Instance.EnumerateReferenceSpaces(
				xrSession,
				spaceCapacityInput: 0,
				spaceCountOutput: ref spaceCountOutput,
				spaces: ref spaces) == XrResult.XR_SUCCESS)
			{
				//Log.I(TAG, "spaceCountOutput: " + spaceCountOutput);

				Array.Resize(ref spaces, (int)spaceCountOutput);
				if (SpaceWrapper.Instance.EnumerateReferenceSpaces(
                    xrSession,
					spaceCapacityInput: spaceCountOutput,
					spaceCountOutput: ref spaceCountOutput,
					spaces: ref spaces) == XrResult.XR_SUCCESS)
				{
					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoWorldLock;
						referenceSpaceCreateInfoWorldLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoWorldLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoWorldLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL;
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (SpaceWrapper.Instance.CreateReferenceSpace(
                            xrSession,
							createInfo: referenceSpaceCreateInfoWorldLock,
							space: out m_WorldLockSpaceOriginOnHead) == XrResult.XR_SUCCESS)
						{
							//Log.I(TAG, "CreateReferenceSpace: " + m_WorldLockSpaceOriginOnHead);
						}
						else
						{
							Log.E(TAG, "CreateReferenceSpace for world lock layers on head failed.");
						}
					}
					else
					{
						Log.E(TAG, "CreateReferenceSpace no space type for world lock on head layers.");
					}

					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoWorldLock;
						referenceSpaceCreateInfoWorldLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoWorldLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoWorldLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE;
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (SpaceWrapper.Instance.CreateReferenceSpace(
							xrSession,
							createInfo: referenceSpaceCreateInfoWorldLock,
							space: out m_WorldLockSpaceOriginOnFloor) == XrResult.XR_SUCCESS)
						{
							//Log.I(TAG, "CreateReferenceSpace: " + m_WorldLockSpaceOriginOnFloor);
						}
						else
						{
							Log.E(TAG, "CreateReferenceSpace for world lock layers on floor failed.");
						}
					}
					else
					{
						Log.E(TAG, "CreateReferenceSpace no space type for world lock on floor layers.");
					}

					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_VIEW))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoHeadLock;
						referenceSpaceCreateInfoHeadLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoHeadLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoHeadLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_VIEW;
						referenceSpaceCreateInfoHeadLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoHeadLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (SpaceWrapper.Instance.CreateReferenceSpace(
							xrSession,
							createInfo: referenceSpaceCreateInfoHeadLock,
							space: out m_HeadLockSpace) == XrResult.XR_SUCCESS)
						{
							//Log.I(TAG, "CreateReferenceSpace: " + m_HeadLockSpace);
						}
						else
						{
							Log.E(TAG, "CreateReferenceSpace for head lock layers failed.");
						}
					}
					else
					{
						Log.E(TAG, "CreateReferenceSpace no space type for head lock layers.");
					}
				}
				else
				{
					Log.E(TAG, "EnumerateReferenceSpaces(" + spaceCountOutput + ") failed.");
				}
			}
			else
			{
				Log.E(TAG, "EnumerateReferenceSpaces(0) failed.");
			}
		}
		protected override void OnSessionEnd(ulong xrSession)
		{
			m_XrSessionEnding = true;
			Log.D(TAG, "OnSessionEnd() " + m_XrSession);
		}

		/// <summary>
		/// The delegate of Passthrough Session Destroy.
		/// </summary>
		public delegate void OnPassthroughSessionDestroyDelegate(XrPassthroughHTC passthroughID);
		private Dictionary<XrPassthroughHTC, OnPassthroughSessionDestroyDelegate> OnPassthroughSessionDestroyHandlerDictionary = new Dictionary<XrPassthroughHTC, OnPassthroughSessionDestroyDelegate>();
		protected override void OnSessionDestroy(ulong xrSession)
		{
			if (!m_XrSessionCreated || m_XrSession != xrSession) { return; }

			Log.D(TAG, CSB.Append("OnSessionDestroy() " + xrSession));
			m_XrSessionCreated = false;
			m_XrSession = 0;

			List<XrPassthroughHTC> passthroughs = PassthroughList;
			for (int i = 0; i < passthroughs.Count; i++)
			{
				XrPassthroughHTC currentpassthrough = passthroughs[i];
				DestroyPassthroughHTC(currentpassthrough);
				if (OnPassthroughSessionDestroyHandlerDictionary.ContainsKey(currentpassthrough) && OnPassthroughSessionDestroyHandlerDictionary[currentpassthrough] != null)
				{
					Log.D(TAG, CSB.Append("OnSessionDestroy() Call back ").Append(passthroughs[i]));
					OnPassthroughSessionDestroyHandlerDictionary[passthroughs[i]].Invoke(passthroughs[i]);
				}
			}

			if (m_HeadLockSpace != 0)
			{
                SpaceWrapper.Instance.DestroySpace(m_HeadLockSpace);
				m_HeadLockSpace = 0;
			}
			if (m_WorldLockSpaceOriginOnFloor != 0)
			{
                SpaceWrapper.Instance.DestroySpace(m_WorldLockSpaceOriginOnFloor);
				m_WorldLockSpaceOriginOnFloor = 0;
			}
			if (m_WorldLockSpaceOriginOnHead != 0)
			{
                SpaceWrapper.Instance.DestroySpace(m_WorldLockSpaceOriginOnHead);
				m_WorldLockSpaceOriginOnHead = 0;
			}
		}

		/// <summary>
		/// The current XR session state.
		/// </summary>
		public XrSessionState XrSessionCurrentState
		{
			get { return m_XrSessionNewState; }
		}
		private XrSessionState m_XrSessionNewState = XrSessionState.XR_SESSION_STATE_UNKNOWN;
		private XrSessionState m_XrSessionOldState = XrSessionState.XR_SESSION_STATE_UNKNOWN;
		protected override void OnSessionStateChange(int oldState, int newState)
		{
			Log.D(TAG, "OnSessionStateChange() oldState: " + oldState + " newState:" + newState);

			if (Enum.IsDefined(typeof(XrSessionState), oldState))
			{
				m_XrSessionOldState = (XrSessionState)oldState;
			}
			else
			{
				Log.I(TAG, "OnSessionStateChange() oldState undefined");
			}

			if (Enum.IsDefined(typeof(XrSessionState), newState))
			{
				m_XrSessionNewState = (XrSessionState)newState;
			}
			else
			{
				Log.I(TAG, "OnSessionStateChange() newState undefined");
			}

		}
		#endregion

		#region OpenXR function delegates
		/// xrGetInstanceProcAddr
		OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;
		VivePassthroughHelper.xrCreatePassthroughHTCDelegate xrCreatePassthroughHTC;
		VivePassthroughHelper.xrDestroyPassthroughHTCDelegate xrDestroyPassthroughHTC;
		VivePassthroughHelper.xrEnumeratePassthroughImageRatesHTCDelegate xrEnumeratePassthroughImageRatesHTC;
		VivePassthroughHelper.xrGetPassthroughConfigurationHTCDelegate xrGetPassthroughConfigurationHTC;
		VivePassthroughHelper.xrSetPassthroughConfigurationHTCDelegate xrSetPassthroughConfigurationHTC;

		private bool GetXrFunctionDelegates(XrInstance xrInstance)
		{
			/// xrGetInstanceProcAddr
			if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
			{
				Log.I(TAG, "Get function pointer of xrGetInstanceProcAddr.");
				XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
					xrGetInstanceProcAddr,
					typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
			}
			else
			{
				Log.E(TAG, "xrGetInstanceProcAddr");
				return false;
			}

            bool ret = true;
            IntPtr funcPtr = IntPtr.Zero;

            ret &= OpenXRHelper.GetXrFunctionDelegate(XrGetInstanceProcAddr, xrInstance, "xrCreatePassthroughHTC", out xrCreatePassthroughHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(XrGetInstanceProcAddr, xrInstance, "xrDestroyPassthroughHTC", out xrDestroyPassthroughHTC);

#if UNITY_ANDROID
            ret &= OpenXRHelper.GetXrFunctionDelegate(XrGetInstanceProcAddr, xrInstance, "xrEnumeratePassthroughImageRatesHTC", out xrEnumeratePassthroughImageRatesHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(XrGetInstanceProcAddr, xrInstance, "xrGetPassthroughConfigurationHTC", out xrGetPassthroughConfigurationHTC);
            ret &= OpenXRHelper.GetXrFunctionDelegate(XrGetInstanceProcAddr, xrInstance, "xrSetPassthroughConfigurationHTC", out xrSetPassthroughConfigurationHTC);
#endif
			return ret;
		}
#endregion

		private List<XrPassthroughHTC> passthroughList = new List<XrPassthroughHTC>();
		public List<XrPassthroughHTC> PassthroughList {
			get {
				if (passthroughList == null) { passthroughList = new List<XrPassthroughHTC>(); }
				return passthroughList;
			}
		}

#region Public API
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, uint compositionDepth, OnPassthroughSessionDestroyDelegate onDestroy)
		{
			passthrough = 0;

			if (!m_XrSessionCreated)
			{
				Log.E(TAG, "CreatePassthroughHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			if (!m_XrInstanceCreated)
			{
				Log.E(TAG, "CreatePassthroughHTC() XR_ERROR_INSTANCE_LOST.");
				return XrResult.XR_ERROR_INSTANCE_LOST;
			}

			Log.I(TAG, CSB.Append("CreatePassthroughHTC() layerType: ").Append(layerType).Append(", compositionDepth: ").Append(compositionDepth));

			XrResult result = XrResult.XR_ERROR_RUNTIME_FAILURE;

			result = xrCreatePassthroughHTC(m_XrSession, createInfo, out passthrough);
			Log.I(TAG, "CreatePassthroughHTC() xrCreatePassthroughHTC result: " + result);
			if (result == XrResult.XR_SUCCESS)
			{
				passthroughList.Add(passthrough);
				if (onDestroy != null) { OnPassthroughSessionDestroyHandlerDictionary.Add(passthrough, onDestroy); }
			}

			return result;
		}
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, OnPassthroughSessionDestroyDelegate onDestroy = null)
		{
			return CreatePassthroughHTC(createInfo, out passthrough, layerType, 0, onDestroy);
		}
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType = CompositionLayer.LayerType.Overlay)
		{
			return CreatePassthroughHTC(createInfo, out passthrough, layerType, 0, null);
		}

		public XrResult DestroyPassthroughHTC(XrPassthroughHTC passthrough)
		{
			if (!passthroughList.Contains(passthrough))
			{
				Log.E(TAG, "DestroyPassthroughHTC() Invalid passthrough: " + passthrough);
				return XrResult.XR_ERROR_VALIDATION_FAILURE;
			}

			Log.I(TAG, CSB.Append("DestroyPassthroughHTC() passthrough: ").Append(passthrough));

			XrResult result = XrResult.XR_ERROR_RUNTIME_FAILURE;

			int passthroughID = (int)(passthrough & 0x00007FFF);
			result = xrDestroyPassthroughHTC(passthrough);
			Log.I(TAG, CSB.Append("DestroyPassthroughHTC() ").Append(passthrough).Append(", result: ").Append(result));
			if (result == XrResult.XR_SUCCESS)
			{
				passthroughList.Remove(passthrough);
				if (OnPassthroughSessionDestroyHandlerDictionary.ContainsKey(passthrough))
					OnPassthroughSessionDestroyHandlerDictionary.Remove(passthrough);
			}

			return result;
		}

		/// <summary>
		/// According to XRInputSubsystem's tracking origin mode, return the corresponding XrSpace.
		/// </summary>
		/// <returns><see cref="XrSpace"/> for tracking origin.</returns>
		public XrSpace GetTrackingSpace()
		{
			XrSpace space = GetCurrentAppSpace();
			Log.I(TAG, CSB.Append("GetTrackingSpace() ").Append(space));
			return space;
		}

		private List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
		/// <summary>
		/// Helper function to get XrSpace from space type.
		/// </summary>
		public XrSpace GetXrSpaceFromSpaceType(ProjectedPassthroughSpaceType spaceType)
		{
			XrSpace meshSpace = 0;
			switch (spaceType)
			{
				case ProjectedPassthroughSpaceType.Headlock:
					meshSpace = HeadLockSpace;
					break;
				case ProjectedPassthroughSpaceType.Worldlock:
				default:
					XRInputSubsystem subsystem = null;
					SubsystemManager.GetSubsystems(inputSubsystems);
					if (inputSubsystems.Count > 0)
					{
						subsystem = inputSubsystems[0];
					}

					if (subsystem != null)
					{
						TrackingOriginModeFlags trackingOriginMode = subsystem.GetTrackingOriginMode();

						switch (trackingOriginMode)
						{
							default:
							case TrackingOriginModeFlags.Floor:
								meshSpace = WorldLockSpaceOriginOnFloor;
								break;
							case TrackingOriginModeFlags.Device:
								meshSpace = WorldLockSpaceOriginOnHead;
								break;
						}
					}
					else
					{
						meshSpace = WorldLockSpaceOriginOnFloor;
					}
					break;
			}

			return meshSpace;
		}

		public XrResult EnumeratePassthroughImageRatesHTC([In] UInt32 imageRateCapacityInput, ref UInt32 imageRateCountOutput,[In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates)
		{
			if (!m_XrSessionCreated)
			{
				Log.E(TAG, "EnumeratePassthroughImageRatesHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrEnumeratePassthroughImageRatesHTC(m_XrSession, imageRateCapacityInput, ref imageRateCountOutput, imageRates);
		}

		public XrResult GetPassthroughConfigurationHTC(IntPtr config)
		{
			if (!m_XrSessionCreated)
			{
				Log.E(TAG, "GetPassthroughConfigurationHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrGetPassthroughConfigurationHTC(m_XrSession, config);
		}

		public XrResult SetPassthroughConfigurationHTC(IntPtr config)
		{
			if (!m_XrSessionCreated)
			{
				Log.E(TAG, "SetPassthroughConfigurationHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrSetPassthroughConfigurationHTC(m_XrSession, config);
		}

		private XrBool32 m_SupportsImageRate;
		private XrBool32 m_SupportsImageQuality;
		XrSystemProperties systemProperties;
		XrSystemPassthroughConfigurationPropertiesHTC passthroughConfigurationProperties;
		private void CheckConfigurationSupport()
		{
			m_SupportsImageRate = false;
			m_SupportsImageQuality = false;
			if (!m_XrSessionCreated)
			{
				Log.E(TAG, "CheckUserPresenceSupport() session is not created.");
				return;
			}

			passthroughConfigurationProperties.type = XrStructureType.XR_TYPE_SYSTEM_PASSTHROUGH_CONFIGURATION_PROPERTIES_HTC;
			passthroughConfigurationProperties.next = IntPtr.Zero;
			systemProperties.type = XrStructureType.XR_TYPE_SYSTEM_PROPERTIES;
			systemProperties.next = Marshal.AllocHGlobal(Marshal.SizeOf(passthroughConfigurationProperties));

			long offset = 0;
			if (IntPtr.Size == 4)
				offset = systemProperties.next.ToInt32();
			else
				offset = systemProperties.next.ToInt64();

			IntPtr passthroughConfigurationPtr = new IntPtr(offset);
			Marshal.StructureToPtr(passthroughConfigurationProperties, passthroughConfigurationPtr, false);

			if (CommonWrapper.Instance.GetSystemProperties(m_XrInstance, m_XrSystemId, ref systemProperties) == XrResult.XR_SUCCESS)
			{
				if (IntPtr.Size == 4)
					offset = systemProperties.next.ToInt32();
				else
					offset = systemProperties.next.ToInt64();

				passthroughConfigurationPtr = new IntPtr(offset);
				passthroughConfigurationProperties = (XrSystemPassthroughConfigurationPropertiesHTC)Marshal.PtrToStructure(passthroughConfigurationPtr, typeof(XrSystemPassthroughConfigurationPropertiesHTC));

				Log.I(TAG, CSB.Append("CheckConfigurationSupport() supportsImageQuality: ").Append((UInt32)passthroughConfigurationProperties.supportsImageQuality));
				Log.I(TAG, CSB.Append("CheckConfigurationSupport() supportsImageRate: ").Append((UInt32)passthroughConfigurationProperties.supportsImageRate));
				m_SupportsImageQuality = passthroughConfigurationProperties.supportsImageQuality;
				m_SupportsImageRate = passthroughConfigurationProperties.supportsImageRate;
			}
			else
			{
				Log.E(TAG, "CheckSupport() GetSystemProperties failed.");
			}

			Marshal.FreeHGlobal(systemProperties.next);
		}
		public bool SupportsImageRate() { return m_SupportsImageRate; }
		public bool SupportsImageQuality() { return m_SupportsImageQuality; }

#endregion
	}
}
