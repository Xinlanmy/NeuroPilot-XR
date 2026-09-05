// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using System.Linq;

namespace VIVE.OpenXR.Passthrough
{
	public static class PassthroughAPI
	{
		#region LOG
		const string LOG_TAG = "VIVE.OpenXR.Passthrough.PassthroughAPI";
		static void DEBUG(string msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		static void WARNING(string msg) { Debug.LogWarningFormat("{0} {1}", LOG_TAG, msg); }
		static void ERROR(string msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		private static VivePassthrough passthroughFeature = null;
		private static bool AssertFeature()
		{
			if (passthroughFeature == null) { passthroughFeature = OpenXRSettings.Instance.GetFeature<VivePassthrough>(); }
			if (passthroughFeature) { return true; }
			return false;
		}

		private static Dictionary<XrPassthroughHTC, PassthroughLayer> layersDict = new Dictionary<XrPassthroughHTC, PassthroughLayer>();

		#region Public APIs
		/// <summary>
		/// Creates a fullscreen passthrough.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreatePlanarPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null, float alpha = 1f, uint compositionDepth = 0)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}
			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
#if UNITY_ANDROID
                IntPtr.Zero,
#else
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
#endif
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PLANAR_HTC
			);

			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if(res == XrResult.XR_SUCCESS)
			{
				PassthroughLayer layer = new PassthroughLayer(passthrough, layerType);
				var xrLayer = PassthroughLayer.MakeEmptyLayer();
                xrLayer.passthrough = passthrough;
				xrLayer.layerFlags = (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
                xrLayer.color.alpha = alpha;
				layer.SetLayer(xrLayer);

				layersDict.Add(passthrough, layer);
			}

			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
			}

			return res;
		}

		/// <summary>
		/// Creates a projected passthrough (i.e. Passthrough is only partially visible). Visible region of the projected passthrough is determined by the mesh and its transform.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="vertexBuffer">Positions of the vertices in the mesh.</param>
		/// <param name="indexBuffer">List of triangles represented by indices into the <paramref name="vertexBuffer"/>.</param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreateProjectedPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType,
											  [In, Out] Vector3[] vertexBuffer, [In, Out] int[] indexBuffer, //For Mesh
											  ProjectedPassthroughSpaceType spaceType, Vector3 meshPosition, Quaternion meshOrientation, Vector3 meshScale, //For Mesh Transform
											  VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null,
											  float alpha = 1f, uint compositionDepth = 0, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}

			if (vertexBuffer.Length < 3 || indexBuffer.Length % 3 != 0) //Must have at least 3 vertices and complete triangles
			{
				ERROR("Mesh data invalid.");
				return res;
			}

			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
#if UNITY_ANDROID
				IntPtr.Zero,
#else
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
#endif
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PROJECTED_HTC
			);

			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if (res == XrResult.XR_SUCCESS)
			{
				var layer = new PassthroughLayer(passthrough, layerType);
				var xrLayer = PassthroughLayer.MakeEmptyLayer();
				xrLayer.passthrough = passthrough;
				xrLayer.layerFlags = (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
                xrLayer.space = 0;
				xrLayer.color.alpha = alpha;
                layer.SetLayer(xrLayer);
                var xrMesh = PassthroughLayer.MakeMeshTransform();

                xrMesh.time = XR_HTC_passthrough.Interop.GetFrameState().predictedDisplayTime;
                xrMesh.baseSpace = XR_HTC_passthrough.Interop.GetTrackingSpace();
                xrMesh.scale = new XrVector3f(meshScale.x, meshScale.y, meshScale.z);
				layer.SetMeshTransform(xrMesh);

				layersDict.Add(passthrough, layer);
			}

			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
				SetProjectedPassthroughMesh(passthrough, vertexBuffer, indexBuffer, convertFromUnityToOpenXR);
				SetProjectedPassthroughMeshTransform(passthrough, spaceType, meshPosition, meshOrientation, meshScale, trackingToWorldSpace, convertFromUnityToOpenXR);
			}

			return res;
		}

		/// <summary>
		/// Creates a projected passthrough (i.e. Passthrough is only partially visible). Visible region of the projected passthrough is determined by the mesh and its transform.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreateProjectedPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null, float alpha = 1f, uint compositionDepth = 0)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}

			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
#if UNITY_ANDROID
                IntPtr.Zero,
