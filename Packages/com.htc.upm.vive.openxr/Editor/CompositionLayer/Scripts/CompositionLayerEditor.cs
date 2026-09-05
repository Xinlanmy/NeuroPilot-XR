// "VIVE SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the VIVE SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;

#if UNITY_EDITOR
namespace VIVE.OpenXR.CompositionLayer.Editor
{
#region Composition Layer Editor

	using UnityEditor;
	using UnityEditor.SceneManagement;
	using VIVE.OpenXR.CompositionLayer;
	using UnityEditor.XR.OpenXR.Features;

	[CustomEditor(typeof(CompositionLayer))]
	public class CompositionLayerEditor : Editor
	{
		static string PropertyName_LayerType = "layerType";
		static GUIContent Label_LayerType = new GUIContent("Type", "Specify the type of the composition layer.");
		SerializedProperty Property_LayerType;

		static string PropertyName_CompositionDepth = "compositionDepth";
		static GUIContent Label_CompositionDepth = new GUIContent("Composition Depth", "Specify the composition depth of the layer.");
		SerializedProperty Property_CompositionDepth;

		static string PropertyName_LayerShape = "layerShape";
		static GUIContent Label_LayerShape = new GUIContent("Shape", "Specify the shape of the layer.");
		SerializedProperty Property_LayerShape;

		static string PropertyName_LayerVisibility = "layerVisibility";
		static GUIContent Label_LayerVisibility = new GUIContent("Visibility", "Specify the visibility of the layer.");
		SerializedProperty Property_LayerVisibility;

		static string PropertyName_LockMode = "lockMode";
		static GUIContent Label_LockMode = new GUIContent("Locked Parameter", "Cylinder Layer parameter to be locked when changing the radius.");
		SerializedProperty Property_LockMode;

		static string PropertyName_QuadWidth = "m_QuadWidth";
		static GUIContent Label_QuadWidth = new GUIContent("Width", "Width of a Quad Layer");
		SerializedProperty Property_QuadWidth;

		static string PropertyName_QuadHeight = "m_QuadHeight";
		static GUIContent Label_QuadHeight = new GUIContent("Height", "Height of a Quad Layer");
		SerializedProperty Property_QuadHeight;

		static string PropertyName_EquirectRadius = "m_EquirectRadius";
		static GUIContent Label_EquirectRadius = new GUIContent("Radius", "Radius of Equirect Layer");
		SerializedProperty Property_EquirectRadius;

		static string PropertyName_EquirectScaleX = "m_EquirectScaleX";
		static GUIContent Label_EquirectScaleX = new GUIContent("scale.x", "Scale.X of Equirect Layer");
		SerializedProperty Property_EquirectScaleX;

		static string PropertyName_EquirectScaleY = "m_EquirectScaleY";
		static GUIContent Label_EquirectScaleY = new GUIContent("scale.y", "Scale.Y of Equirect Layer");
		SerializedProperty Property_EquirectScaleY;

		static string PropertyName_EquirectBiasX = "m_EquirectBiasX";
		static GUIContent Label_EquirectBiasX = new GUIContent("bias.x", "Bias.X of Equirect Layer");
		SerializedProperty Property_EquirectBiasX;

		static string PropertyName_EquirectBiasY = "m_EquirectBiasY";
		static GUIContent Label_EquirectBiasY = new GUIContent("bias.y", "Bias.Y of Equirect Layer");
		SerializedProperty Property_EquirectBiasY;

		static string PropertyName_EquirectCentralHorizontalAngle = "m_EquirectCentralHorizontalAngle";
		static GUIContent Label_EquirectCentralHorizontalAngle = new GUIContent("CentralHorizontalAngle", "Central Horizontal Angle of Equirect Layer");
		SerializedProperty Property_EquirectCentralHorizontalAngle;

		static string PropertyName_EquirectUpperVerticalAngle = "m_EquirectUpperVerticalAngle";
		static GUIContent Label_EquirectUpperVerticalAngle = new GUIContent("UpperVerticalAngle", "Upper Vertical Angle of Equirect Layer");
		SerializedProperty Property_EquirectUpperVerticalAngle;

		static string PropertyName_EquirectLowerVerticalAngle = "m_EquirectLowerVerticalAngle";
		static GUIContent Label_EquirectLowerVerticalAngle = new GUIContent("LowerVerticalAngle", "Lower Vertical Angle of Equirect Layer");
		SerializedProperty Property_EquirectLowerVerticalAngle;


		static string PropertyName_CylinderHeight = "m_CylinderHeight";
		static GUIContent Label_CylinderHeight = new GUIContent("Height", "Height of Cylinder Layer");
		SerializedProperty Property_CylinderHeight;

		static string PropertyName_CylinderArcLength = "m_CylinderArcLength";
		static GUIContent Label_CylinderArcLength = new GUIContent("Arc Length", "Arc Length of Cylinder Layer");
		SerializedProperty Property_CylinderArcLength;

		static string PropertyName_CylinderRadius = "m_CylinderRadius";
		static GUIContent Label_CylinderRadius = new GUIContent("Radius", "Radius of Cylinder Layer");
		SerializedProperty Property_CylinderRadius;

