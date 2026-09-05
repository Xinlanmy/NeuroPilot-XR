// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.UserPresence
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR User Presence",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		Company = "HTC",
		Desc = "Support the User Presence extension.",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenxrExtensionString,
		Version = "1.0.0",
		FeatureId = featureId)]
#endif
	public class ViveUserPresence : OpenXRFeature
	{
		#region Log
		const string LOG_TAG = "VIVE.OpenXR.UserPresence.ViveUserPresence";
		StringBuilder m_sb = null;
		StringBuilder sb {
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
		/// OpenXR specification <see href="https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_user_presence">12.39. XR_EXT_user_presence</see>.
		/// </summary>
		public const string kOpenxrExtensionString = "XR_EXT_user_presence";
        /// <summary>
        /// The feature id string. This is used to give the feature a well known id for reference.
        /// </summary>
        public const string featureId = "vive.openxr.feature.userpresence";

		#region OpenXR Life Cycle
		/// <inheritdoc />
		protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		{
			sb.Clear().Append("HookGetInstanceProcAddr() xrPollEvent"); DEBUG(sb);

			ViveInterceptors.Instance.AddRequiredFunction("xrPollEvent");
			return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
		}

		private bool m_XrInstanceCreated = false;
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

			return GetXrFunctionDelegates(m_XrInstance);
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

		private bool m_XrSessionCreated = false;
		private XrSession m_XrSession = 0;
        /// <summary>
        /// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateSession">xrCreateSession</see> is done.
        /// </summary>
        /// <param name="xrSession">The created session ID.</param>
        protected override void OnSessionCreate(ulong xrSession)
        {
            m_XrSession = xrSession;
            m_XrSessionCreated = true;
			CheckUserPresenceSupport();
			sb.Clear().Append("OnSessionCreate() ").Append(m_XrSession).Append(", support User Presence: ").Append(SupportedUserPresence()); DEBUG(sb);
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

		#region OpenXR function delegates
		OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;
		OpenXRHelper.xrGetSystemPropertiesDelegate xrGetSystemProperties;
		/// <summary>
		/// An application can call GetSystemProperties to retrieve information about the system such as vendor ID, system name, and graphics and tracking properties.
		/// </summary>
		/// <param name="properties">Points to an instance of the XrSystemProperties structure, that will be filled with returned information.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		private XrResult GetSystemProperties(ref XrSystemProperties properties)
		{
			if (!m_XrSessionCreated)
			{
				sb.Clear().Append("GetSystemProperties() XR_ERROR_SESSION_LOST."); ERROR(sb);
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			if (!m_XrInstanceCreated)
			{
				sb.Clear().Append("GetSystemProperties() XR_ERROR_INSTANCE_LOST."); ERROR(sb);
				return XrResult.XR_ERROR_INSTANCE_LOST;
			}

			return xrGetSystemProperties(m_XrInstance, m_XrSystemId, ref properties);
		}

        private bool GetXrFunctionDelegates(XrInstance xrInstance)
        {
            /// xrGetInstanceProcAddr
            if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
            {
                sb.Clear().Append("Get function pointer of xrGetInstanceProcAddr."); DEBUG(sb);
                XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
                    xrGetInstanceProcAddr,
                    typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
            }
            else
            {
                sb.Clear().Append("xrGetInstanceProcAddr"); ERROR(sb);
                return false;
            }

            IntPtr funcPtr = IntPtr.Zero;
            /// xrGetSystemProperties
            if (XrGetInstanceProcAddr(xrInstance, "xrGetSystemProperties", out funcPtr) == XrResult.XR_SUCCESS)
            {
                if (funcPtr != IntPtr.Zero)
                {
                    sb.Clear().Append("Get function pointer of xrGetSystemProperties."); DEBUG(sb);
                    xrGetSystemProperties = Marshal.GetDelegateForFunctionPointer(
                        funcPtr,
                        typeof(OpenXRHelper.xrGetSystemPropertiesDelegate)) as OpenXRHelper.xrGetSystemPropertiesDelegate;
                }
            }
            else
            {
                sb.Clear().Append("xrGetSystemProperties"); ERROR(sb);
                return false;
            }

            return true;
        }
		#endregion

		private bool m_SupportUserPresence = false;
		XrSystemProperties systemProperties;
		XrSystemUserPresencePropertiesEXT userPresenceProperties;
		private void CheckUserPresenceSupport()
		{
			m_SupportUserPresence = false;
			if (!m_XrSessionCreated)
			{
				sb.Clear().Append("CheckUserPresenceSupport() session is not created."); ERROR(sb);
				return;
			}

			userPresenceProperties.type = XrStructureType.XR_TYPE_SYSTEM_USER_PRESENCE_PROPERTIES_EXT;
			systemProperties.type = XrStructureType.XR_TYPE_SYSTEM_PROPERTIES;
			systemProperties.next = Marshal.AllocHGlobal(Marshal.SizeOf(userPresenceProperties));

			long offset = 0;
			if (IntPtr.Size == 4)
				offset = systemProperties.next.ToInt32();
			else
				offset = systemProperties.next.ToInt64();

			IntPtr userPresencePropertiesPtr = new IntPtr(offset);
			Marshal.StructureToPtr(userPresenceProperties, userPresencePropertiesPtr, false);

#pragma warning disable 0618
			if (GetSystemProperties(ref systemProperties) == XrResult.XR_SUCCESS)
#pragma warning restore 0618
			{
				if (IntPtr.Size == 4)
					offset = systemProperties.next.ToInt32();
				else
					offset = systemProperties.next.ToInt64();

				userPresencePropertiesPtr = new IntPtr(offset);
				userPresenceProperties = (XrSystemUserPresencePropertiesEXT)Marshal.PtrToStructure(userPresencePropertiesPtr, typeof(XrSystemUserPresencePropertiesEXT));

				sb.Clear().Append("CheckUserPresenceSupport() userPresenceProperties.supportsUserPresence: ").Append((UInt32)userPresenceProperties.supportsUserPresence); DEBUG(sb);
				m_SupportUserPresence = userPresenceProperties.supportsUserPresence > 0;
			}
			else
			{
				sb.Clear().Append("CheckUserPresenceSupport() GetSystemProperties failed."); ERROR(sb);
			}

			Marshal.FreeHGlobal(systemProperties.next);
		}
		public bool SupportedUserPresence() { return m_SupportUserPresence; }
		public bool IsUserPresent()
		{
			if (!SupportedUserPresence()) { return true; } // user is always present
			return ViveInterceptors.Instance.IsUserPresent();
		}
	}
}