// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using System.Collections.Generic;
using System.Linq;
using VIVE.OpenXR.CompositionLayer;
using System.Runtime.CompilerServices;

namespace VIVE.OpenXR.CompositionLayer
{
	public class CompositionLayer : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField]
		public bool isPreviewingCylinder = false;
		public bool isPreviewingEquirect = false;
		[SerializeField]
		public bool isPreviewingQuad = false;
		[SerializeField]
		public GameObject generatedPreview = null;

		public const string CylinderPreviewName = "CylinderPreview";
		public const string EquirectPreviewName = "EquirectPreview";
		public const string QuadPreviewName = "QuadPreview";
#endif
		public const string CylinderUnderlayMeshName = "Underlay Alpha Mesh (Cylinder)";
		public const string EquirectUnderlayMeshName = "Underlay Alpha Mesh (Equirect)";
		public const string QuadUnderlayMeshName = "Underlay Alpha Mesh (Quad)";
		public const string FallbackMeshName = "FallbackMeshGO";

		[SerializeField]
		public LayerType layerType = LayerType.Overlay;

		[SerializeField]
		public uint compositionDepth = 0;

		[SerializeField]
		public LayerShape layerShape = LayerShape.Quad;

		[SerializeField]
		public Visibility layerVisibility = Visibility.Both;

		[SerializeField]
		[Min(0.001f)]
		private float m_QuadWidth = 1f;
		public float quadWidth { get { return m_QuadWidth; } }
#if UNITY_EDITOR
		public float QuadWidth { get { return m_QuadWidth; } set { m_QuadWidth = value; } }
#endif

		[SerializeField]
		[Min(0.001f)]
		private float m_QuadHeight = 1f;
		public float quadHeight { get { return m_QuadHeight; } }
#if UNITY_EDITOR
		public float QuadHeight { get { return m_QuadHeight; } set { m_QuadHeight = value; } }
#endif

		[SerializeField]
		[Min(0.001f)]
		private float m_CylinderRadius = 1f;
		public float cylinderRadius { get { return m_CylinderRadius; } }
#if UNITY_EDITOR
		public float CylinderRadius { get { return m_CylinderRadius; } set { m_CylinderRadius = value; } }
#endif


		[SerializeField]
		[Min(0.001f)]
		private float m_CylinderHeight = 1f;
		public float cylinderHeight { get { return m_CylinderHeight; } }
#if UNITY_EDITOR
		public float CylinderHeight { get { return m_CylinderHeight; } set { m_CylinderHeight = value; } }
#endif

		[SerializeField]
		[Min(0.001f)]
		private float m_CylinderArcLength = 1f;
		public float cylinderArcLength { get { return m_CylinderArcLength; } }
#if UNITY_EDITOR
		public float CylinderArcLength { get { return m_CylinderArcLength; } set { m_CylinderArcLength = value; } }
#endif

		[SerializeField]
		[Min(0.001f)]
		private float m_EquirectRadius = 1f;
		public float equirectRadius { get { return m_EquirectRadius; } }
#if UNITY_EDITOR
		public float EquirectRadius { get { return m_EquirectRadius; } set { m_EquirectRadius = value; } }
#endif

		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_EquirectScaleX = 1.0f;
		public float equirectScaleX { get { return m_EquirectScaleX; } }
#if UNITY_EDITOR
		public float EquirectScaleX { get { return m_EquirectScaleX; } set { m_EquirectScaleX = value; } }
#endif

		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_EquirectScaleY = 1.0f;
		public float equirectScaleY { get { return m_EquirectScaleY; } }
#if UNITY_EDITOR
		public float EquirectScaleY { get { return m_EquirectScaleY; } set { m_EquirectScaleY = value; } }
#endif

		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_EquirectBiasX = 0.0f;
		public float equirectBiasX { get { return m_EquirectBiasX; } }
#if UNITY_EDITOR
		public float EquirectBiasX { get { return m_EquirectBiasX; } set { m_EquirectBiasX = value; } }
#endif

		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_EquirectBiasY = 0.0f;
		public float equirectBiasY { get { return m_EquirectBiasY; } }
#if UNITY_EDITOR
		public float EquirectBiasY { get { return m_EquirectBiasY; } set { m_EquirectBiasY = value; } }
#endif

		[SerializeField]
		[Range(0f, 360f)]
		private float m_EquirectCentralHorizontalAngle = 360f;
		public float equirectCentralHorizontalAngle { get { return m_EquirectCentralHorizontalAngle; } }
#if UNITY_EDITOR
		public float EquirectCentralHorizontalAngle { get { return m_EquirectCentralHorizontalAngle; } set { m_EquirectCentralHorizontalAngle = value; } }
#endif

		[SerializeField]
		[Range(-90f, 90f)]
		private float m_EquirectUpperVerticalAngle = 90f;
		public float equirectUpperVerticalAngle { get { return m_EquirectUpperVerticalAngle; } }
#if UNITY_EDITOR
		public float EquirectUpperVerticalAngle { get { return m_EquirectUpperVerticalAngle; } set { m_EquirectUpperVerticalAngle = value; } }
#endif

		[SerializeField]
		[Range(-90f, 90f)]
		private float m_EquirectLowerVerticalAngle = -90f;
		public float equirectLowerVerticalAngle { get { return m_EquirectLowerVerticalAngle; } }
#if UNITY_EDITOR
		public float EquirectLowerVerticalAngle { get { return m_EquirectLowerVerticalAngle; } set { m_EquirectLowerVerticalAngle = value; } }
#endif

		[SerializeField]
		[Range(0f, 360f)]
		private float m_CylinderAngleOfArc = 180f;
		public float cylinderAngleOfArc { get { return m_CylinderAngleOfArc; } }
#if UNITY_EDITOR
		public float CylinderAngleOfArc { get { return m_CylinderAngleOfArc; } set { m_CylinderAngleOfArc = value; } }
#endif

#if UNITY_EDITOR
		[SerializeField]
		public CylinderLayerParamLockMode lockMode = CylinderLayerParamLockMode.ArcLength;