		static string PropertyName_AngleOfArc = "m_CylinderAngleOfArc";
		static GUIContent Label_AngleOfArc = new GUIContent("Arc Angle", "Central angle of arc of Cylinder Layer");
		SerializedProperty Property_AngleOfArc;

		static string PropertyName_isExternalSurface = "isExternalSurface";
		static GUIContent Label_isExternalSurface = new GUIContent("External Surface", "Specify using external surface or not.");
		SerializedProperty Property_isExternalSurface;

		static string PropertyName_ExternalSurfaceWidth = "externalSurfaceWidth";
		static GUIContent Label_ExternalSurfaceWidth = new GUIContent("Width");
		SerializedProperty Property_ExternalSurfaceWidth;

		static string PropertyName_ExternalSurfaceHeight = "externalSurfaceHeight";
		static GUIContent Label_ExternalSurfaceHeight = new GUIContent("Height");
		SerializedProperty Property_ExternalSurfaceHeight;

		static string PropertyName_IsDynamicLayer = "isDynamicLayer";
		static GUIContent Label_IsDynamicLayer = new GUIContent("Dynamic Layer", "Specify whether Layer needs to be updated each frame or not.");
		SerializedProperty Property_IsDynamicLayer;

		static string PropertyName_IsCustomRects = "isCustomRects";
		static GUIContent Label_IsCustomRects = new GUIContent("Customize Rects", "Using a single texture as a stereo image");
		SerializedProperty Property_IsCustomRects;

		static string PropertyName_CustomRects = "customRects";
		static GUIContent Label_CustomRects = new GUIContent("Customize Rects Type", "Specify the customize rects type of the left texture.");
		SerializedProperty Property_CustomRects;


		static string PropertyName_ApplyColorScaleBias = "applyColorScaleBias";
		static GUIContent Label_ApplyColorScaleBias = new GUIContent("Apply Color Scale Bias", "Color scale and bias are applied to a layer color during composition, after its conversion to premultiplied alpha representation. LayerColor = LayerColor * colorScale + colorBias");
		SerializedProperty Property_ApplyColorScaleBias;

		static string PropertyName_SolidEffect = "solidEffect";
		static GUIContent Label_SolidEffect = new GUIContent("Solid Effect", "Apply UnderLay Color Scale Bias in Runtime Level.");
		SerializedProperty Property_SolidEffect;


		static string PropertyName_ColorScale = "colorScale";
		static GUIContent Label_ColorScale = new GUIContent("Color Scale", "Will be used for modulatting the color sourced from the images.");
		SerializedProperty Property_ColorScale;

		static string PropertyName_ColorBias = "colorBias";
		static GUIContent Label_ColorBias = new GUIContent("Color Bias", "Will be used for offseting the color sourced from the images.");
		SerializedProperty Property_ColorBias;

		static string PropertyName_IsProtectedSurface = "isProtectedSurface";
		static GUIContent Label_IsProtectedSurface = new GUIContent("Protected Surface");
		SerializedProperty Property_IsProtectedSurface;

		static string PropertyName_RenderPriority = "renderPriority";
		static GUIContent Label_RenderPriority = new GUIContent("Render Priority", "When Auto Fallback is enabled, layers with a higher render priority will be rendered as normal layers first.");
		SerializedProperty Property_RenderPriority;

		static string PropertyName_TrackingOrigin = "trackingOrigin";
		static GUIContent Label_TrackingOrigin = new GUIContent("Tracking Origin", "Assign the tracking origin here to offset the pose of the Composition Layer.");
		SerializedProperty Property_TrackingOrigin;

		private bool showLayerParams = true, showColorScaleBiasParams = true;

		//private bool showExternalSurfaceParams = false;