#else
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
#endif
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PROJECTED_HTC
            );

			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if (res == XrResult.XR_SUCCESS)
			{
				var layer = new PassthroughLayer(passthrough, layerType);
				var xrLayer = PassthroughLayer.MakeEmptyLayer();
				xrLayer.passthrough = passthrough;
				xrLayer.layerFlags = (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT;
				xrLayer.color.alpha = alpha;
				layer.SetLayer(xrLayer);

				var xrMesh = PassthroughLayer.MakeMeshTransform();
				layer.SetMeshTransform(xrMesh, true);

				layersDict.Add(passthrough, layer);
			}

			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
			}

			return res;
		}

		private static void SubmitLayer()
		{
            passthroughFeature.SubmitLayers(layersDict.Values.ToList());
        }

        /// <summary>
        /// To Destroying a passthrough.
        /// You should call this function when the <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> is invoked.
        /// </summary>
        /// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
        /// <returns>XR_SUCCESS for success.</returns>
        public static XrResult DestroyPassthrough(XrPassthroughHTC passthrough)
		{
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}
			if (!passthroughFeature.PassthroughList.Contains(passthrough))
			{
				ERROR("Passthrough to be destroyed not found");
				return res;
			}

			if (layersDict.ContainsKey(passthrough))
			{
                XR_HTC_passthrough.xrDestroyPassthroughHTC(passthrough);
				var layer = layersDict[passthrough];
                layer.Dispose();
                layersDict.Remove(passthrough);
			}

			SubmitLayer();
			
			res = XrResult.XR_SUCCESS;
			return res;
		}

		/// <summary>
		/// Modifies the opacity of a specific passthrough layer.
		/// Can be used for both Planar and Projected passthroughs.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="autoClamp">
		/// Specify whether out of range alpha values should be clamped automatically.
		/// When set to true, the function will clamp and apply the alpha value automatically.
		/// When set to false, the function will return false if the alpha is out of range.
		/// Default is true.
		/// </param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetPassthroughAlpha(XrPassthroughHTC passthrough, float alpha, bool autoClamp = true)
		{
			bool ret = false;
			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (layersDict.ContainsKey(passthrough))
			{
				var layer = layersDict[passthrough];
				var xrLayer = layer.GetLayer();
				xrLayer.color.alpha = alpha;
				layer.SetLayer(xrLayer);
				
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
			return ret;
		}

		/// <summary>
		/// Modifies the mesh data of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="vertexBuffer">Positions of the vertices in the mesh.</param>
		/// <param name="indexBuffer">List of triangles represented by indices into the <paramref name="vertexBuffer"/>.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMesh(XrPassthroughHTC passthrough, [In, Out] Vector3[] vertexBuffer, [In, Out] int[] indexBuffer, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (vertexBuffer.Length < 3 || indexBuffer.Length % 3 != 0) //Must have at least 3 vertices and complete triangles
			{
				ERROR("Mesh data invalid.");
				return ret;
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			layer.SetMeshData(ref xrMesh, vertexBuffer, indexBuffer, convertFromUnityToOpenXR);
			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies the mesh transform of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshTransform(XrPassthroughHTC passthrough, ProjectedPassthroughSpaceType spaceType, Vector3 meshPosition, Quaternion meshOrientation, Vector3 meshScale, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Vector3 trackingSpaceMeshPosition = meshPosition;
			Quaternion trackingSpaceMeshRotation = meshOrientation;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(currentTrackingSpaceOrigin.transform.position, currentTrackingSpaceOrigin.transform.rotation, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(meshPosition, meshOrientation, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshPosition = trackingSpaceLayerPoseTRS.GetColumn(3); //4th Column of TRS Matrix is the position
				trackingSpaceMeshRotation = Quaternion.LookRotation(trackingSpaceLayerPoseTRS.GetColumn(2), trackingSpaceLayerPoseTRS.GetColumn(1));
			}

			XrPosef meshXrPose;
			meshXrPose.position = OpenXRHelper.ToOpenXRVector(trackingSpaceMeshPosition, convertFromUnityToOpenXR);
			meshXrPose.orientation = OpenXRHelper.ToOpenXRQuaternion(trackingSpaceMeshRotation, convertFromUnityToOpenXR);

			XrVector3f meshXrScale = OpenXRHelper.ToOpenXRVector(meshScale, false);

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			xrMesh.pose = meshXrPose;
			xrMesh.scale = meshXrScale;
			xrMesh.time = XR_HTC_passthrough.Interop.GetFrameState().predictedDisplayTime;
			xrMesh.baseSpace = passthroughFeature.GetXrSpaceFromSpaceType(spaceType);

			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies layer type and composition depth of a passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetPassthroughLayerType(XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, uint compositionDepth = 0)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			layer.LayerType = layerType;
			layer.Depth = (int)compositionDepth;

			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies the space of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughSpaceType(XrPassthroughHTC passthrough, ProjectedPassthroughSpaceType spaceType)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			xrMesh.baseSpace = passthroughFeature.GetXrSpaceFromSpaceType(spaceType);
			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies the mesh position of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshPosition(XrPassthroughHTC passthrough, Vector3 meshPosition, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Vector3 trackingSpaceMeshPosition = meshPosition;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(currentTrackingSpaceOrigin.transform.position, Quaternion.identity, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(meshPosition, Quaternion.identity, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshPosition = trackingSpaceLayerPoseTRS.GetColumn(3); //4th Column of TRS Matrix is the position
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			xrMesh.pose.position = OpenXRHelper.ToOpenXRVector(trackingSpaceMeshPosition, convertFromUnityToOpenXR);
			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies the mesh orientation of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshOrientation(XrPassthroughHTC passthrough, Quaternion meshOrientation, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Quaternion trackingSpaceMeshRotation = meshOrientation;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(Vector3.zero, currentTrackingSpaceOrigin.transform.rotation, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(Vector3.zero, meshOrientation, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshRotation = Quaternion.LookRotation(trackingSpaceLayerPoseTRS.GetColumn(2), trackingSpaceLayerPoseTRS.GetColumn(1));
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			xrMesh.pose.orientation = OpenXRHelper.ToOpenXRQuaternion(trackingSpaceMeshRotation, convertFromUnityToOpenXR);
			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// Modifies the mesh scale of a passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughScale(XrPassthroughHTC passthrough, Vector3 meshScale)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (layersDict[passthrough] == null)
            {
                ERROR("Passthrough layer not found.");
                return ret;
            }

			var layer = layersDict[passthrough];
			var xrMesh = layer.GetMesh();
			xrMesh.scale = OpenXRHelper.ToOpenXRVector(meshScale, false);
			layer.SetMeshTransform(xrMesh);
			SubmitLayer();
			return true;
		}

		/// <summary>
		/// To get the list of IDs of active passthrough layers.
		/// </summary>
		/// <returns>
		/// The a copy of the list of IDs of active passthrough layers.
		/// </returns>
		public static List<XrPassthroughHTC> GetCurrentPassthroughLayerIDs()
		{
			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return null;
			}

			return passthroughFeature.PassthroughList;
		}
#endregion
	}
}