#endif

		[SerializeField]
		public bool isDynamicLayer = false;

		[SerializeField]
		public bool isExternalSurface = false;

		[SerializeField]
		public bool isCustomRects = false;

		[SerializeField]
		public CustomRectsType customRects = CustomRectsType.TopDown;

		[Tooltip("Width of external surface in pixels.")]
		[SerializeField]
		public uint externalSurfaceWidth = 1280;

		[Tooltip("Height of external surface in pixels.")]
		[SerializeField]
		public uint externalSurfaceHeight = 720;

		[SerializeField]
		public bool applyColorScaleBias = false;

		[SerializeField]
		public bool solidEffect = false;

		[SerializeField]
		public Color colorScale = Color.white;

		[SerializeField]
		public Color colorBias = Color.clear;

		[SerializeField]
		public bool isProtectedSurface = false;

		public Texture texture = null;
 
		private Texture m_TextureLeft => texture;
		public Texture textureLeft { get { return m_TextureLeft; } }

		public Texture textureRight = null;

		[SerializeField]
		private uint renderPriority = 0;
		public uint GetRenderPriority() { return renderPriority; }
		public void SetRenderPriority(uint newRenderPriority)
		{
			renderPriority = newRenderPriority;
			isRenderPriorityChanged = true;
			CompositionLayerManager.GetInstance().SubscribeToLayerManager(this);
		}
		public bool isRenderPriorityChanged
		{
			get; private set;
		}

		[SerializeField]
		public GameObject trackingOrigin = null;

		public GameObject generatedUnderlayMesh = null;
		private MeshRenderer generatedUnderlayMeshRenderer = null;
		private MeshFilter generatedUnderlayMeshFilter = null;

		public GameObject generatedFallbackMesh = null;
		private MeshRenderer generatedFallbackMeshRenderer = null;
		private MeshFilter generatedFallbackMeshFilter = null;

		private LayerTextures[] layerTextures = new LayerTextures[] {null, null};
		private Material texture2DBlitMaterial;

		private GameObject compositionLayerPlaceholderPrefabGO = null;

		public readonly float angleOfArcLowerLimit = 1f;
		public readonly float angleOfArcUpperLimit = 360f;

		private LayerShape previousLayerShape = LayerShape.Quad;
		private float previousQuadWidth = 1f;
		private float previousQuadHeight = 1f;
		private float previousEquirectRadius = 1f;
		private float previousCylinderHeight = 1f;
		private float previousCylinderArcLength = 1f;
		private float previousCylinderRadius = 1f;
		private float previousAngleOfArc = 180f;
		private Texture previousTextureLeft = null;
		private Texture previousTextureRight = null;
		private bool previousIsDynamicLayer = false;

		private int layerID; //For native
		private int layerIDRight; //For native
		private bool isHeadLock = false;
		private bool InitStatus = false;
		private bool isInitializationComplete = false;
		private bool reinitializationNeeded = false;
		private bool isOnBeforeRenderSubscribed = false;
		private bool isLayerReadyForSubmit = false;
		private bool isLinear = false;
		private bool isAutoFallbackActive = false;
		private bool placeholderGenerated = false;
		private static bool isSynchronized = false;
		private static RenderThreadSynchronizer synchronizer;
		public Camera hmd;

		private ViveCompositionLayer compositionLayerFeature = null;

		private const string LOG_TAG = "VIVE_CompositionLayer";
		static void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
		static void WARNING(string msg) { Debug.LogWarning(LOG_TAG + " " + msg); }
		static void ERROR(string msg) { Debug.LogError(LOG_TAG + " " + msg); }

		#region Composition Layer Lifecycle
		private bool CompositionLayerInit()
		{
			if (isInitializationComplete)
			{
				//DEBUG("CompositionLayerInit: Already initialized.");
				return true;
			}

			if (isExternalSurface)
			{
				CompositionLayerRenderThreadSyncObject SetupExternalAndroidSurfaceSyncObjects = new CompositionLayerRenderThreadSyncObject(
					(taskQueue) =>
					{
						lock (taskQueue)
						{
							CompositionLayerRenderThreadTask task = (CompositionLayerRenderThreadTask)taskQueue.Dequeue();

							//Enumerate Swapchain formats
							compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

							uint imageCount;

							GraphicsAPI graphicsAPI = GraphicsAPI.GLES3;

							switch (SystemInfo.graphicsDeviceType)
							{
								case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
									graphicsAPI = GraphicsAPI.GLES3;
									break;
								case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
									graphicsAPI = GraphicsAPI.Vulkan;
									break;
								default:
									ERROR("Unsupported Graphics API, aborting init process.");
									return;
							}

							layerID = compositionLayerFeature.CompositionLayer_Init(externalSurfaceWidth, externalSurfaceHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount, true);

							if (layerID != 0)
							{
								DEBUG("Init completed, ID: " + layerID);
								layerTextures[0] = new LayerTextures(imageCount);
								InitStatus = true;
							}

							if (textureRight != null && textureLeft != textureRight) {
							    layerIDRight = compositionLayerFeature.CompositionLayer_Init(externalSurfaceWidth, externalSurfaceHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount, true);
								if (layerIDRight != 0)
							    {
								    DEBUG("Init completed, ID right: " + layerIDRight);
								    layerTextures[1] = new LayerTextures(imageCount);
							    }
							}
							else if (isCustomRects) {
							    layerIDRight = compositionLayerFeature.CompositionLayer_Init(externalSurfaceWidth, externalSurfaceHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount, true);
								if (layerIDRight != 0)
							    {
								    DEBUG("Init completed, ID right: " + layerIDRight);
								    layerTextures[1] = new LayerTextures(imageCount);
							    }
							}

							taskQueue.Release(task);
						}
					});

				CompositionLayerRenderThreadTask.IssueObtainSwapchainEvent(SetupExternalAndroidSurfaceSyncObjects);

				texture = new Texture2D((int)externalSurfaceWidth, (int)externalSurfaceHeight, TextureFormat.RGBA32, false, isLinear);
				textureRight = new Texture2D((int)externalSurfaceWidth, (int)externalSurfaceHeight, TextureFormat.RGBA32, false, isLinear);


				DEBUG("CompositionLayerInit Ext Surf");

				return true;
			}

			if (textureLeft == null)
			{
				ERROR("CompositionLayerInit: Source Texture not found, abort init.");
				return false;
			}
			if (textureLeft != null && textureRight == null)
			{
				DEBUG("CompositionLayerInit: Using Left Texture as Right Texture.");
				textureRight = textureLeft;
			}

			DEBUG("CompositionLayerInit");

			uint textureWidth = (uint)textureLeft.width;
			uint textureHeight = (uint)textureLeft.height;

            DEBUG("Init : textureWidth = " + textureWidth + " textureHeight = " + textureHeight);

			CompositionLayerRenderThreadSyncObject ObtainLayerSwapchainSyncObject = new CompositionLayerRenderThreadSyncObject(
				(taskQueue) =>
				{
					lock (taskQueue)
					{
						CompositionLayerRenderThreadTask task = (CompositionLayerRenderThreadTask)taskQueue.Dequeue();

						//Enumerate Swapchain formats
						compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

						uint imageCount;

						GraphicsAPI graphicsAPI = GraphicsAPI.GLES3;

						switch(SystemInfo.graphicsDeviceType)
						{
							case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
								graphicsAPI = GraphicsAPI.GLES3;
								break;
							case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
								graphicsAPI = GraphicsAPI.Vulkan;
								break;
							default:
								ERROR("Unsupported Graphics API, aborting init process.");
								return;
						}

						layerID = compositionLayerFeature.CompositionLayer_Init(textureWidth, textureHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount);

						if (layerID != 0)
						{
							DEBUG("Init completed, ID: " + layerID + ", Image Count: " + imageCount);
							layerTextures[0] = new LayerTextures(imageCount);
							InitStatus = true;
						}
						if (textureRight != null && textureLeft != textureRight) {
						    layerIDRight = compositionLayerFeature.CompositionLayer_Init(textureWidth, textureHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount);

						    if (layerIDRight != 0)
						    {
							    DEBUG("Init completed, ID Right: " + layerIDRight + ", Image Count: " + imageCount);
							    layerTextures[1] = new LayerTextures(imageCount);
						    }
						}
						else if (isCustomRects)
						{
						    layerIDRight = compositionLayerFeature.CompositionLayer_Init(textureWidth, textureHeight, graphicsAPI, isDynamicLayer, isProtectedSurface, out imageCount);

						    if (layerIDRight != 0)
						    {
							    DEBUG("Init completed, ID Right: " + layerIDRight + ", Image Count: " + imageCount);
							    layerTextures[1] = new LayerTextures(imageCount);
						    }
						}

						taskQueue.Release(task);
					}
				});

			CompositionLayerRenderThreadTask.IssueObtainSwapchainEvent(ObtainLayerSwapchainSyncObject);

			previousLayerShape = layerShape;
			previousQuadWidth = m_QuadWidth;
			previousQuadHeight = m_QuadHeight;
			previousEquirectRadius = m_EquirectRadius;
			previousCylinderHeight = m_CylinderHeight;
			previousCylinderArcLength = m_CylinderArcLength;
			previousCylinderRadius = m_CylinderRadius;
			previousAngleOfArc = m_CylinderAngleOfArc;
			previousTextureLeft = textureLeft;
			previousTextureRight = textureRight;
			previousIsDynamicLayer = isDynamicLayer;

			return true;
		}

		private bool[] textureAcquired = new bool[] {false, false};
		private bool[] textureAcquiredOnce = new bool[] {false, false};
		XrOffset2Di offset = new XrOffset2Di();
		XrExtent2Di extent = new XrExtent2Di();
		XrRect2Di rect = new XrRect2Di();

		private bool SetLayerTexture(int eyeid)
		{
			if (!isInitializationComplete || !isSynchronized) return false;

			if (isExternalSurface)
			{
				//Set Texture Layout
				offset.x = 0;
				offset.y = (int)externalSurfaceHeight;
				extent.width = (int)externalSurfaceWidth;
				extent.height = (int)-externalSurfaceHeight;

				if (isCustomRects && customRects == CustomRectsType.TopDown)
			    {
			        extent.height = (int)-externalSurfaceHeight/2;
				    if (eyeid == 0)
				        offset.y = (int)(externalSurfaceHeight-externalSurfaceHeight/2);
			    }
			    else if (isCustomRects && customRects == CustomRectsType.LeftRight)
			    {
			        extent.width = (int)externalSurfaceWidth/2;
				    if (eyeid != 0)
				        offset.x = extent.width;
			    }

				rect.offset = offset;
				rect.extent = extent;

				layerTextures[eyeid].textureLayout = rect;

				return true; //No need to process texture queues
			}
			

			if (texture != null) //check for texture size change
			{
				if (TextureParamsChanged())
				{
					//Destroy queues
					DEBUG("SetLayerTexture: Texture params changed, need to re-init queues. layerID: " + ((eyeid ==0) ? layerID : layerIDRight));
					if (layerID != 0)
					{
						DestroyCompositionLayer(0);
						layerID = 0;
					}
					
					if (layerIDRight != 0)
					{
						DestroyCompositionLayer(1);
						layerIDRight = 0;
					}
					reinitializationNeeded = true;
					return false;
				}
			}
			else
			{
				ERROR("SetLayerTexture: No texture found. layerID: " + ((eyeid ==0) ? layerID : layerIDRight));
				return false;
			}

			if (isDynamicLayer || (!isDynamicLayer && !textureAcquiredOnce[eyeid]))
			{
				//Get available texture id
				compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();
				uint currentImageIndex;

				IntPtr newTextureID = compositionLayerFeature.CompositionLayer_GetTexture((eyeid ==0) ? layerID : layerIDRight, out currentImageIndex);

				textureAcquired[eyeid] = true;
				textureAcquiredOnce[eyeid] = true;

				if (newTextureID == IntPtr.Zero)
				{
					ERROR("SetLayerTexture: Invalid Texture ID");
					if (compositionLayerFeature.CompositionLayer_ReleaseTexture((eyeid ==0) ? layerID : layerIDRight))
					{
						textureAcquired[eyeid] = false;
					}
					return false;
				}

				bool textureIDUpdated = false;
				layerTextures[eyeid].currentAvailableTextureIndex = currentImageIndex;
				IntPtr currentTextureID = layerTextures[eyeid].GetCurrentAvailableTextureID();
				if (currentTextureID == IntPtr.Zero || currentTextureID != newTextureID)
				{
					DEBUG("SetLayerTexture: Update Texture ID. layerID: " + ((eyeid ==0) ? layerID : layerIDRight));
					layerTextures[eyeid].SetCurrentAvailableTextureID(newTextureID);
					textureIDUpdated = true;
				}

				if (layerTextures[eyeid].GetCurrentAvailableTextureID() == IntPtr.Zero)
				{
					ERROR("SetLayerTexture: Failed to get texture.");
					return false;
				}

				// Create external texture
				if (layerTextures[eyeid].GetCurrentAvailableExternalTexture() == null || textureIDUpdated)
				{
					DEBUG("SetLayerTexture: Create External Texture.");
					layerTextures[eyeid].SetCurrentAvailableExternalTexture(Texture2D.CreateExternalTexture(texture.width, texture.height, TextureFormat.RGBA32, false, isLinear, layerTextures[eyeid].GetCurrentAvailableTextureID()));
				}

				if (layerTextures[eyeid].externalTextures[layerTextures[eyeid].currentAvailableTextureIndex] == null)
				{
					ERROR("SetLayerTexture: Create External Texture Failed.");
					return false;
				}
			}

			//Set Texture Content

			bool isContentSet = layerTextures[eyeid].textureContentSet[layerTextures[eyeid].currentAvailableTextureIndex];

			if (!isDynamicLayer && isContentSet)
			{
				return true;
			}
			int currentTextureWidth = layerTextures[eyeid].GetCurrentAvailableExternalTexture().width;
			int currentTextureHeight = layerTextures[eyeid].GetCurrentAvailableExternalTexture().height;

			//Set Texture Layout
			offset.x = 0;
			offset.y = 0;
			extent.width = (int)currentTextureWidth;
			extent.height = (int)currentTextureHeight;


			if (isCustomRects && customRects == CustomRectsType.TopDown)
			{
			    extent.height = (int)currentTextureHeight/2;
				if (eyeid == 0)
				    offset.y = extent.height;
			}
			else if (isCustomRects && customRects == CustomRectsType.LeftRight)
			{
			    extent.width = (int)currentTextureWidth/2;
				if (eyeid != 0)
				    offset.x = extent.width;
			}

			rect.offset = offset;
			rect.extent = extent;
			layerTextures[eyeid].textureLayout = rect;

			RenderTexture srcTexture = ((eyeid == 0 || isCustomRects) ? textureLeft : textureRight) as RenderTexture;
			int msaaSamples = 1;
			if (srcTexture != null)
			{
				msaaSamples = srcTexture.antiAliasing;
			}

			Material currentBlitMat = texture2DBlitMaterial;

			DEBUG("RenderTextureDescriptor currentTextureWidth = " + currentTextureWidth + " currentTextureHeight = " + currentTextureHeight);

			RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor(currentTextureWidth, currentTextureHeight, RenderTextureFormat.ARGB32, 0);
			rtDescriptor.msaaSamples = msaaSamples;
			rtDescriptor.autoGenerateMips = false;
			rtDescriptor.useMipMap = false;
			rtDescriptor.sRGB = false;

			RenderTexture blitTempRT = RenderTexture.GetTemporary(rtDescriptor);
			if (!blitTempRT.IsCreated())
			{
				blitTempRT.Create();
			}
			blitTempRT.DiscardContents();

			Texture dstTexture = layerTextures[eyeid].GetCurrentAvailableExternalTexture();

		    Graphics.Blit((eyeid == 0) ? textureLeft : textureRight, blitTempRT, currentBlitMat);

			Graphics.CopyTexture(blitTempRT, 0, 0, dstTexture, 0, 0);

			//DEBUG("Blit and CopyTexture complete.");

			if (blitTempRT != null)
			{
				RenderTexture.ReleaseTemporary(blitTempRT);
			}
			else
			{
				ERROR("blitTempRT not found");
				return false;
			}

			layerTextures[eyeid].textureContentSet[layerTextures[eyeid].currentAvailableTextureIndex] = true;

			bool releaseTextureResult = compositionLayerFeature.CompositionLayer_ReleaseTexture((eyeid == 0) ? layerID : layerIDRight);
			if (releaseTextureResult)
			{
				textureAcquired[eyeid] = false;
			}

			return releaseTextureResult;
		}

		private void GetCompositionLayerPose(ref XrPosef pose) //Call at onBeforeRender
		{
			//Check if overlay is child of hmd transform i.e. head-lock
			Transform currentTransform = transform;
			isHeadLock = false;
			while (currentTransform != null)
			{
				if (currentTransform == hmd.transform) //Overlay is child of hmd transform
				{
					isHeadLock = true;
					break;
				}
				currentTransform = currentTransform.parent;
			}

			if (isHeadLock)
			{
				//Calculate Layer Rotation and Position relative to Camera
				Vector3 relativePosition = hmd.transform.InverseTransformPoint(transform.position);
				Quaternion relativeRotation = Quaternion.Inverse(hmd.transform.rotation).normalized * transform.rotation.normalized;

				UnityToOpenXRConversionHelper.GetOpenXRVector(relativePosition, ref pose.position);
				UnityToOpenXRConversionHelper.GetOpenXRQuaternion(relativeRotation.normalized, ref pose.orientation);
			}
			else
			{
				Vector3 trackingSpacePosition = transform.position;
				Quaternion trackingSpaceRotation = transform.rotation;

				if (trackingOrigin != null) //Apply origin correction to the layer pose
				{
					Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(trackingOrigin.transform.position, trackingOrigin.transform.rotation, Vector3.one);
					Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

					Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

					trackingSpacePosition = trackingSpaceLayerPoseTRS.GetColumn(3); //4th Column of TRS Matrix is the position
					trackingSpaceRotation = Quaternion.LookRotation(trackingSpaceLayerPoseTRS.GetColumn(2), trackingSpaceLayerPoseTRS.GetColumn(1));
				}
				UnityToOpenXRConversionHelper.GetOpenXRVector(trackingSpacePosition, ref pose.position);
				UnityToOpenXRConversionHelper.GetOpenXRQuaternion(trackingSpaceRotation.normalized, ref pose.orientation);
			}
		}

		bool enabledColorScaleBiasInShader = false;
		XrCompositionLayerColorScaleBiasKHR CompositionLayerParamsColorScaleBias = new XrCompositionLayerColorScaleBiasKHR();
		private void SubmitCompositionLayer(int eyeid, bool botheye) //Call at onBeforeRender
		{
			if (!isInitializationComplete && !isLayerReadyForSubmit) return;
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			if (isInitializationComplete && isSynchronized)
			{
				ViveCompositionLayerColorScaleBias compositionLayerColorScaleBias = OpenXRSettings.Instance.GetFeature<ViveCompositionLayerColorScaleBias>();
				if (applyColorScaleBias && compositionLayerColorScaleBias.ColorScaleBiasExtensionEnabled)
				{
					if (layerType == LayerType.Underlay)
					{
						//if (!enabledColorScaleBiasInShader)
						{
							if (generatedUnderlayMeshRenderer != null && generatedUnderlayMeshRenderer.material != null)
							{
								generatedUnderlayMeshRenderer.material.EnableKeyword("COLOR_SCALE_BIAS_ENABLED");
								enabledColorScaleBiasInShader = true;
							}
						}

						if (generatedUnderlayMeshRenderer != null && generatedUnderlayMeshRenderer.material != null)
						{
							generatedUnderlayMeshRenderer.material.SetColor("_ColorScale", colorScale);
							generatedUnderlayMeshRenderer.material.SetColor("_ColorBias", colorBias);
						}
					}
					else if (enabledColorScaleBiasInShader) //Disable if not underlay
					{
						generatedUnderlayMeshRenderer.material.DisableKeyword("COLOR_SCALE_BIAS_ENABLED");
						enabledColorScaleBiasInShader = false;
					}

					CompositionLayerParamsColorScaleBias.type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_COLOR_SCALE_BIAS_KHR;

					UnityToOpenXRConversionHelper.GetOpenXRColor4f(colorScale, ref CompositionLayerParamsColorScaleBias.colorScale);
					UnityToOpenXRConversionHelper.GetOpenXRColor4f(colorBias, ref CompositionLayerParamsColorScaleBias.colorBias);
					if (!solidEffect && enabledColorScaleBiasInShader)
					{
						CompositionLayerParamsColorScaleBias.colorScale.a = 1.0f;
						CompositionLayerParamsColorScaleBias.colorBias.a = 0.0f;
					}

					compositionLayerColorScaleBias.Submit_CompositionLayerColorBias(CompositionLayerParamsColorScaleBias, (eyeid == 0) ? layerID : layerIDRight);
				}
				else if (enabledColorScaleBiasInShader) //Disable if color scale bias is no longer active
				{
					generatedUnderlayMeshRenderer.material.DisableKeyword("COLOR_SCALE_BIAS_ENABLED");
					enabledColorScaleBiasInShader = false;
				}


				switch (layerShape)
				{
					default:
					case LayerShape.Quad:
						compositionLayerFeature.Submit_CompositionLayerQuad(AssignCompositionLayerParamsQuad(eyeid, botheye), (OpenXR.CompositionLayer.LayerType)layerType, compositionDepth, (eyeid == 0) ? layerID : layerIDRight);
						break;
					case LayerShape.Cylinder:
						ViveCompositionLayerCylinder compositionLayerCylinderFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayerCylinder>();
						if (compositionLayerCylinderFeature != null && compositionLayerCylinderFeature.CylinderExtensionEnabled)
						{
							compositionLayerCylinderFeature.Submit_CompositionLayerCylinder(AssignCompositionLayerParamsCylinder(eyeid, botheye), (OpenXR.CompositionLayer.LayerType)layerType, compositionDepth, (eyeid == 0) ? layerID : layerIDRight);
						}
						break;
					case LayerShape.Equirect:// TODO added code to submit
						ViveCompositionLayerEquirect compositionLayerEquicrectFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayerEquirect>();
						if (compositionLayerEquicrectFeature != null && compositionLayerEquicrectFeature.EquirectExtensionEnabled)
						{
							compositionLayerEquicrectFeature.Submit_CompositionLayerEquirect(AssignCompositionLayerParamsEquirect(eyeid), (OpenXR.CompositionLayer.LayerType)layerType, compositionDepth, (eyeid == 0) ? layerID : layerIDRight);
						}
						break;
					case LayerShape.Equirect2:// TODO added code to submit
						compositionLayerEquicrectFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayerEquirect>();
						if (compositionLayerEquicrectFeature != null && compositionLayerEquicrectFeature.Equirect2ExtensionEnabled)
						{
							compositionLayerEquicrectFeature.Submit_CompositionLayerEquirect2(AssignCompositionLayerParamsEquirect2(eyeid), (OpenXR.CompositionLayer.LayerType)layerType, compositionDepth, (eyeid == 0) ? layerID : layerIDRight);
						}
						break;
				}
			}

		}


		public delegate void OnDestroyCompositionLayer();
		public event OnDestroyCompositionLayer OnDestroyCompositionLayerDelegate = null;

		private void DestroyCompositionLayer(int eyeid)
		{
			if (layerTextures[eyeid] == null)
			{
				DEBUG("DestroyCompositionLayer: Layer already destroyed/not initialized.");
				return;
			}



			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			if (textureAcquired[eyeid])
			{
				DEBUG("DestroyCompositionLayer: textureAcquired, releasing.");
				textureAcquired[eyeid] = !compositionLayerFeature.CompositionLayer_ReleaseTexture((eyeid == 0) ? layerID : layerIDRight);
			}

			CompositionLayerRenderThreadSyncObject DestroyLayerSwapchainSyncObject = new CompositionLayerRenderThreadSyncObject(
				(taskQueue) =>
				{
					lock (taskQueue)
					{
						CompositionLayerRenderThreadTask task = (CompositionLayerRenderThreadTask)taskQueue.Dequeue();

						//Enumerate Swapchain formats
						compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

						if (!compositionLayerFeature.CompositionLayer_Destroy(task.layerID))
						{
							ERROR("DestroyCompositionLayer: CompositionLayer_Destroy failed : " + task.layerID);
						}

						taskQueue.Release(task);
					}
				});

			CompositionLayerRenderThreadTask.IssueDestroySwapchainEvent(DestroyLayerSwapchainSyncObject, (eyeid == 0) ? layerID : layerIDRight);

			InitStatus = false;
			isLayerReadyForSubmit = false;
			isInitializationComplete = false;
			textureAcquiredOnce[eyeid] = false;

			foreach (Texture externalTexture in layerTextures[eyeid].externalTextures)
			{
				DEBUG("DestroyCompositionLayer: External textures");
				if (externalTexture != null) Destroy(externalTexture);
			}
			layerTextures[eyeid] = null;

			if (generatedFallbackMeshFilter != null && generatedFallbackMeshFilter.mesh != null)
			{
				DEBUG("DestroyCompositionLayer: generatedFallbackMeshFilter");
				Destroy(generatedFallbackMeshFilter.mesh);
				generatedFallbackMeshFilter = null;
			}
			if (generatedFallbackMeshRenderer != null && generatedFallbackMeshRenderer.material != null)
			{
				DEBUG("DestroyCompositionLayer: generatedFallbackMeshRenderer");
				Destroy(generatedFallbackMeshRenderer.material);
				generatedFallbackMeshRenderer = null;
			}

			if (generatedUnderlayMeshFilter != null && generatedUnderlayMeshFilter.mesh != null)
			{
				DEBUG("DestroyCompositionLayer: generatedUnderlayMeshFilter");
				Destroy(generatedUnderlayMeshFilter.mesh);
				generatedUnderlayMeshFilter = null;
			}
			if (generatedUnderlayMeshRenderer != null && generatedUnderlayMeshRenderer.material != null)
			{
				DEBUG("DestroyCompositionLayer: generatedUnderlayMeshRenderer");
				Destroy(generatedUnderlayMeshRenderer.material);
				generatedUnderlayMeshRenderer = null;
			}

			if (generatedFallbackMesh != null)
			{
				Destroy(generatedFallbackMesh);
				generatedFallbackMesh = null;
			}

			if (generatedUnderlayMesh != null)
			{
				Destroy(generatedUnderlayMesh);
				generatedUnderlayMesh = null;
			}

			OnDestroyCompositionLayerDelegate?.Invoke();
		}

		private List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
		XrCompositionLayerQuad CompositionLayerParamsQuad = new XrCompositionLayerQuad();
		XrExtent2Df quadSize = new XrExtent2Df();
		private XrCompositionLayerQuad AssignCompositionLayerParamsQuad(int eyeid, bool botheye)
		{
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			CompositionLayerParamsQuad.type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_QUAD;
			CompositionLayerParamsQuad.layerFlags = ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_CORRECT_CHROMATIC_ABERRATION_BIT | ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT;

			if (!enabledColorScaleBiasInShader)
			{
				CompositionLayerParamsQuad.layerFlags |= ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
			}

			switch (layerVisibility)
			{
				default:
				case Visibility.Both:
					CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_BOTH;
					break;
				case Visibility.Left:
					CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
					break;
				case Visibility.Right:
					CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
					break;
			}

			if (!botheye) {
				if (eyeid == 0)
				    CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
			    else
				    CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
			}

			CompositionLayerParamsQuad.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsQuad.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsQuad.pose); //Update isHeadLock

			if (isHeadLock)
			{
				CompositionLayerParamsQuad.space = compositionLayerFeature.HeadLockSpace;
			}
			else
			{
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
							CompositionLayerParamsQuad.space = compositionLayerFeature.WorldLockSpaceOriginOnFloor;
							break;
						case TrackingOriginModeFlags.Device:
							CompositionLayerParamsQuad.space = compositionLayerFeature.WorldLockSpaceOriginOnHead;
							break;
					}
				}
				else
				{
					CompositionLayerParamsQuad.space = compositionLayerFeature.WorldLockSpaceOriginOnFloor;
				}
			}

			quadSize.width = m_QuadWidth;
			quadSize.height = m_QuadHeight;

			CompositionLayerParamsQuad.size = quadSize;

			return CompositionLayerParamsQuad;
		}

		XrCompositionLayerEquirectKHR CompositionLayerParamsEquirect = new XrCompositionLayerEquirectKHR();
		private XrCompositionLayerEquirectKHR AssignCompositionLayerParamsEquirect(int eyeid)
		{
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			CompositionLayerParamsEquirect.type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_EQUIRECT_KHR;
			CompositionLayerParamsEquirect.layerFlags = ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_CORRECT_CHROMATIC_ABERRATION_BIT | ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT;

			if (!enabledColorScaleBiasInShader)
			{
				CompositionLayerParamsEquirect.layerFlags |= ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
			}

			CompositionLayerParamsEquirect.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsEquirect.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsEquirect.pose); //Update isHeadLock
																		  
			// HeadLock
			CompositionLayerParamsEquirect.space = compositionLayerFeature.HeadLockSpace;

			switch (layerVisibility)
			{
				default:
				case Visibility.Both:
					CompositionLayerParamsEquirect.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_BOTH;
					break;
				case Visibility.Left:
					CompositionLayerParamsEquirect.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
					break;
				case Visibility.Right:
					CompositionLayerParamsEquirect.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
					break;
			}

			CompositionLayerParamsEquirect.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsEquirect.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsEquirect.pose);
			CompositionLayerParamsEquirect.radius = m_EquirectRadius;
			CompositionLayerParamsEquirect.scale.x = m_EquirectScaleX;
			CompositionLayerParamsEquirect.scale.y = m_EquirectScaleY;
			CompositionLayerParamsEquirect.bias.x = m_EquirectBiasX;
			CompositionLayerParamsEquirect.bias.y = m_EquirectBiasY;

			return CompositionLayerParamsEquirect;
		}

		XrCompositionLayerEquirect2KHR CompositionLayerParamsEquirect2 = new XrCompositionLayerEquirect2KHR();
		private XrCompositionLayerEquirect2KHR AssignCompositionLayerParamsEquirect2(int eyeid)
		{
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			CompositionLayerParamsEquirect2.type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_EQUIRECT2_KHR;
			CompositionLayerParamsEquirect2.layerFlags = ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_CORRECT_CHROMATIC_ABERRATION_BIT | ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT;

			if (!enabledColorScaleBiasInShader)
			{
				CompositionLayerParamsEquirect2.layerFlags |= ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
			}

			CompositionLayerParamsEquirect2.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsEquirect2.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsEquirect2.pose); //Update isHeadLock

			// HeadLock
			CompositionLayerParamsEquirect2.space = compositionLayerFeature.HeadLockSpace;

			switch (layerVisibility)
			{
				default:
				case Visibility.Both:
					CompositionLayerParamsEquirect2.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_BOTH;
					break;
				case Visibility.Left:
					CompositionLayerParamsEquirect2.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
					break;
				case Visibility.Right:
					CompositionLayerParamsEquirect2.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
					break;
			}

			CompositionLayerParamsEquirect2.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsEquirect2.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsEquirect2.pose);
			CompositionLayerParamsEquirect2.radius = m_EquirectRadius;
			CompositionLayerParamsEquirect2.centralHorizontalAngle = Mathf.Deg2Rad * m_EquirectCentralHorizontalAngle;
			CompositionLayerParamsEquirect2.upperVerticalAngle = Mathf.Deg2Rad * m_EquirectUpperVerticalAngle;
			CompositionLayerParamsEquirect2.lowerVerticalAngle = Mathf.Deg2Rad * m_EquirectLowerVerticalAngle;

			return CompositionLayerParamsEquirect2;
		}

		XrCompositionLayerCylinderKHR CompositionLayerParamsCylinder = new XrCompositionLayerCylinderKHR();
		private XrCompositionLayerCylinderKHR AssignCompositionLayerParamsCylinder(int eyeid, bool botheye)
		{
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			CompositionLayerParamsCylinder.type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_CYLINDER_KHR;
			CompositionLayerParamsCylinder.layerFlags = ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_CORRECT_CHROMATIC_ABERRATION_BIT | ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT;

			if (!enabledColorScaleBiasInShader)
			{
				CompositionLayerParamsCylinder.layerFlags |= ViveCompositionLayerHelper.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
			}

			if (isHeadLock)
			{
				CompositionLayerParamsCylinder.space = compositionLayerFeature.HeadLockSpace;
			}
			else
			{
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
							DEBUG("TrackingOriginModeFlags.Floor");
							CompositionLayerParamsCylinder.space = compositionLayerFeature.WorldLockSpaceOriginOnFloor;
							break;
						case TrackingOriginModeFlags.Device:
							DEBUG("TrackingOriginModeFlags.Device");
							CompositionLayerParamsCylinder.space = compositionLayerFeature.WorldLockSpaceOriginOnHead;
							break;
					}
				}
				else
				{
					CompositionLayerParamsCylinder.space = compositionLayerFeature.WorldLockSpaceOriginOnFloor;
				}
			}

			switch (layerVisibility)
			{
				default:
				case Visibility.Both:
					CompositionLayerParamsCylinder.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_BOTH;
					break;
				case Visibility.Left:
					CompositionLayerParamsCylinder.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
					break;
				case Visibility.Right:
					CompositionLayerParamsCylinder.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
					break;
			}

			if (!botheye) {
				if (eyeid == 0)
				    CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_LEFT;
			    else
				    CompositionLayerParamsQuad.eyeVisibility = XrEyeVisibility.XR_EYE_VISIBILITY_RIGHT;
			}

			CompositionLayerParamsCylinder.subImage.imageRect = layerTextures[eyeid].textureLayout;
			CompositionLayerParamsCylinder.subImage.imageArrayIndex = 0;
			GetCompositionLayerPose(ref CompositionLayerParamsCylinder.pose);
			CompositionLayerParamsCylinder.radius = m_CylinderRadius;
			CompositionLayerParamsCylinder.centralAngle = Mathf.Deg2Rad * m_CylinderAngleOfArc;
			CompositionLayerParamsCylinder.aspectRatio = m_CylinderArcLength / m_CylinderHeight;

			return CompositionLayerParamsCylinder;
		}

		private void ActivatePlaceholder()
		{
			if (Debug.isDebugBuild && !placeholderGenerated)
			{
				if (CompositionLayerManager.GetInstance().MaxLayerCount() == 0)//Platform does not support multi-layer. Show placeholder instead if in development build
				{
					DEBUG("Generate Placeholder");
					compositionLayerPlaceholderPrefabGO = Instantiate((GameObject)Resources.Load("Prefabs/CompositionLayerDebugPlaceholder", typeof(GameObject)));
					compositionLayerPlaceholderPrefabGO.name = "CompositionLayerDebugPlaceholder";
					compositionLayerPlaceholderPrefabGO.transform.SetParent(this.transform);
					compositionLayerPlaceholderPrefabGO.transform.position = this.transform.position;
					compositionLayerPlaceholderPrefabGO.transform.rotation = this.transform.rotation;
					compositionLayerPlaceholderPrefabGO.transform.localScale = this.transform.localScale;

					Text placeholderText = compositionLayerPlaceholderPrefabGO.transform.GetChild(0).Find("Text").GetComponent<Text>();

					placeholderText.text = placeholderText.text.Replace("{REASON}", "Device does not support Multi-Layer");

					placeholderGenerated = true;
				}
				else if (CompositionLayerManager.GetInstance().MaxLayerCount() <= CompositionLayerManager.GetInstance().CurrentLayerCount())//Do not draw layer as limit is reached. Show placeholder instead if in development build 
				{
					DEBUG("Generate Placeholder");
					compositionLayerPlaceholderPrefabGO = Instantiate((GameObject)Resources.Load("Prefabs/CompositionLayerDebugPlaceholder", typeof(GameObject)));
					compositionLayerPlaceholderPrefabGO.name = "CompositionLayerDebugPlaceholder";
					compositionLayerPlaceholderPrefabGO.transform.SetParent(this.transform);
					compositionLayerPlaceholderPrefabGO.transform.position = this.transform.position;
					compositionLayerPlaceholderPrefabGO.transform.rotation = this.transform.rotation;
					compositionLayerPlaceholderPrefabGO.transform.localScale = this.transform.localScale;

					Text placeholderText = compositionLayerPlaceholderPrefabGO.transform.GetChild(0).Find("Text").GetComponent<Text>();

					placeholderText.text = placeholderText.text.Replace("{REASON}", "Max number of layers exceeded");

					placeholderGenerated = true;
				}
			}
			else if (placeholderGenerated && compositionLayerPlaceholderPrefabGO != null)
			{
				DEBUG("Placeholder already generated, activating.");
				compositionLayerPlaceholderPrefabGO.SetActive(true);
			}
		}

		public IntPtr GetExternalSurfaceObj()
		{
			IntPtr value = compositionLayerFeature.Compositionlayer_GetExternalSurfaceObj2(layerID);
			//DEBUG("GetExternalSurfaceObj layerID " + layerID + " SurfaceObj " + value);
				
			return value;
		}

		public bool RenderAsLayer()
		{
			if (placeholderGenerated && compositionLayerPlaceholderPrefabGO != null)
			{
				compositionLayerPlaceholderPrefabGO.SetActive(false);
			}

			if (isAutoFallbackActive)
			{
				generatedFallbackMesh.SetActive(false);
				isAutoFallbackActive = false;
			}

			isRenderPriorityChanged = false;

			//if Underlay Mesh is present but needs to be reconstructed
			if (layerType == LayerType.Underlay)
			{
				if (!UnderlayMeshIsValid()) //Underlay Mesh needs to be generated
				{
					UnderlayMeshGeneration();
				}
				else if (LayerDimensionsChanged()) //if Underlay Mesh is present but needs to be updated
				{
					UnderlayMeshUpdate();
				}
				else generatedUnderlayMesh.SetActive(true);
			}

			return CompositionLayerInit();
		}

		public void RenderInGame()
		{
			compositionLayerFeature = compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();
			if (compositionLayerFeature.enableAutoFallback)
			{
				if (!isAutoFallbackActive)
				{
					//Activate auto fallback
					//Activate auto fallback
					if (!FallbackMeshIsValid())
					{
						AutoFallbackMeshGeneration();
					}
					else if (LayerDimensionsChanged())
					{
						AutoFallbackMeshUpdate();
					}
					generatedFallbackMesh.SetActive(true);
					isAutoFallbackActive = true;
				}
			}
			else //Use placeholder
			{
				ActivatePlaceholder();
			}

			if (layerType == LayerType.Underlay && generatedUnderlayMesh != null)
			{
				generatedUnderlayMesh.SetActive(false);
			}

			isRenderPriorityChanged = false;
		}

		public void TerminateLayer()
		{
			if (layerID != 0)
			{
				DEBUG("TerminateLayer: layerID: " + layerID);
				DestroyCompositionLayer(0);
				layerID = 0;
			}
			
			if (layerIDRight != 0)
			{
				DEBUG("TerminateLayer: layerIDRight: " + layerIDRight);
				DestroyCompositionLayer(1);
				layerIDRight = 0;
			}
			    

			if (placeholderGenerated && compositionLayerPlaceholderPrefabGO != null)
			{
				compositionLayerPlaceholderPrefabGO.SetActive(false);
			}

			if (isAutoFallbackActive)
			{
				generatedFallbackMesh.SetActive(false);
				isAutoFallbackActive = false;
			}
		}

		public bool TextureParamsChanged()
		{
			if (previousTextureLeft != textureLeft || previousTextureRight != textureRight)
			{
			    previousTextureLeft = textureLeft;
				previousTextureRight = textureRight;
				return true;
			}

			if (previousIsDynamicLayer != isDynamicLayer)
			{
				previousIsDynamicLayer = isDynamicLayer;
				return true;
			}

			return false;
		}

		public bool LayerDimensionsChanged()
		{
			bool isChanged = false;

			if (layerShape == LayerShape.Equirect)
			{
				if (previousEquirectRadius != m_EquirectRadius)
				{
					previousEquirectRadius = m_EquirectRadius;
					isChanged = true;
				}
			}
			else if (layerShape == LayerShape.Cylinder)
			{
				if (previousAngleOfArc != m_CylinderAngleOfArc ||
					previousCylinderArcLength != m_CylinderArcLength ||
					previousCylinderHeight != m_CylinderHeight ||
					previousCylinderRadius != m_CylinderRadius)
				{
					previousAngleOfArc = m_CylinderAngleOfArc;
					previousCylinderArcLength = m_CylinderArcLength;
					previousCylinderHeight = m_CylinderHeight;
					previousCylinderRadius = m_CylinderRadius;
					isChanged = true;
				}
			}
			else if (layerShape == LayerShape.Quad)
			{
				if (previousQuadWidth != m_QuadWidth ||
					previousQuadHeight != m_QuadHeight)
				{
					previousQuadWidth = m_QuadWidth;
					previousQuadHeight = m_QuadHeight;
					isChanged = true;
				}
			}

			if (previousLayerShape != layerShape)
			{
				previousLayerShape = layerShape;
				isChanged = true;
			}

			return isChanged;
		}

		#region Quad Runtime Parameter Change
		/// <summary>
		/// Use this function to update the width of a Quad Layer.
		/// </summary>
		/// <param name="inWidth"></param>
		/// <returns></returns>
		public bool SetQuadLayerWidth(float inWidth)
		{
			if (inWidth <= 0)
			{
				return false;
			}

			m_QuadWidth = inWidth;

			return true;
		}

		/// <summary>
		/// Use this function to update the height of a Quad Layer.
		/// </summary>
		/// <param name="inHeight"></param>
		/// <returns></returns>
		public bool SetQuadLayerHeight(float inHeight)
		{
			if (inHeight <= 0)
			{
				return false;
			}

			m_QuadHeight = inHeight;

			return true;
		}
		#endregion

		#region Cylinder Runtime Parameter Change
		/// <summary>
		/// Use this function to update the radius and arc angle of a Cylinder Layer. 
		/// A new arc length will be calculated automatically.
		/// </summary>
		/// <param name="inRadius"></param>
		/// <param name="inArcAngle"></param>
		/// <returns>True if the parameters are valid and successfully updated.</returns>
		public bool SetCylinderLayerRadiusAndArcAngle(float inRadius, float inArcAngle)
		{
			//Check if radius is valid
			if (inRadius <= 0)
			{
				return false;
			}

			//Check if angle of arc is valid
			if (inArcAngle < angleOfArcLowerLimit || inArcAngle > angleOfArcUpperLimit)
			{
				return false;
			}

			//Check if new arc length is valid
			float newArcLength = CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(inArcAngle, inRadius);
			if (newArcLength <= 0)
			{
				return false;
			}

			//All parameters are valid, assign to layer
			m_CylinderArcLength = newArcLength;
			m_CylinderRadius = inRadius;
			m_CylinderAngleOfArc = inArcAngle;

			return true;
		}

		/// <summary>
		/// Use this function to update the radius and arc length of a Cylinder Layer. 
		/// A new arc angle will be calculated automatically.
		/// </summary>
		/// <param name="inRadius"></param>
		/// <param name="inArcLength"></param>
		/// <returns>True if the parameters are valid and successfully updated.</returns>
		public bool SetCylinderLayerRadiusAndArcLength(float inRadius, float inArcLength)
		{
			//Check if radius is valid
			if (inRadius <= 0)
			{
				return false;
			}

			//Check if arc length is valid
			if (inArcLength <= 0)
			{
				return false;
			}

			//Check if new arc angle is valid
			float newArcAngle = CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(inArcLength, inRadius);
			if (newArcAngle < angleOfArcLowerLimit || newArcAngle > angleOfArcUpperLimit)
			{
				return false;
			}

			//All parameters are valid, assign to layer
			m_CylinderArcLength = inArcLength;
			m_CylinderRadius = inRadius;
			m_CylinderAngleOfArc = newArcAngle;

			return true;
		}

		/// <summary>
		/// Use this function to update the arc angle and arc length of a Cylinder Layer. 
		/// A new radius will be calculated automatically.
		/// </summary>
		/// <param name="inArcAngle"></param>
		/// <param name="inArcLength"></param>
		/// <returns>True if the parameters are valid and successfully updated.</returns>
		public bool SetCylinderLayerArcAngleAndArcLength(float inArcAngle, float inArcLength)
		{
			//Check if arc angle is valid
			if (inArcAngle < angleOfArcLowerLimit || inArcAngle > angleOfArcUpperLimit)
			{
				return false;
			}

			//Check if arc length is valid
			if (inArcLength <= 0)
			{
				return false;
			}

			//Check if new radius is valid
			float newRadius = CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, inArcAngle);
			if (newRadius <= 0)
			{
				return false;
			}

			//All parameters are valid, assign to layer
			m_CylinderArcLength = inArcLength;
			m_CylinderRadius = newRadius;
			m_CylinderAngleOfArc = inArcAngle;

			return true;

		}

		/// <summary>
		/// Use this function to update the height of a Cylinder Layer.
		/// </summary>
		/// <param name="inHeight"></param>
		/// <returns></returns>
		public bool SetCylinderLayerHeight(float inHeight)
		{
			if (inHeight <= 0)
			{
				return false;
			}

			m_CylinderHeight = inHeight;

			return true;
		}

		#endregion