		Rect FullRect = new Rect(0, 0, 1, 1);
		Rect LeftRightRect = new Rect(0, 0, 0.5f, 1);
		Rect TopDownRect = new Rect(0, 0.5f, 1, 0.5f);
		public override void OnInspectorGUI()
		{
			if (Property_LayerType == null) Property_LayerType = serializedObject.FindProperty(PropertyName_LayerType);
			if (Property_CompositionDepth == null) Property_CompositionDepth = serializedObject.FindProperty(PropertyName_CompositionDepth);
			if (Property_LayerShape == null) Property_LayerShape = serializedObject.FindProperty(PropertyName_LayerShape);
			if (Property_LayerVisibility == null) Property_LayerVisibility = serializedObject.FindProperty(PropertyName_LayerVisibility);
			if (Property_CustomRects == null) Property_CustomRects = serializedObject.FindProperty(PropertyName_CustomRects);
			if (Property_LockMode == null) Property_LockMode = serializedObject.FindProperty(PropertyName_LockMode);
			if (Property_QuadWidth == null) Property_QuadWidth = serializedObject.FindProperty(PropertyName_QuadWidth);
			if (Property_QuadHeight == null) Property_QuadHeight = serializedObject.FindProperty(PropertyName_QuadHeight);
			if (Property_EquirectRadius == null) Property_EquirectRadius = serializedObject.FindProperty(PropertyName_EquirectRadius);
			if (Property_EquirectScaleX == null) Property_EquirectScaleX = serializedObject.FindProperty(PropertyName_EquirectScaleX);
			if (Property_EquirectScaleY == null) Property_EquirectScaleY = serializedObject.FindProperty(PropertyName_EquirectScaleY);
			if (Property_EquirectBiasX == null) Property_EquirectBiasX = serializedObject.FindProperty(PropertyName_EquirectBiasX);
			if (Property_EquirectBiasY == null) Property_EquirectBiasY = serializedObject.FindProperty(PropertyName_EquirectBiasY);
			if (Property_EquirectCentralHorizontalAngle == null) Property_EquirectCentralHorizontalAngle = serializedObject.FindProperty(PropertyName_EquirectCentralHorizontalAngle);
			if (Property_EquirectUpperVerticalAngle == null) Property_EquirectUpperVerticalAngle = serializedObject.FindProperty(PropertyName_EquirectUpperVerticalAngle);
			if (Property_EquirectLowerVerticalAngle == null) Property_EquirectLowerVerticalAngle = serializedObject.FindProperty(PropertyName_EquirectLowerVerticalAngle);
			if (Property_CylinderHeight == null) Property_CylinderHeight = serializedObject.FindProperty(PropertyName_CylinderHeight);
			if (Property_CylinderArcLength == null) Property_CylinderArcLength = serializedObject.FindProperty(PropertyName_CylinderArcLength);
			if (Property_CylinderRadius == null) Property_CylinderRadius = serializedObject.FindProperty(PropertyName_CylinderRadius);
			if (Property_AngleOfArc == null) Property_AngleOfArc = serializedObject.FindProperty(PropertyName_AngleOfArc);
			if (Property_isExternalSurface == null) Property_isExternalSurface = serializedObject.FindProperty(PropertyName_isExternalSurface);
			if (Property_ExternalSurfaceWidth == null) Property_ExternalSurfaceWidth = serializedObject.FindProperty(PropertyName_ExternalSurfaceWidth);
			if (Property_ExternalSurfaceHeight == null) Property_ExternalSurfaceHeight = serializedObject.FindProperty(PropertyName_ExternalSurfaceHeight);
			if (Property_IsDynamicLayer == null) Property_IsDynamicLayer = serializedObject.FindProperty(PropertyName_IsDynamicLayer);
			if (Property_IsCustomRects == null) Property_IsCustomRects = serializedObject.FindProperty(PropertyName_IsCustomRects);
			if (Property_ApplyColorScaleBias == null) Property_ApplyColorScaleBias = serializedObject.FindProperty(PropertyName_ApplyColorScaleBias);
			if (Property_SolidEffect == null) Property_SolidEffect = serializedObject.FindProperty(PropertyName_SolidEffect);
			if (Property_ColorScale == null) Property_ColorScale = serializedObject.FindProperty(PropertyName_ColorScale);
			if (Property_ColorBias == null) Property_ColorBias = serializedObject.FindProperty(PropertyName_ColorBias);
			if (Property_IsProtectedSurface == null) Property_IsProtectedSurface = serializedObject.FindProperty(PropertyName_IsProtectedSurface);
			if (Property_RenderPriority == null) Property_RenderPriority = serializedObject.FindProperty(PropertyName_RenderPriority);
			if (Property_TrackingOrigin == null) Property_TrackingOrigin = serializedObject.FindProperty(PropertyName_TrackingOrigin);

			CompositionLayer targetCompositionLayer = target as CompositionLayer;

			if (!FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, ViveCompositionLayer.featureId).enabled)
			{
				EditorGUILayout.HelpBox("The Composition Layer feature is not enabled in OpenXR Settings.\nEnable it to use the Composition Layer.", MessageType.Warning);
			}

			EditorGUILayout.PropertyField(Property_LayerType, new GUIContent(Label_LayerType));
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.PropertyField(Property_CompositionDepth, new GUIContent(Label_CompositionDepth));
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.PropertyField(Property_LayerShape, new GUIContent(Label_LayerShape));
			serializedObject.ApplyModifiedProperties();

