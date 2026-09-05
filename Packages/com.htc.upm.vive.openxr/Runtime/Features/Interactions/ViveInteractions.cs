// Copyright HTC Corporation All Rights Reserved.

using UnityEditor;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using System.Text;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.Interaction
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR - Interaction Group",
		Category = "Interactions",
		BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
		Company = "HTC",
		Desc = "VIVE interaction profiles management.",
		OpenxrExtensionStrings = kOpenxrExtensionString,
		Version = "2.5.0",
		FeatureId = featureId)]
#endif
	public class ViveInteractions : OpenXRFeature
	{
        #region Log
        const string LOG_TAG = "VIVE.OpenXR.Interaction.ViveInteractions ";
        static StringBuilder m_sb = null;
        static StringBuilder sb
        {
            get
            {
                if (m_sb == null) { m_sb = new StringBuilder(); }
                return m_sb;
            }
        }
        static void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
        static void WARNING(StringBuilder msg) { Debug.LogWarningFormat("{0} {1}", LOG_TAG, msg); }
        static void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
        #endregion

        public const string kOpenxrExtensionString = "";

        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "vive.openxr.feature.interactions";

		#region OpenXR Life Cycle
#pragma warning disable
		private bool m_XrInstanceCreated = false;
#pragma warning enable
		private XrInstance m_XrInstance = 0;
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateInstance">xrCreateInstance</see> is done.
		/// </summary>
		/// <param name="xrInstance">The created instance.</param>
		/// <returns>True for valid <see cref="XrInstance">XrInstance</see></returns>
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			m_XrInstance = xrInstance;
			m_XrInstanceCreated = true;
			sb.Clear().Append("OnInstanceCreate() ").Append(m_XrInstance); DEBUG(sb);

			return true;
		}
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroyInstance">xrDestroyInstance</see> is done.
		/// </summary>
		/// <param name="xrInstance">The instance to destroy.</param>
		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			sb.Clear().Append("OnInstanceDestroy() ").Append(xrInstance).Append(", current: ").Append(m_XrInstance); DEBUG(sb);
			if (m_XrInstance == xrInstance)
			{
				m_XrInstanceCreated = false;
				m_XrInstance = 0;
			}
		}

#pragma warning disable
		private bool m_XrSessionCreated = false;
#pragma warning enable
		private XrSession m_XrSession = 0;
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateSession">xrCreateSession</see> is done.
		/// </summary>
		/// <param name="xrSession">The created session ID.</param>
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			sb.Clear().Append("OnSessionCreate() ").Append(m_XrSession); DEBUG(sb);
		}
		protected override void OnSessionEnd(ulong xrSession)
		{
			sb.Clear().Append("OnSessionEnd() ").Append(xrSession).Append(", current: ").Append(m_XrSession); DEBUG(sb);
		}
		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroySession">xrDestroySession</see> is done.
		/// </summary>
		/// <param name="xrSession">The session ID to destroy.</param>
		protected override void OnSessionDestroy(ulong xrSession)
		{
			sb.Clear().Append("OnSessionDestroy() ").Append(xrSession).Append(", current: ").Append(m_XrSession); DEBUG(sb);
			if (m_XrSession == xrSession)
			{
				m_XrSessionCreated = false;
				m_XrSession = 0;
			}
		}

		private XrSystemId m_XrSystemId = 0;
		/// <summary>
		/// Called when the <see cref="XrSystemId">XrSystemId</see> retrieved by <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrGetSystem">xrGetSystem</see> is changed.
		/// </summary>
		/// <param name="xrSystem">The system id.</param>
		protected override void OnSystemChange(ulong xrSystem)
		{
			m_XrSystemId = xrSystem;
			sb.Clear().Append("OnSystemChange() " + m_XrSystemId); DEBUG(sb);
		}
		#endregion

		[SerializeField]
		internal bool m_ViveHandInteraction = false;
		/// <summary>
		/// Checks if using <see href="https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_HTC_hand_interaction">XR_HTC_hand_interaction</see> or not.
		/// </summary>
		/// <returns>True for using.</returns>
		public bool UseViveHandInteraction() { return m_ViveHandInteraction; }
		[SerializeField]
		internal bool m_ViveWristTracker = false;
		/// <summary>
		/// Checks if using <see href="https://business.vive.com/eu/product/vive-wrist-tracker/">VIVE Wrist Tracker</see> or not.
		/// </summary>
		/// <returns>True for using.</returns>
		public bool UseViveWristTracker() { return m_ViveWristTracker; }
		[SerializeField]
		internal bool m_ViveXRTracker = false;
		/// <summary>
		/// Checks if using <see href="https://business.vive.com/eu/product/vive-ultimate-tracker/">VIVE Ultimate Tracker</see> or not.
		/// </summary>
		/// <returns>True for using.</returns>
		public bool UseViveXrTracker() { return m_ViveXRTracker; }
		[SerializeField]
		internal bool m_KHRHandInteraction = false;
		/// <summary>
		/// Checks if using <see href="https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_hand_interaction">XR_EXT_hand_interaction</see> or not.
		/// </summary>
		/// <returns>True for using.</returns>
		public bool UseKhrHandInteraction() { return m_KHRHandInteraction; }
	}
}