#if UNITY_EDITOR
		public CylinderLayerParamAdjustmentMode CurrentAdjustmentMode()
		{
			if (previousCylinderArcLength != m_CylinderArcLength)
			{
				return CylinderLayerParamAdjustmentMode.ArcLength;
			}
			else if (previousAngleOfArc != m_CylinderAngleOfArc)
			{
				return CylinderLayerParamAdjustmentMode.ArcAngle;
			}
			else
			{
				return CylinderLayerParamAdjustmentMode.Radius;
			}
		}
#endif

		public void ChangeBlitShadermode(BlitShaderMode shaderMode, bool enable)
		{
			if (texture2DBlitMaterial == null) return;

			switch (shaderMode)
			{
				case BlitShaderMode.LINEAR_TO_SRGB_COLOR:
					if (enable)
					{
						texture2DBlitMaterial.EnableKeyword("LINEAR_TO_SRGB_COLOR");
					}
					else
					{
						texture2DBlitMaterial.DisableKeyword("LINEAR_TO_SRGB_COLOR");
					}
					break;
				case BlitShaderMode.LINEAR_TO_SRGB_ALPHA:
					if (enable)
					{
						texture2DBlitMaterial.EnableKeyword("LINEAR_TO_SRGB_ALPHA");
					}
					else
					{
						texture2DBlitMaterial.DisableKeyword("LINEAR_TO_SRGB_ALPHA");
					}
					break;
				default:
					break;
			}
		}
		#endregion

		#region Monobehavior Lifecycle
		private void Awake()
		{
			//Create blit mat
			texture2DBlitMaterial = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/Texture2DBlitShader"));

			//Create render thread synchornizer
			if (synchronizer == null) synchronizer = new RenderThreadSynchronizer();

			ColorSpace currentActiveColorSpace = QualitySettings.activeColorSpace;
			if (currentActiveColorSpace == ColorSpace.Linear)
			{
				isLinear = true;
			}
			else
			{
				isLinear = false;
			}

			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();
		}

		private void OnEnable()
		{
			hmd = Camera.main;

			CompositionLayerManager.GetInstance().SubscribeToLayerManager(this);
			if (!isOnBeforeRenderSubscribed)
			{
				CompositionLayerManager.GetInstance().CompositionLayerManagerOnBeforeRenderDelegate += OnBeforeRender;
				isOnBeforeRenderSubscribed = true;
			}
		}

		private void OnDisable()
		{
			if (isOnBeforeRenderSubscribed)
			{
				CompositionLayerManager.GetInstance().CompositionLayerManagerOnBeforeRenderDelegate -= OnBeforeRender;
				isOnBeforeRenderSubscribed = false;
			}
			CompositionLayerManager.GetInstance().UnsubscribeFromLayerManager(this, false);
		}

		private void OnDestroy()
		{
			if (isOnBeforeRenderSubscribed)
			{
				CompositionLayerManager.GetInstance().CompositionLayerManagerOnBeforeRenderDelegate -= OnBeforeRender;
				isOnBeforeRenderSubscribed = false;
			}

			Destroy(texture2DBlitMaterial);

			CompositionLayerManager.GetInstance().UnsubscribeFromLayerManager(this, true);
		}

		private void LateUpdate()
		{
			if (isAutoFallbackActive) //Do not submit when auto fallback is active
			{
				//Check if auto fallback mesh needs to be updated
				if (!FallbackMeshIsValid()) //fallback Mesh needs to be generated
				{
					AutoFallbackMeshGeneration();
				}
				else if (LayerDimensionsChanged()) //if fallback Mesh is present but needs to be updated
				{
					AutoFallbackMeshUpdate();
				}

				//Handle possible lossy scale change
				if (generatedFallbackMesh.transform.lossyScale != Vector3.one)
				{
					generatedFallbackMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);
				}

				return;
			}

			if (layerType == LayerType.Underlay)
			{
				if (!UnderlayMeshIsValid()) //Underlay Mesh needs to be generated
				{
					UnderlayMeshGeneration();
				}
				else if (LayerDimensionsChanged()) //if Underlay Mesh is present but needs to be updated
				{
					UnderlayMeshUpdate();
				}

				//Handle possible lossy scale change
				if (generatedUnderlayMesh.transform.lossyScale != Vector3.one)
				{
					generatedUnderlayMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);
				}

				generatedUnderlayMesh.SetActive(true);
			}
		}

		private void OnBeforeRender()
		{
			compositionLayerFeature = OpenXRSettings.Instance.GetFeature<ViveCompositionLayer>();

			isLayerReadyForSubmit = false;

			if (compositionLayerFeature.XrSessionEnding) return;

			if (!isInitializationComplete) //Layer not marked as initialized
			{
				if (InitStatus) //Initialized
				{
					reinitializationNeeded = false;
					isInitializationComplete = true;
					isSynchronized = false;
				}
				else if (reinitializationNeeded) //Layer is still active but needs to be reinitialized
				{
					CompositionLayerInit();
					return;
				}
				else
				{
					DEBUG("Composition Layer Lifecycle OnBeforeRender: Layer not initialized.");
					return;
				}
			}

			if (!isSynchronized)
			{
				DEBUG("CompositionLayer: Sync");
				if (synchronizer != null)
				{
					synchronizer.sync();
					isSynchronized = true;
				}
			}

			if (isAutoFallbackActive || ((compositionLayerPlaceholderPrefabGO != null) && compositionLayerPlaceholderPrefabGO.activeSelf)) //Do not submit when auto fallback or placeholder is active
			{
				return;
			}

			if (compositionLayerFeature.XrSessionCurrentState != XrSessionState.XR_SESSION_STATE_VISIBLE && compositionLayerFeature.XrSessionCurrentState != XrSessionState.XR_SESSION_STATE_FOCUSED)
			{
				//DEBUG("XrSessionCurrentState is not focused or visible");
				return;
			}

			bool isBotheye = (textureRight == null || textureLeft == textureRight);

			if (isCustomRects)
			{
			    isBotheye = false;
			}

			if (SetLayerTexture(0))
			{
				isLayerReadyForSubmit = true;
			}

			if (!isLayerReadyForSubmit)
			{
				DEBUG("Composition Layer Lifecycle OnBeforeRender: Layer not ready for submit.");
				return;
			}

			if (!isBotheye) {
				if (SetLayerTexture(1))
			    {
				    isLayerReadyForSubmit = true;
			    }
			    if (!isLayerReadyForSubmit)
			    {
				    DEBUG("Composition Layer Lifecycle OnBeforeRender: Layer not ready for submit.");
				    return;
			    }
			}

			SubmitCompositionLayer(0, isBotheye);
			if (!isBotheye)
			    SubmitCompositionLayer(1, isBotheye);

			isLayerReadyForSubmit = false; //reset flag after submit
		}

		#endregion

		#region Mesh Generation
		public void AutoFallbackMeshGeneration()
		{
			if (generatedFallbackMeshFilter != null && generatedFallbackMeshFilter.mesh != null)
			{
				Destroy(generatedFallbackMeshFilter.mesh);
			}
			if (generatedFallbackMeshRenderer != null && generatedFallbackMeshRenderer.material != null)
			{
				Destroy(generatedFallbackMeshRenderer.material);
			}
			if (generatedFallbackMesh != null) Destroy(generatedFallbackMesh);

			Mesh generatedMesh = null;

			switch (layerShape)
			{
				case LayerShape.Quad:
					generatedMesh = MeshGenerationHelper.GenerateQuadMesh(MeshGenerationHelper.GenerateQuadVertex(m_QuadWidth, m_QuadHeight));
					break;
				case LayerShape.Cylinder:
					generatedMesh = MeshGenerationHelper.GenerateCylinderMesh(m_CylinderAngleOfArc, MeshGenerationHelper.GenerateCylinderVertex(m_CylinderAngleOfArc, m_CylinderRadius, m_CylinderHeight));
					break;
				case LayerShape.Equirect:
					generatedMesh = MeshGenerationHelper.GenerateEquirectMesh(hmd, m_EquirectRadius);
					break;
			}

			generatedFallbackMesh = new GameObject();
			generatedFallbackMesh.SetActive(false);

			generatedFallbackMesh.name = FallbackMeshName;
			generatedFallbackMesh.transform.SetParent(gameObject.transform);
			generatedFallbackMesh.transform.localPosition = Vector3.zero;
			generatedFallbackMesh.transform.localRotation = Quaternion.identity;

			generatedFallbackMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);

			generatedFallbackMeshRenderer = generatedFallbackMesh.AddComponent<MeshRenderer>();
			generatedFallbackMeshFilter = generatedFallbackMesh.AddComponent<MeshFilter>();

			generatedFallbackMeshFilter.mesh = generatedMesh;

			Material fallBackMat = new Material(Shader.Find("Unlit/Transparent"));
			fallBackMat.mainTexture = texture;
			generatedFallbackMeshRenderer.material = fallBackMat;
		}

		public void AutoFallbackMeshUpdate()
		{
			if (generatedFallbackMesh == null || generatedFallbackMeshRenderer == null || generatedFallbackMeshFilter == null)
			{
				return;
			}

			Mesh generatedMesh = null;

			switch (layerShape)
			{
				case LayerShape.Quad:
					generatedMesh = MeshGenerationHelper.GenerateQuadMesh(MeshGenerationHelper.GenerateQuadVertex(m_QuadWidth, m_QuadHeight));
					break;
				case LayerShape.Cylinder:
					generatedMesh = MeshGenerationHelper.GenerateCylinderMesh(m_CylinderAngleOfArc, MeshGenerationHelper.GenerateCylinderVertex(m_CylinderAngleOfArc, m_CylinderRadius, m_CylinderHeight));
					break;
				case LayerShape.Equirect:
					generatedMesh = MeshGenerationHelper.GenerateEquirectMesh(hmd, m_EquirectRadius);
					break;
			}

			generatedFallbackMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);
			Destroy(generatedFallbackMeshFilter.mesh);
			generatedFallbackMeshFilter.mesh = generatedMesh;
			generatedFallbackMeshRenderer.material.mainTexture = texture;
		}

		public bool FallbackMeshIsValid()
		{
			if (generatedFallbackMesh == null || generatedFallbackMeshRenderer == null || generatedFallbackMeshFilter == null)
			{
				return false;
			}
			else if (generatedFallbackMeshFilter.mesh == null || generatedFallbackMeshRenderer.material == null)
			{
				return false;
			}
			return true;
		}

		public void UnderlayMeshGeneration()
		{
			if (generatedUnderlayMeshFilter != null && generatedUnderlayMeshFilter.mesh != null)
			{
				Destroy(generatedUnderlayMeshFilter.mesh);
			}
			if (generatedUnderlayMeshRenderer != null && generatedUnderlayMeshRenderer.material != null)
			{
				Destroy(generatedUnderlayMeshRenderer.material);
			}
			if (generatedUnderlayMesh != null) Destroy(generatedUnderlayMesh);

			switch (layerShape)
			{
				case LayerShape.Equirect:
					generatedUnderlayMesh = new GameObject();
					generatedUnderlayMesh.name = EquirectUnderlayMeshName;
					generatedUnderlayMesh.transform.SetParent(transform);
					generatedUnderlayMesh.transform.localPosition = Vector3.zero;
					generatedUnderlayMesh.transform.localRotation = Quaternion.identity;

					generatedUnderlayMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);

					generatedUnderlayMeshRenderer = generatedUnderlayMesh.AddComponent<MeshRenderer>();
					generatedUnderlayMeshFilter = generatedUnderlayMesh.AddComponent<MeshFilter>();
					if (solidEffect)
						generatedUnderlayMeshRenderer.sharedMaterial = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZeroSolid"));
					else
						generatedUnderlayMeshRenderer.sharedMaterial = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZero"));
					generatedUnderlayMeshRenderer.material.mainTexture = texture;

					//Generate Mesh
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateEquirectMesh(hmd, m_EquirectRadius);
					break;
				case LayerShape.Cylinder:
					//Generate vertices
					Vector3[] cylinderVertices = MeshGenerationHelper.GenerateCylinderVertex(m_CylinderAngleOfArc, m_CylinderRadius, m_CylinderHeight);

					//Add components to Game Object
					generatedUnderlayMesh = new GameObject();
					generatedUnderlayMesh.name = CylinderUnderlayMeshName;
					generatedUnderlayMesh.transform.SetParent(transform);
					generatedUnderlayMesh.transform.localPosition = Vector3.zero;
					generatedUnderlayMesh.transform.localRotation = Quaternion.identity;

					generatedUnderlayMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);

					generatedUnderlayMeshRenderer = generatedUnderlayMesh.AddComponent<MeshRenderer>();
					generatedUnderlayMeshFilter = generatedUnderlayMesh.AddComponent<MeshFilter>();
					if (solidEffect)
						generatedUnderlayMeshRenderer.sharedMaterial = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZeroSolid"));
					else
						generatedUnderlayMeshRenderer.sharedMaterial = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZero"));
					generatedUnderlayMeshRenderer.material.mainTexture = texture;

					//Generate Mesh
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateCylinderMesh(m_CylinderAngleOfArc, cylinderVertices);
					break;
				case LayerShape.Quad:
				default:
					//Generate vertices
					Vector3[] quadVertices = MeshGenerationHelper.GenerateQuadVertex(m_QuadWidth, m_QuadHeight);

					//Add components to Game Object
					generatedUnderlayMesh = new GameObject();
					generatedUnderlayMesh.name = QuadUnderlayMeshName;
					generatedUnderlayMesh.transform.SetParent(transform);
					generatedUnderlayMesh.transform.localPosition = Vector3.zero;
					generatedUnderlayMesh.transform.localRotation = Quaternion.identity;

					generatedUnderlayMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);

					generatedUnderlayMeshRenderer = generatedUnderlayMesh.AddComponent<MeshRenderer>();
					generatedUnderlayMeshFilter = generatedUnderlayMesh.AddComponent<MeshFilter>();
					if (solidEffect)
						generatedUnderlayMeshRenderer.material = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZeroSolid"));
					else
						generatedUnderlayMeshRenderer.material = new Material(Shader.Find("VIVE/OpenXR/CompositionLayer/UnderlayAlphaZero"));
					generatedUnderlayMeshRenderer.material.mainTexture = texture;

					//Generate Mesh
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateQuadMesh(quadVertices);
					break;
			}
		}

		public void UnderlayMeshUpdate()
		{
			if (generatedUnderlayMesh == null || generatedUnderlayMeshRenderer == null || generatedUnderlayMeshFilter == null)
			{
				return;
			}

			switch (layerShape)
			{
				case LayerShape.Equirect:
					Destroy(generatedUnderlayMeshFilter.mesh);
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateEquirectMesh(hmd, m_EquirectRadius);
					break;
				case LayerShape.Cylinder:
					//Generate vertices
					Vector3[] cylinderVertices = MeshGenerationHelper.GenerateCylinderVertex(m_CylinderAngleOfArc, m_CylinderRadius, m_CylinderHeight);

					//Generate Mesh
					Destroy(generatedUnderlayMeshFilter.mesh);
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateCylinderMesh(m_CylinderAngleOfArc, cylinderVertices);
					break;
				case LayerShape.Quad:
				default:
					//Generate vertices
					Vector3[] quadVertices = MeshGenerationHelper.GenerateQuadVertex(m_QuadWidth, m_QuadHeight);

					//Generate Mesh
					Destroy(generatedUnderlayMeshFilter.mesh);
					generatedUnderlayMeshFilter.mesh = MeshGenerationHelper.GenerateQuadMesh(quadVertices);
					break;
			}

			generatedUnderlayMesh.transform.localScale = GetNormalizedLocalScale(transform, Vector3.one);
		}

		public Vector3 GetNormalizedLocalScale(Transform targetTransform, Vector3 targetGlobalScale) //Return the local scale needed to make it match to the target global scale
		{
			Vector3 normalizedLocalScale = new Vector3(targetGlobalScale.x / targetTransform.lossyScale.x, targetGlobalScale.y / targetTransform.lossyScale.y, targetGlobalScale.z / targetTransform.lossyScale.z);

			return normalizedLocalScale;
		}

		public bool UnderlayMeshIsValid()
		{
			if (generatedUnderlayMesh == null || generatedUnderlayMeshRenderer == null || generatedUnderlayMeshFilter == null)
			{
				return false;
			}
			else if (generatedUnderlayMeshFilter.mesh == null || generatedUnderlayMeshRenderer.material == null)
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Enum Definitions
		public enum LayerType
		{
			Overlay = 1,
			Underlay = 2,
		}

		public enum LayerShape
		{
			Quad = 0,
			Cylinder = 1,
			Equirect = 2,
			Equirect2 = 3,
		}

		public enum Visibility
		{
			Both = 0,
			Left = 1,
			Right = 2,
		}

		public enum CustomRectsType
		{
			LeftRight = 1,
			TopDown = 2,
		}

#if UNITY_EDITOR
		public enum CylinderLayerParamAdjustmentMode
		{
			Radius = 0,
			ArcLength = 1,
			ArcAngle = 2,
		}

		public enum CylinderLayerParamLockMode
		{
			ArcLength = 0,
			ArcAngle = 1,
		}
#endif

		public enum BlitShaderMode
		{
			LINEAR_TO_SRGB_COLOR = 0,
			LINEAR_TO_SRGB_ALPHA = 1,
		}
		#endregion

		#region Helper Classes
		private class LayerTextures
		{
			private uint layerTextureQueueLength;

			public uint currentAvailableTextureIndex { get; set; }
			public IntPtr[] textureIDs;
			public Texture[] externalTextures;
			public bool[] textureContentSet;
			public XrRect2Di textureLayout { get; set; }

			public LayerTextures(uint swapchainImageCount)
			{
				layerTextureQueueLength = swapchainImageCount;
				textureIDs = new IntPtr[swapchainImageCount];
				externalTextures = new Texture[swapchainImageCount];
				textureContentSet = new bool[swapchainImageCount];

				for (int i = 0; i < swapchainImageCount; i++)
				{
					textureContentSet[i] = false;
					textureIDs[i] = IntPtr.Zero;
				}
			}

			public IntPtr GetCurrentAvailableTextureID()
			{
				if (currentAvailableTextureIndex < 0 || currentAvailableTextureIndex > layerTextureQueueLength - 1)
				{
					return IntPtr.Zero;
				}
				return textureIDs[currentAvailableTextureIndex];
			}

			public void SetCurrentAvailableTextureID(IntPtr newTextureID)
			{
				if (currentAvailableTextureIndex < 0 || currentAvailableTextureIndex > layerTextureQueueLength - 1)
				{
					return;
				}
				textureIDs[currentAvailableTextureIndex] = newTextureID;
			}

			public Texture GetCurrentAvailableExternalTexture()
			{
				if (currentAvailableTextureIndex < 0 || currentAvailableTextureIndex > layerTextureQueueLength - 1)
				{
					return null;
				}
				return externalTextures[currentAvailableTextureIndex];
			}

			public void SetCurrentAvailableExternalTexture(Texture newExternalTexture)
			{
				if (currentAvailableTextureIndex < 0 || currentAvailableTextureIndex > layerTextureQueueLength - 1)
				{
					return;
				}
				externalTextures[currentAvailableTextureIndex] = newExternalTexture;
			}
		}

		private class CompositionLayerRenderThreadTask : Task
		{
			public int layerID;

			public CompositionLayerRenderThreadTask() { }

			public static void IssueObtainSwapchainEvent(CompositionLayerRenderThreadSyncObject syncObject)
			{
				PreAllocatedQueue taskQueue = syncObject.Queue;
				lock (taskQueue)
				{
					CompositionLayerRenderThreadTask task = taskQueue.Obtain<CompositionLayerRenderThreadTask>();
					taskQueue.Enqueue(task);
				}
				syncObject.IssueEvent();
			}

			public static void IssueDestroySwapchainEvent(CompositionLayerRenderThreadSyncObject syncObject, int inLayerID)
			{
				PreAllocatedQueue taskQueue = syncObject.Queue;
				lock (taskQueue)
				{
					CompositionLayerRenderThreadTask task = taskQueue.Obtain<CompositionLayerRenderThreadTask>();
					task.layerID = inLayerID;
					taskQueue.Enqueue(task);
				}
				syncObject.IssueEvent();
			}
		}

		private class RenderThreadSynchronizer
		{
			RenderTexture mutable = new RenderTexture(1, 1, 0);
			public RenderThreadSynchronizer()
			{
				mutable.useMipMap = false;
				mutable.Create();
			}

			public void sync()
			{
				var originalLogType = Application.GetStackTraceLogType(LogType.Error);
				Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

				// Sync
				mutable.GetNativeTexturePtr();

				Application.SetStackTraceLogType(LogType.Error, originalLogType);
			}
		}

		private class UnityToOpenXRConversionHelper
		{
			public static void GetOpenXRQuaternion(Quaternion rot, ref XrQuaternionf qua)
			{
				qua.x = rot.x;
				qua.y = rot.y;
				qua.z = -rot.z;
				qua.w = -rot.w;
			}

			public static void GetOpenXRVector(Vector3 pos, ref XrVector3f vec)
			{
				vec.x = pos.x;
				vec.y = pos.y;
				vec.z = -pos.z;
			}

			public static void GetOpenXRColor4f(Color color, ref XrColor4f color4f)
			{
				color4f.r = color.r;
				color4f.g = color.g;
				color4f.b = color.b;
				color4f.a = color.a;
			}
		}

		public static class MeshGenerationHelper
		{
			public static Vector3[] GenerateQuadVertex(float quadWidth, float quadHeight)
			{
				Vector3[] vertices = new Vector3[4]; //Four corners

				vertices[0] = new Vector3(-quadWidth / 2, -quadHeight / 2, 0); //Bottom Left
				vertices[1] = new Vector3(quadWidth / 2, -quadHeight / 2, 0); //Bottom Right
				vertices[2] = new Vector3(-quadWidth / 2, quadHeight / 2, 0); //Top Left
				vertices[3] = new Vector3(quadWidth / 2, quadHeight / 2, 0); //Top Right

				return vertices;
			}

			public static Mesh GenerateQuadMesh(Vector3[] vertices)
			{
				Mesh quadMesh = new Mesh();
				quadMesh.vertices = vertices;

				//Create array that represents vertices of the triangles
				int[] triangles = new int[6];
				triangles[0] = 0;
				triangles[1] = 2;
				triangles[2] = 1;

				triangles[3] = 1;
				triangles[4] = 2;
				triangles[5] = 3;

				quadMesh.triangles = triangles;
				Vector2[] uv = new Vector2[vertices.Length];
				Vector4[] tangents = new Vector4[vertices.Length];
				Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
				for (int i = 0, y = 0; y < 2; y++)
				{
					for (int x = 0; x < 2; x++, i++)
					{
						uv[i] = new Vector2((float)x, (float)y);
						tangents[i] = tangent;
					}
				}
				quadMesh.uv = uv;
				quadMesh.tangents = tangents;
				quadMesh.RecalculateNormals();

				return quadMesh;
			}

			public static Vector3[] GenerateCylinderVertex(float cylinderAngleOfArc, float cylinderRadius, float cylinderHeight)
			{
				float angleUpperLimitDeg = cylinderAngleOfArc / 2; //Degrees
				float angleLowerLimitDeg = -angleUpperLimitDeg; //Degrees

				float angleUpperLimitRad = angleUpperLimitDeg * Mathf.Deg2Rad; //Radians
				float angleLowerLimitRad = angleLowerLimitDeg * Mathf.Deg2Rad; //Radians

				int arcSegments = Mathf.RoundToInt(cylinderAngleOfArc / 5f);

				float anglePerArcSegmentRad = (cylinderAngleOfArc / arcSegments) * Mathf.Deg2Rad;

				Vector3[] vertices = new Vector3[2 * (arcSegments + 1)]; //Top and bottom lines * Vertex count per line

				int vertexCount = 0;
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < arcSegments + 1; j++) //Clockwise
					{
						float currentVertexAngleRad = angleLowerLimitRad + anglePerArcSegmentRad * j;
						float x = cylinderRadius * Mathf.Sin(currentVertexAngleRad);
						float y = 0;
						float z = cylinderRadius * Mathf.Cos(currentVertexAngleRad);

						if (i == 1) //Top
						{
							y += cylinderHeight / 2;

						}
						else //Bottom
						{
							y -= cylinderHeight / 2;
						}

						vertices[vertexCount] = new Vector3(x, y, z);
						vertexCount++;
					}
				}

				return vertices;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static Vector3 InverseTransformVert(in Vector3 vert, in Vector3 position, in Vector3 scale, float worldScale)
			{
				return new Vector3(
					(worldScale * vert.x - position.x) / scale.x,
					(worldScale * vert.y - position.y) / scale.y,
					(worldScale * vert.z - position.z) / scale.z);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static Vector2 GetSphereUV(float theta, float phi, float expandScale)
			{
				float thetaU = expandScale * (theta / (2 * Mathf.PI) - 0.5f) + 0.5f;
				float phiV = expandScale * phi / Mathf.PI + 0.5f;

				return new Vector2(thetaU, phiV);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static Vector3 GetSphereVert(float theta, float phi)
			{
				return new Vector3(-Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(phi), -Mathf.Cos(theta) * Mathf.Cos(phi));
			}

			public static void BuildSphere(List<Vector3> verts, List<Vector2> uv, List<int> triangles, Vector3 position,
				Quaternion rotation, Vector3 scale, Rect rect, float worldScale = 800, int latitudes = 128,
				int longitudes = 128, float expandCoefficient = 1.0f)
			{
				position = Quaternion.Inverse(rotation) * position;

				latitudes = Mathf.CeilToInt(latitudes * rect.height);
				longitudes = Mathf.CeilToInt(longitudes * rect.width);

				float minTheta = Mathf.PI * 2.0f * rect.x;
				float minPhi = Mathf.PI * (0.5f - rect.y - rect.height);

				float thetaScale = Mathf.PI * 2.0f * rect.width / longitudes;
				float phiScale = Mathf.PI * rect.height / latitudes;

				float expandScale = 1.0f / expandCoefficient;

				for (int j = 0; j < latitudes + 1; j += 1)
				{
					for (int k = 0; k < longitudes + 1; k++)
					{
						float theta = minTheta + k * thetaScale;
						float phi = minPhi + j * phiScale;

						Vector2 suv = GetSphereUV(theta, phi, expandScale);
						uv.Add(new Vector2((suv.x - rect.x) / rect.width, (suv.y - rect.y) / rect.height));
						Vector3 vert = GetSphereVert(theta, phi);
						verts.Add(InverseTransformVert(in vert, in position, in scale, worldScale));
					}
				}

				for (int j = 0; j < latitudes; j++)
				{
					for (int k = 0; k < longitudes; k++)
					{
						triangles.Add(j * (longitudes + 1) + k);
						triangles.Add((j + 1) * (longitudes + 1) + k);
						triangles.Add((j + 1) * (longitudes + 1) + k + 1);
						triangles.Add((j + 1) * (longitudes + 1) + k + 1);
						triangles.Add(j * (longitudes + 1) + k + 1);
						triangles.Add(j * (longitudes + 1) + k);
					}
				}
			}

			public static Mesh GenerateEquirectMesh(Camera hmd, float equirectRadius)
			{
				Mesh eqicrectMesh = new Mesh();
				List<int> _Tris = new List<int>();
 			    List<Vector2> _UV = new List<Vector2>();
			    List<Vector3> _Verts = new List<Vector3>();
			    Rect _Rect = new Rect(0, 0, 1, 1);

			    BuildSphere(_Verts, _UV, _Tris, Camera.main.transform.position, Camera.main.transform.rotation, equirectRadius* Vector3.one, _Rect);

				eqicrectMesh.SetVertices(_Verts);
				eqicrectMesh.SetTriangles(_Tris, 0);
				eqicrectMesh.SetUVs(0, _UV);
				eqicrectMesh.UploadMeshData(false);
				return eqicrectMesh;
			}

			public static Mesh GenerateCylinderMesh(float cylinderAngleOfArc, Vector3[] vertices)
			{
				Mesh cylinderMesh = new Mesh();
				cylinderMesh.vertices = vertices;
				int arcSegments = Mathf.RoundToInt(cylinderAngleOfArc / 5f);

				//Create array that represents vertices of the triangles
				int[] triangles = new int[arcSegments * 6];
				for (int currentTriangleIndex = 0, currentVertexIndex = 0, y = 0; y < 1; y++, currentVertexIndex++)
				{
					for (int x = 0; x < arcSegments; x++, currentTriangleIndex += 6, currentVertexIndex++)
					{
						triangles[currentTriangleIndex] = currentVertexIndex;
						triangles[currentTriangleIndex + 1] = currentVertexIndex + arcSegments + 1;
						triangles[currentTriangleIndex + 2] = currentVertexIndex + 1;

						triangles[currentTriangleIndex + 3] = currentVertexIndex + 1;
						triangles[currentTriangleIndex + 4] = currentVertexIndex + arcSegments + 1;
						triangles[currentTriangleIndex + 5] = currentVertexIndex + arcSegments + 2;
					}
				}
				cylinderMesh.triangles = triangles;
				Vector2[] uv = new Vector2[vertices.Length];
				Vector4[] tangents = new Vector4[vertices.Length];
				Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
				for (int i = 0, y = 0; y < 2; y++)
				{
					for (int x = 0; x < arcSegments + 1; x++, i++)
					{
						uv[i] = new Vector2((float)x / arcSegments, (float)y);
						tangents[i] = tangent;
					}
				}
				cylinderMesh.uv = uv;
				cylinderMesh.tangents = tangents;
				cylinderMesh.RecalculateNormals();

				return cylinderMesh;
			}
		}

		public static class CylinderParameterHelper
		{
			public static float RadiusAndDegAngleOfArcToArcLength(float inDegAngleOfArc, float inRadius)
			{
				float arcLength = inRadius * (inDegAngleOfArc * Mathf.Deg2Rad);

				return arcLength;
			}

			public static float RadiusAndArcLengthToDegAngleOfArc(float inArcLength, float inRadius)
			{
				float degAngleOfArc = (inArcLength / inRadius) * Mathf.Rad2Deg;

				return degAngleOfArc;
			}

			public static float ArcLengthAndDegAngleOfArcToRadius(float inArcLength, float inDegAngleOfArc)
			{
				float radius = (inArcLength / (inDegAngleOfArc * Mathf.Deg2Rad));

				return radius;
			}
		}

		#endregion
	}
}