			if (Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Equirect || Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Equirect2)
			{
				if (targetCompositionLayer.isPreviewingQuad)
				{
					targetCompositionLayer.isPreviewingQuad = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				if (targetCompositionLayer.isPreviewingCylinder)
				{
					targetCompositionLayer.isPreviewingCylinder = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				if (!FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, ViveCompositionLayerEquirect.featureId).enabled)
				{
					EditorGUILayout.HelpBox("The Composition Layer Equirect feature is not enabled in OpenXR Settings.\nEnable it to use Equirect layers.", MessageType.Warning);
				}

				EditorGUI.indentLevel++;
				showLayerParams = EditorGUILayout.Foldout(showLayerParams, "Equirect Parameters");
				if (showLayerParams)
				{
					EditorGUILayout.PropertyField(Property_EquirectRadius, new GUIContent(Label_EquirectRadius));

					if (Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Equirect)
					{
						EditorGUILayout.PropertyField(Property_EquirectScaleX, new GUIContent(Label_EquirectScaleX));
						EditorGUILayout.PropertyField(Property_EquirectScaleY, new GUIContent(Label_EquirectScaleY));
						EditorGUILayout.PropertyField(Property_EquirectBiasX, new GUIContent(Label_EquirectBiasX));
						EditorGUILayout.PropertyField(Property_EquirectBiasY, new GUIContent(Label_EquirectBiasY));
					}

					else if (Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Equirect2)
					{
						EditorGUILayout.PropertyField(Property_EquirectCentralHorizontalAngle, new GUIContent(Label_EquirectCentralHorizontalAngle));
						EditorGUILayout.PropertyField(Property_EquirectUpperVerticalAngle, new GUIContent(Label_EquirectUpperVerticalAngle));
						EditorGUILayout.PropertyField(Property_EquirectLowerVerticalAngle, new GUIContent(Label_EquirectLowerVerticalAngle));
					}
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUI.indentLevel--;

				bool EquirectParamsChanged = targetCompositionLayer.LayerDimensionsChanged();
				if (targetCompositionLayer.isPreviewingEquirect)
				{
					Transform generatedPreviewTransform = targetCompositionLayer.transform.Find(CompositionLayer.EquirectPreviewName);

					if (generatedPreviewTransform != null)
					{
						targetCompositionLayer.generatedPreview = generatedPreviewTransform.gameObject;

						if (EquirectParamsChanged)
						{
							MeshFilter equirectMeshFilter = targetCompositionLayer.generatedPreview.GetComponent<MeshFilter>();

							//Generate vertices
							equirectMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateEquirectMesh(targetCompositionLayer.hmd, targetCompositionLayer.EquirectRadius);

							targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
							targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

							targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);
						}

						if (targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != targetCompositionLayer.texture)
						{
							targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = targetCompositionLayer.texture;
						}

						if (GUILayout.Button("Hide Equirect Preview"))
						{
							targetCompositionLayer.isPreviewingEquirect = false;
							if (targetCompositionLayer.generatedPreview != null)
							{
								DestroyImmediate(targetCompositionLayer.generatedPreview);
							}
						}
					}
					else
					{
						targetCompositionLayer.isPreviewingEquirect = false;
					}
				}
				else
				{
					if (GUILayout.Button("Show Equirect Preview"))
					{
						Rect srcRectLeft = FullRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.LeftRight)
							srcRectLeft = LeftRightRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.TopDown)
							srcRectLeft = TopDownRect;

						targetCompositionLayer.isPreviewingEquirect = true;
						//Vector3[] cylinderVertices = CompositionLayer.MeshGenerationHelper.GenerateCylinderVertex(targetCompositionLayer.CylinderAngleOfArc, targetCompositionLayer.CylinderRadius, targetCompositionLayer.CylinderHeight);
						//Add components to Game Object
						targetCompositionLayer.generatedPreview = new GameObject();
						targetCompositionLayer.generatedPreview.hideFlags = HideFlags.HideAndDontSave;
						targetCompositionLayer.generatedPreview.name = CompositionLayer.EquirectPreviewName;
						targetCompositionLayer.generatedPreview.transform.SetParent(targetCompositionLayer.gameObject.transform);
						targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
						targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

						targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);

						MeshRenderer equirectMeshRenderer = targetCompositionLayer.generatedPreview.AddComponent<MeshRenderer>();
						MeshFilter equirectMeshFilter = targetCompositionLayer.generatedPreview.AddComponent<MeshFilter>();
						equirectMeshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));

						if (targetCompositionLayer.texture != null)
						{
							equirectMeshRenderer.sharedMaterial.mainTexture = targetCompositionLayer.texture;
							equirectMeshRenderer.sharedMaterial.mainTextureOffset = srcRectLeft.position;
							equirectMeshRenderer.sharedMaterial.mainTextureScale = srcRectLeft.size;
						}

