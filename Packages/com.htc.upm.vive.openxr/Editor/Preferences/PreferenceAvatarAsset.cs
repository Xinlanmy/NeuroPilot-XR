// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;

#if UNITY_EDITOR
namespace VIVE.OpenXR.Editor
{
	[Serializable]
	public class PreferenceAvatarAsset : ScriptableObject
	{
		public const string AssetPath = "Assets/VIVE/OpenXR/Preferences/PreferenceAvatarAsset.asset";

		// VRM constants
		public const string kVrm0Package = "UniVRM-0.109.0_7aff.unitypackage";
		public const string kVrm0Asset = "Assets/VRM.meta";
		public const string kVrm1Package = "VRM-0.109.0_7aff.unitypackage";
		public const string kVrm1Asset = "Assets/VRM10.meta";

		public bool SupportVrm0 = false;
		public bool SupportVrm1 = false;
	}
}
#endif
