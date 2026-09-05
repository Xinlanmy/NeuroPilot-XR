using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
namespace VIVE.OpenXR.Enterprise
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Enterprise Command",
		Desc = "Support Enterprise request with special command",
		Company = "HTC",
		OpenxrExtensionStrings = kOpenxrExtensionString,
		Version = "0.1",
		BuildTargetGroups = new[] { BuildTargetGroup.Android },
		FeatureId = featureId,
		Hidden = true
	)]
#endif
	public class ViveEnterpriseCommand : OpenXRFeature
	{
		#region Log
		const string LOG_TAG = "VIVE.OpenXR.Enterprise.Command ";
		private static void DEBUG(String msg) { Debug.Log(LOG_TAG + msg); }
		private static void ERROR(String msg) { Debug.LogError(LOG_TAG + msg); }
		#endregion

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.enterprise.command";

		/// <summary>
		/// The extension string.
		/// </summary>
		public const string kOpenxrExtensionString = "XR_HTC_enterprise_command";

		#region OpenXR Life Cycle
		private static bool m_XrInstanceCreated = false;
		private static bool m_XrSessionCreated = false;
		private static XrInstance m_XrInstance = 0;
		private static XrSession m_XrSession = 0;
		private static XrSystemId m_XrSystemId = 0;

		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateInstance">xrCreateInstance</see> is done.
		/// </summary>
		/// <param name="xrInstance">The created instance.</param>
		/// <returns>True for valid <see cref="XrInstance">XrInstance</see></returns>
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
			{
				ERROR($"OnInstanceCreate() {kOpenxrExtensionString}  is NOT enabled.");
				return false;
			}

			m_XrInstanceCreated = true;
			m_XrInstance = xrInstance;
			DEBUG($"OnInstanceCreate() {m_XrInstance}");

			return GetXrFunctionDelegates(m_XrInstance);
		}

		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroyInstance">xrDestroyInstance</see> is done.
		/// </summary>
		/// <param name="xrInstance">The instance to destroy.</param>
		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			if (m_XrInstance == xrInstance)
			{
				m_XrInstanceCreated = false;
				m_XrInstance = 0;
			}
			DEBUG($"OnInstanceDestroy() {xrInstance}");
		}

		/// <summary>
		/// Called when the <see cref="XrSystemId">XrSystemId</see> retrieved by <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrGetSystem">xrGetSystem</see> is changed.
		/// </summary>
		/// <param name="xrSystem">The system id.</param>
		protected override void OnSystemChange(ulong xrSystem)
		{
			m_XrSystemId = xrSystem;
			DEBUG($"OnSystemChange() {m_XrSystemId}");
		}

		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreateSession">xrCreateSession</see> is done.
		/// </summary>
		/// <param name="xrSession">The created session ID.</param>
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			DEBUG($"OnSessionCreate() {m_XrSession}");
		}

		/// <summary>
		/// Called when <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroySession">xrDestroySession</see> is done.
		/// </summary>
		/// <param name="xrSession">The session ID to destroy.</param>
		protected override void OnSessionDestroy(ulong xrSession)
		{
			DEBUG($"OnSessionDestroy() {xrSession}");

			if (m_XrSession == xrSession)
			{
				m_XrSession = 0;
				m_XrSessionCreated = false;
			}
		}
		#endregion

		#region OpenXR function delegates
		/// xrEnterpriseCommandHTC
		private static ViveEnterpriseCommandHelper.xrEnterpriseCommandHTCDelegate xrEnterpriseCommandHTC;
		/// xrGetInstanceProcAddr
		private static OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;

		/// <summary>
		/// Enterprise command request for special functionality.
		/// </summary>
		/// <param name="request">The request of enterprise command</param>
		/// <param name="result">The result of enterprise command</param>
		/// <returns>Return XR_SUCCESS if request successfully. False otherwise.</returns>
		private static XrResult EnterpriseCommandHTC(XrEnterpriseCommandBufferHTC request, ref XrEnterpriseCommandBufferHTC result)
		{
			if (!m_XrSessionCreated)
			{
				ERROR("EnterpriseCommandHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			if (!m_XrInstanceCreated)
			{
				ERROR("EnterpriseCommandHTC() XR_ERROR_INSTANCE_LOST.");
				return XrResult.XR_ERROR_INSTANCE_LOST;
			}

			DEBUG($"EnterpriseCommandHTC() code: {request.code}, data: {CharArrayToString(request.data)}");
			return xrEnterpriseCommandHTC(m_XrSession, request, ref result);
		}

		/// <summary>
		/// Get the OpenXR function via XrInstance.
		/// </summary>
		/// <param name="xrInstance">The XrInstance is provided by the Unity OpenXR Plugin.</param>
		/// <returns>Return true if request successfully. False otherwise.</returns>
		private bool GetXrFunctionDelegates(XrInstance xrInstance)
		{
			/// xrGetInstanceProcAddr
			if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
			{
				DEBUG("Get function pointer of xrGetInstanceProcAddr.");
				XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
					xrGetInstanceProcAddr,
					typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
			}
			else
			{
				ERROR("xrGetInstanceProcAddr");
				return false;
			}

			/// xrEnterpriseCommandHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrEnterpriseCommandHTC", out IntPtr funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrEnterpriseCommandHTC.");
					xrEnterpriseCommandHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(ViveEnterpriseCommandHelper.xrEnterpriseCommandHTCDelegate)) as ViveEnterpriseCommandHelper.xrEnterpriseCommandHTCDelegate;
				}
			}
			else
			{
				ERROR("xrEnterpriseCommandHTC");
				return false;
			}
			return true;
		}
		#endregion

		#region Public API
		private const int kCharLength = 256;
		private const char kEndChar = '\0';
		private static char[] charArray = new char[kCharLength];

		/// <summary>
		/// Request special feature with command, it should take code and command string.
		/// </summary>
		/// <param name="requestCode">The type of request code is integer.</param>
		/// <param name="requestCommand">The maximum length of request command is 256.</param>
		/// <param name="resultCode">The output of result code.</param>
		/// <param name="resultCommand">The output of result command.</param>
		/// <returns>Return true if request successfully. False otherwise.</returns>
		public static bool CommandRequest(int requestCode, string requestCommand, out int resultCode, out string resultCommand)
		{
			resultCode = 0;
			resultCommand = string.Empty;
			XrEnterpriseCommandBufferHTC request = new XrEnterpriseCommandBufferHTC(requestCode, StringToCharArray(requestCommand));
			XrEnterpriseCommandBufferHTC result = new XrEnterpriseCommandBufferHTC(resultCode, StringToCharArray(resultCommand));
			if (EnterpriseCommandHTC(request, ref result) == XrResult.XR_SUCCESS)
			{
				resultCode = result.code;
				resultCommand = CharArrayToString(result.data);
				DEBUG($"CommandRequest Result code: {resultCode}, data: {resultCommand}");
				return true;
			}
			return false;
		}
		#endregion

		private static char[] StringToCharArray(string str)
		{
			Array.Clear(charArray, 0, kCharLength);
			if (!string.IsNullOrEmpty(str))
			{
				int arrayLength = Math.Min(str.Length, kCharLength);
				for (int i = 0; i < arrayLength; i++)
				{
					charArray[i] = str[i];
				}
				charArray[kCharLength - 1] = kEndChar;
			}
			return charArray;
		}

		private static string CharArrayToString(char[] charArray)
		{
			int actualLength = Array.FindIndex(charArray, c => c == kEndChar);
			if (actualLength == -1)
			{
				actualLength = charArray.Length;
			}

			return new string(charArray, 0, actualLength);
		}
	}

	#region Helper
	public struct XrEnterpriseCommandBufferHTC
	{
		public Int32 code;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		public char[] data;

		public XrEnterpriseCommandBufferHTC(int in_code, char[] in_data)
		{
			code = (Int32)in_code;
			data = new char[in_data.Length];
			Array.Copy(in_data, data, in_data.Length);
		}
	}

	public class ViveEnterpriseCommandHelper
	{
		/// <summary>
		/// The function delegate of xrEnterpriseCommandHTC.
		/// </summary>
		/// <param name="session">An <see cref="XrSession">XrSession</see> in which the enterprise command will be active.</param>
		/// <param name="request">The request of enterprise command</param>
		/// <param name="result">The result of enterprise command</param>
		/// <returns>Return XR_SUCCESS if request successfully. False otherwise.</returns>
		public delegate XrResult xrEnterpriseCommandHTCDelegate(
			XrSession session,
			XrEnterpriseCommandBufferHTC request,
			ref XrEnterpriseCommandBufferHTC result);
	}
	#endregion
}