						//Generate Mesh
						equirectMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateEquirectMesh(targetCompositionLayer.hmd, targetCompositionLayer.EquirectRadius);
					}
				}

				EditorGUILayout.Space(10);
				serializedObject.ApplyModifiedProperties();

			}
			if (Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Cylinder)
			{
				if (!FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, ViveCompositionLayerCylinder.featureId).enabled)
				{
					EditorGUILayout.HelpBox("The Composition Layer Cylinder feature is not enabled in OpenXR Settings.\nEnable it to use Cylinder layers.", MessageType.Warning);
				}

				if (targetCompositionLayer.isPreviewingQuad)
				{
					targetCompositionLayer.isPreviewingQuad = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				if (targetCompositionLayer.isPreviewingEquirect)
				{
					targetCompositionLayer.isPreviewingEquirect = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				Transform generatedQuadTransform = targetCompositionLayer.transform.Find(CompositionLayer.QuadUnderlayMeshName);
				if (generatedQuadTransform != null)
				{
					DestroyImmediate(generatedQuadTransform.gameObject);
				}

				EditorGUI.indentLevel++;
				showLayerParams = EditorGUILayout.Foldout(showLayerParams, "Cylinder Parameters");
				if (showLayerParams)
				{
					float radiusLowerBound = Mathf.Max(0.001f, CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(targetCompositionLayer.cylinderArcLength, targetCompositionLayer.angleOfArcUpperLimit));
					float radiusUpperBound = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(targetCompositionLayer.cylinderArcLength, targetCompositionLayer.angleOfArcLowerLimit);
					EditorGUILayout.HelpBox("Changing the Arc Length will affect the upper and lower bounds of the radius.\nUpper Bound of Radius: " + radiusUpperBound + "\nLower Bound of Radius: " + radiusLowerBound, MessageType.Info);

					EditorGUILayout.PropertyField(Property_CylinderRadius, new GUIContent(Label_CylinderRadius));
					EditorGUILayout.PropertyField(Property_LockMode, new GUIContent(Label_LockMode));
					serializedObject.ApplyModifiedProperties();

					EditorGUILayout.HelpBox("Arc Length and Arc Angle are correlated, adjusting one of them changes the other as well. The Radius will not be changed when adjusting these two values.", MessageType.Info);
					if (targetCompositionLayer.lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcLength)
					{
						GUI.enabled = false;
						EditorGUILayout.PropertyField(Property_CylinderArcLength, new GUIContent(Label_CylinderArcLength));
						GUI.enabled = true;
						EditorGUILayout.Slider(Property_AngleOfArc, targetCompositionLayer.angleOfArcLowerLimit, targetCompositionLayer.angleOfArcUpperLimit, new GUIContent(Label_AngleOfArc));
					}
					else if (targetCompositionLayer.lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcAngle)
					{
						EditorGUILayout.PropertyField(Property_CylinderArcLength, new GUIContent(Label_CylinderArcLength));
						GUI.enabled = false;
						EditorGUILayout.Slider(Property_AngleOfArc, targetCompositionLayer.angleOfArcLowerLimit, targetCompositionLayer.angleOfArcUpperLimit, new GUIContent(Label_AngleOfArc));
						GUI.enabled = true;
					}
					EditorGUILayout.PropertyField(Property_CylinderHeight, new GUIContent(Label_CylinderHeight));
				}
				EditorGUI.indentLevel--;

				serializedObject.ApplyModifiedProperties();

				CompositionLayer.CylinderLayerParamAdjustmentMode currentAdjustmentMode = targetCompositionLayer.CurrentAdjustmentMode();

				switch (currentAdjustmentMode)
				{
					case CompositionLayer.CylinderLayerParamAdjustmentMode.ArcLength:
						{
							targetCompositionLayer.CylinderAngleOfArc = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(targetCompositionLayer.CylinderArcLength, targetCompositionLayer.CylinderRadius);
							float cylinderArcLengthRef = targetCompositionLayer.CylinderArcLength;
							if (!ArcLengthValidityCheck(ref cylinderArcLengthRef, targetCompositionLayer.CylinderRadius, targetCompositionLayer.angleOfArcLowerLimit, targetCompositionLayer.angleOfArcUpperLimit))
							{
								targetCompositionLayer.CylinderArcLength = cylinderArcLengthRef;
								targetCompositionLayer.CylinderAngleOfArc = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(targetCompositionLayer.CylinderArcLength, targetCompositionLayer.CylinderRadius);
							}
							serializedObject.ApplyModifiedProperties();
							break;
						}
					case CompositionLayer.CylinderLayerParamAdjustmentMode.ArcAngle:
						{
							targetCompositionLayer.CylinderArcLength = CompositionLayer.CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(targetCompositionLayer.CylinderAngleOfArc, targetCompositionLayer.CylinderRadius);
							serializedObject.ApplyModifiedProperties();
							break;
						}
					case CompositionLayer.CylinderLayerParamAdjustmentMode.Radius:
					default:
						{
							float cylinderRadiusRef = targetCompositionLayer.CylinderRadius;
							RadiusValidityCheck(targetCompositionLayer.CylinderArcLength, ref cylinderRadiusRef, targetCompositionLayer.angleOfArcLowerLimit, targetCompositionLayer.angleOfArcUpperLimit, targetCompositionLayer.lockMode);
							targetCompositionLayer.CylinderRadius = cylinderRadiusRef;
							if (targetCompositionLayer.lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcLength)
							{
								targetCompositionLayer.CylinderAngleOfArc = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(targetCompositionLayer.CylinderArcLength, targetCompositionLayer.CylinderRadius);
								float cylinderArcLengthRef = targetCompositionLayer.CylinderArcLength;
								if (!ArcLengthValidityCheck(ref cylinderArcLengthRef, targetCompositionLayer.CylinderRadius, targetCompositionLayer.angleOfArcLowerLimit, targetCompositionLayer.angleOfArcUpperLimit))
								{
									targetCompositionLayer.CylinderArcLength = cylinderArcLengthRef;
									targetCompositionLayer.CylinderAngleOfArc = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(targetCompositionLayer.CylinderArcLength, targetCompositionLayer.CylinderRadius);
								}
							}
							else if (targetCompositionLayer.lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcAngle)
							{
								targetCompositionLayer.CylinderArcLength = CompositionLayer.CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(targetCompositionLayer.CylinderAngleOfArc, targetCompositionLayer.CylinderRadius);
							}
							serializedObject.ApplyModifiedProperties();
							break;
						}
				}

				EditorGUILayout.HelpBox("Current Layer Aspect Ratio (Arc Length : Height) = " + targetCompositionLayer.CylinderArcLength + " : " + targetCompositionLayer.CylinderHeight, MessageType.Info);

				Vector3 CompositionLayerScale = targetCompositionLayer.gameObject.transform.localScale;
				bool CylinderParamsChanged = targetCompositionLayer.LayerDimensionsChanged();

				if (targetCompositionLayer.isPreviewingCylinder)
				{

					Transform generatedPreviewTransform = targetCompositionLayer.transform.Find(CompositionLayer.CylinderPreviewName);

					if (generatedPreviewTransform != null)
					{
						targetCompositionLayer.generatedPreview = generatedPreviewTransform.gameObject;

						if (CylinderParamsChanged)
						{
							//Generate vertices
							Vector3[] cylinderVertices = CompositionLayer.MeshGenerationHelper.GenerateCylinderVertex(targetCompositionLayer.CylinderAngleOfArc, targetCompositionLayer.CylinderRadius, targetCompositionLayer.CylinderHeight);;

							MeshFilter cylinderMeshFilter = targetCompositionLayer.generatedPreview.GetComponent<MeshFilter>();
							//Generate Mesh
							cylinderMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateCylinderMesh(targetCompositionLayer.CylinderAngleOfArc, cylinderVertices);

							targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
							targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

							targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);
						}

						if (targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != targetCompositionLayer.texture)
						{
							targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = targetCompositionLayer.texture;
						}

						if (GUILayout.Button("Hide Cylinder Preview"))
						{
							targetCompositionLayer.isPreviewingCylinder = false;
							if (targetCompositionLayer.generatedPreview != null)
							{
								DestroyImmediate(targetCompositionLayer.generatedPreview);
							}
						}
					}
					else
					{
						targetCompositionLayer.isPreviewingCylinder = false;
					}
				}
				else
				{
					if (GUILayout.Button("Show Cylinder Preview"))
					{
						Rect srcRectLeft = FullRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.LeftRight)
							srcRectLeft = LeftRightRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.TopDown)
							srcRectLeft = TopDownRect;

						targetCompositionLayer.isPreviewingCylinder = true;
						Vector3[] cylinderVertices = CompositionLayer.MeshGenerationHelper.GenerateCylinderVertex(targetCompositionLayer.CylinderAngleOfArc, targetCompositionLayer.CylinderRadius, targetCompositionLayer.CylinderHeight);
						//Add components to Game Object
						targetCompositionLayer.generatedPreview = new GameObject();
						targetCompositionLayer.generatedPreview.hideFlags = HideFlags.HideAndDontSave;
						targetCompositionLayer.generatedPreview.name = CompositionLayer.CylinderPreviewName;
						targetCompositionLayer.generatedPreview.transform.SetParent(targetCompositionLayer.gameObject.transform);
						targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
						targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

						targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);

						MeshRenderer cylinderMeshRenderer = targetCompositionLayer.generatedPreview.AddComponent<MeshRenderer>();
						MeshFilter cylinderMeshFilter = targetCompositionLayer.generatedPreview.AddComponent<MeshFilter>();
						cylinderMeshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));

						if (targetCompositionLayer.texture != null)
						{
							cylinderMeshRenderer.sharedMaterial.mainTexture = targetCompositionLayer.texture;
							cylinderMeshRenderer.sharedMaterial.mainTextureOffset = srcRectLeft.position;
							cylinderMeshRenderer.sharedMaterial.mainTextureScale = srcRectLeft.size;
						}

						//Generate Mesh
						cylinderMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateCylinderMesh(targetCompositionLayer.CylinderAngleOfArc, cylinderVertices);
					}
				}

				EditorGUILayout.Space(10);
				serializedObject.ApplyModifiedProperties();
			}
			else if (Property_LayerShape.intValue == (int)CompositionLayer.LayerShape.Quad)
			{
				if (targetCompositionLayer.isPreviewingCylinder)
				{
					targetCompositionLayer.isPreviewingCylinder = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				if (targetCompositionLayer.isPreviewingEquirect)
				{
					targetCompositionLayer.isPreviewingEquirect = false;
					if (targetCompositionLayer.generatedPreview != null)
					{
						DestroyImmediate(targetCompositionLayer.generatedPreview);
					}
				}

				EditorGUI.indentLevel++;
				showLayerParams = EditorGUILayout.Foldout(showLayerParams, "Quad Parameters");
				if (showLayerParams)
				{
					EditorGUILayout.PropertyField(Property_QuadWidth, new GUIContent(Label_QuadWidth));
					EditorGUILayout.PropertyField(Property_QuadHeight, new GUIContent(Label_QuadHeight));
				}
				EditorGUI.indentLevel--;
				EditorGUILayout.HelpBox("Current Layer Aspect Ratio (Width : Height) = " + targetCompositionLayer.quadWidth + " : " + targetCompositionLayer.quadHeight, MessageType.Info);

				Vector3 CompositionLayerScale = targetCompositionLayer.gameObject.transform.localScale;
				bool QuadParamsChanged = targetCompositionLayer.LayerDimensionsChanged();
				if (targetCompositionLayer.isPreviewingQuad)
				{
					Transform generatedPreviewTransform = targetCompositionLayer.transform.Find(CompositionLayer.QuadPreviewName);
					
					if (generatedPreviewTransform != null)
					{
						targetCompositionLayer.generatedPreview = generatedPreviewTransform.gameObject;

						if (QuadParamsChanged)
						{
							//Generate vertices
							Vector3[] quadVertices = CompositionLayer.MeshGenerationHelper.GenerateQuadVertex(targetCompositionLayer.quadWidth, targetCompositionLayer.quadHeight);

							MeshFilter quadMeshFilter = targetCompositionLayer.generatedPreview.GetComponent<MeshFilter>();
							//Generate Mesh
							quadMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateQuadMesh(quadVertices);

							targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
							targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

							targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);
						}

						if (targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != targetCompositionLayer.texture)
						{
							targetCompositionLayer.generatedPreview.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = targetCompositionLayer.texture;
						}

						if (GUILayout.Button("Hide Quad Preview"))
						{
							targetCompositionLayer.isPreviewingQuad = false;
							if (targetCompositionLayer.generatedPreview != null)
							{
								DestroyImmediate(targetCompositionLayer.generatedPreview);
							}
						}
					}
					else
					{
						targetCompositionLayer.isPreviewingQuad = false;
					}
				}
				else
				{
					if (GUILayout.Button("Show Quad Preview"))
					{
						Rect srcRectLeft = FullRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.LeftRight)
							srcRectLeft = LeftRightRect;
						if (targetCompositionLayer.isCustomRects && targetCompositionLayer.customRects == CompositionLayer.CustomRectsType.TopDown)
							srcRectLeft = TopDownRect;

						targetCompositionLayer.isPreviewingQuad = true;
						//Generate vertices
						Vector3[] quadVertices = CompositionLayer.MeshGenerationHelper.GenerateQuadVertex(targetCompositionLayer.quadWidth, targetCompositionLayer.quadHeight);

						//Add components to Game Object
						targetCompositionLayer.generatedPreview = new GameObject();
						targetCompositionLayer.generatedPreview.hideFlags = HideFlags.HideAndDontSave;
						targetCompositionLayer.generatedPreview.name = CompositionLayer.QuadPreviewName;
						targetCompositionLayer.generatedPreview.transform.SetParent(targetCompositionLayer.gameObject.transform);
						targetCompositionLayer.generatedPreview.transform.localPosition = Vector3.zero;
						targetCompositionLayer.generatedPreview.transform.localRotation = Quaternion.identity;

						targetCompositionLayer.generatedPreview.transform.localScale = targetCompositionLayer.GetNormalizedLocalScale(targetCompositionLayer.transform, Vector3.one);

						MeshRenderer quadMeshRenderer = targetCompositionLayer.generatedPreview.AddComponent<MeshRenderer>();
						MeshFilter quadMeshFilter = targetCompositionLayer.generatedPreview.AddComponent<MeshFilter>();
						quadMeshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));

						if (targetCompositionLayer.texture != null)
						{
							quadMeshRenderer.sharedMaterial.mainTexture = targetCompositionLayer.texture;
							quadMeshRenderer.sharedMaterial.mainTextureOffset = srcRectLeft.position;
							quadMeshRenderer.sharedMaterial.mainTextureScale = srcRectLeft.size;
						}
						//Generate Mesh
						quadMeshFilter.mesh = CompositionLayer.MeshGenerationHelper.GenerateQuadMesh(quadVertices);
					}
				}
			}

			//Rect UI For textures
			Rect labelRect = EditorGUILayout.GetControlRect();

			EditorGUI.LabelField(new Rect(labelRect.x, labelRect.y, labelRect.width / 2, labelRect.height), new GUIContent("Left Texture", "Texture used for the left eye"));
			EditorGUI.LabelField(new Rect(labelRect.x + labelRect.width / 2, labelRect.y, labelRect.width / 2, labelRect.height), new GUIContent("Right Texture", "Texture used for the right eye"));

			Rect textureRect = EditorGUILayout.GetControlRect(GUILayout.Height(64));

			targetCompositionLayer.texture = (Texture)EditorGUI.ObjectField(new Rect(textureRect.x, textureRect.y, 64, textureRect.height), targetCompositionLayer.texture, typeof(Texture), true);
			targetCompositionLayer.textureRight = (Texture)EditorGUI.ObjectField(new Rect(textureRect.x + textureRect.width / 2, textureRect.y, 64, textureRect.height), targetCompositionLayer.textureRight, typeof(Texture), true);
			if (null == targetCompositionLayer.textureLeft)
			{
			    targetCompositionLayer.texture = targetCompositionLayer.textureRight;
				//myScript.textures[1] = right;
			}

			EditorGUILayout.PropertyField(Property_LayerVisibility, new GUIContent(Label_LayerVisibility));
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.PropertyField(Property_IsDynamicLayer, Label_IsDynamicLayer);
			serializedObject.ApplyModifiedProperties();

			//EditorGUILayout.PropertyField(Property_isExternalSurface, Label_isExternalSurface);
			//serializedObject.ApplyModifiedProperties();

			//if (targetCompositionLayer.isExternalSurface)
			/*if (false)
			{
				EditorGUI.indentLevel++;
				showExternalSurfaceParams = EditorGUILayout.Foldout(showExternalSurfaceParams, "External Surface Parameters");
				if (showExternalSurfaceParams)
				{
					EditorGUILayout.PropertyField(Property_ExternalSurfaceWidth, Label_ExternalSurfaceWidth);
					EditorGUILayout.PropertyField(Property_ExternalSurfaceHeight, Label_ExternalSurfaceHeight);
					EditorGUILayout.PropertyField(Property_IsProtectedSurface, Label_IsProtectedSurface);

					serializedObject.ApplyModifiedProperties();
				}
				EditorGUI.indentLevel--;
			}*/

			if((Property_LayerShape.intValue != (int)CompositionLayer.LayerShape.Equirect && Property_LayerShape.intValue != (int)CompositionLayer.LayerShape.Equirect2) && (targetCompositionLayer.textureLeft == targetCompositionLayer.textureRight || targetCompositionLayer.textureRight == null))
			{
			    EditorGUILayout.PropertyField(Property_IsCustomRects, Label_IsCustomRects);
			    serializedObject.ApplyModifiedProperties();
			}

			if (targetCompositionLayer.isCustomRects)
			{
			    EditorGUILayout.PropertyField(Property_CustomRects, new GUIContent(Label_CustomRects));
			    serializedObject.ApplyModifiedProperties();
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(Property_ApplyColorScaleBias, Label_ApplyColorScaleBias);
			serializedObject.ApplyModifiedProperties();

			if (targetCompositionLayer.applyColorScaleBias)
			{
				if(!FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, ViveCompositionLayerColorScaleBias.featureId).enabled)
				{
					EditorGUILayout.HelpBox("The Color Scale Bias feature is not enabled in OpenXR Settings.", MessageType.Warning);
				}

				EditorGUI.indentLevel++;
				if (targetCompositionLayer.layerType == CompositionLayer.LayerType.Underlay)
				{
					EditorGUILayout.PropertyField(Property_SolidEffect, Label_SolidEffect);
					serializedObject.ApplyModifiedProperties();
				}
				showColorScaleBiasParams = EditorGUILayout.Foldout(showColorScaleBiasParams, "Color Scale Bias Parameters");
				if (showColorScaleBiasParams)
				{
					
					EditorGUILayout.PropertyField(Property_ColorScale, Label_ColorScale);
					EditorGUILayout.PropertyField(Property_ColorBias, Label_ColorBias);
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUI.indentLevel--;
			}

			ViveCompositionLayer compositionLayerFeature = (ViveCompositionLayer)FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, ViveCompositionLayer.featureId);

			if (compositionLayerFeature != null && compositionLayerFeature.enableAutoFallback)
			{
				EditorGUILayout.PropertyField(Property_RenderPriority, new GUIContent(Label_RenderPriority));
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUILayout.PropertyField(Property_TrackingOrigin, Label_TrackingOrigin);
			serializedObject.ApplyModifiedProperties();
		}

		public static bool RadiusValidityCheck(float inArcLength, ref float inRadius, float thetaLowerLimit, float thetaUpperLimit, CompositionLayer.CylinderLayerParamLockMode lockMode)
		{
			bool isValid = true;

			if (inRadius <= 0)
			{
				inRadius = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, thetaUpperLimit);
				isValid = false;
				return isValid;
			}

			float degThetaResult = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(inArcLength, inRadius);

			if (degThetaResult < thetaLowerLimit)
			{
				if (lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcAngle) //Angle locked, increase arc length
				{
					ArcLengthValidityCheck(ref inArcLength, inRadius, thetaLowerLimit, thetaUpperLimit);
					inRadius = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, thetaLowerLimit);
				}
				else if (lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcLength) //ArcLength Locked, keep angle at min
				{
					inRadius = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, thetaLowerLimit);
				}
				isValid = false;
			}
			else if (degThetaResult > thetaUpperLimit)
			{
				if (lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcAngle) //Angle locked, decrease arc length
				{
					ArcLengthValidityCheck(ref inArcLength, inRadius, thetaLowerLimit, thetaUpperLimit);
					inRadius = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, thetaUpperLimit);
				}
				else if (lockMode == CompositionLayer.CylinderLayerParamLockMode.ArcLength) //ArcLength Locked, keep angle at max
				{
					inRadius = CompositionLayer.CylinderParameterHelper.ArcLengthAndDegAngleOfArcToRadius(inArcLength, thetaUpperLimit);
				}
				isValid = false;
			}

			return isValid;
		}

		public static bool ArcLengthValidityCheck(ref float inArcLength, float inRadius, float thetaLowerLimit, float thetaUpperLimit)
		{
			bool isValid = true;

			if (inArcLength <= 0)
			{
				inArcLength = CompositionLayer.CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(thetaLowerLimit, inRadius);
				isValid = false;
				return isValid;
			}

			float degThetaResult = CompositionLayer.CylinderParameterHelper.RadiusAndArcLengthToDegAngleOfArc(inArcLength, inRadius);

			if (degThetaResult < thetaLowerLimit)
			{
				inArcLength = CompositionLayer.CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(thetaLowerLimit, inRadius);
				isValid = false;
			}
			else if (degThetaResult > thetaUpperLimit)
			{
				inArcLength = CompositionLayer.CylinderParameterHelper.RadiusAndDegAngleOfArcToArcLength(thetaUpperLimit, inRadius);
				isValid = false;
			}

			return isValid;
		}
	}
#endregion
}
#endif
