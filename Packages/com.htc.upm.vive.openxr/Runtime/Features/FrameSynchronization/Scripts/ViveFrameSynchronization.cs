// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.FrameSynchronization
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Frame Synchronization (Beta)",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		Company = "HTC",
		Desc = "Support the Frame Synchronization extension.",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenxrExtensionString,
		Version = "1.0.0",
		FeatureId = featureId)]
#endif
	public class ViveFrameSynchronization : OpenXRFeature
	{
		#region Log
		const string LOG_TAG = "VIVE.OpenXR.FrameSynchronization.ViveFrameSynchronization";
		StringBuilder m_sb = null;
		StringBuilder sb  {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		void WARNING(StringBuilder msg) { Debug.LogWarningFormat("{0} {1}", LOG_TAG, msg); }
		void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		/// <summary>
		/// The extension name of 12.1. XR_HTC_frame_synchronization.
		/// </summary>
		public const string kOpenxrExtensionString = "XR_HTC_frame_synchronization";
		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.framesynchronization";

		#region OpenXR Life Cycle
		/// <inheritdoc />
		protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		{
			sb.Clear().Append("HookGetInstanceProcAddr() xrBeginSession"); DEBUG(sb);

			ViveInterceptors.Instance.AddRequiredFunction("xrBeginSession");
			return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
		}

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
			if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
			{
				sb.Clear().Append("OnInstanceCreate() ").Append(kOpenxrExtensionString).Append(" is NOT enabled."); WARNING(sb);
				return false;
			}

			m_XrInstance = xrInstance;
			m_XrInstanceCreated = true;
			sb.Clear().Append("OnInstanceCreate() ").Append(m_XrInstance); DEBUG(sb);

			ActivateFrameSynchronization(true);

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

				ActivateFrameSynchronization(false);
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
		internal SynchronizationModeHTC m_SynchronizationMode = SynchronizationModeHTC.Stablized;
		/// <summary>
		/// Activate or deactivate the Frame Synchronization feature.
		/// </summary>
		/// <param name="active">True for activate</param>
		/// <param name="mode">The <see cref="XrFrameSynchronizationModeHTC"/> used for Frame Synchronization.</param>
		private void ActivateFrameSynchronization(bool active)
		{
			sb.Clear().Append("ActivateFrameSynchronization() ").Append(active ? "enable " : "disable ").Append(m_SynchronizationMode); DEBUG(sb);
			ViveInterceptors.Instance.ActivateFrameSynchronization(active, (XrFrameSynchronizationModeHTC)m_SynchronizationMode);
		}

		/// <summary>
		/// Retrieves current frame synchronization mode.
		/// </summary>
		/// <returns>The mode of <see cref="SynchronizationModeHTC"/>.</returns>
		public SynchronizationModeHTC GetSynchronizationMode() { return m_SynchronizationMode; }
	}
}