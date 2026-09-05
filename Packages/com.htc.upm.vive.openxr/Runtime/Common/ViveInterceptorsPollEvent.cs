// Copyright HTC Corporation All Rights Reserved.

using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using VIVE.OpenXR.DisplayRefreshRate;
using VIVE.OpenXR.Passthrough;
using VIVE.OpenXR.UserPresence;

namespace VIVE.OpenXR
{
	partial class ViveInterceptors
	{
		[HookHandler("xrPollEvent")]
		private static XrResult OnHookXrPollEvent(XrInstance instance, string name, out IntPtr function)
        {
            if (xrPollEventOrigin == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                xrPollEventOrigin = Marshal.GetDelegateForFunctionPointer<xrPollEventDelegate>(function);
            }
            function = xrPollEventPtr;
            return XrResult.XR_SUCCESS;
        }

		#region xrPollEvent
		public delegate XrResult xrPollEventDelegate(XrInstance instance, ref XrEventDataBuffer eventData);
		private static xrPollEventDelegate xrPollEventOrigin = null;

		[MonoPInvokeCallback(typeof(xrPollEventDelegate))]
		private static XrResult xrPollEventInterceptor(XrInstance instance, ref XrEventDataBuffer eventData)
		{
			Profiler.BeginSample("VI:PollEvent");
			XrResult result = XrResult.XR_SUCCESS;

			if (xrPollEventOrigin != null)
			{
				result = xrPollEventOrigin(instance, ref eventData);

				if (result == XrResult.XR_SUCCESS)
				{
					sb.Clear().Append("xrPollEventInterceptor() xrPollEvent ").Append(eventData.type); Log.D("PollEvent", sb);
					switch(eventData.type)
					{
						case XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_CHANGED_HTC:
							if (XrEventDataPassthroughConfigurationImageRateChangedHTC.Get(eventData, out XrEventDataPassthroughConfigurationImageRateChangedHTC eventDataPassthroughConfigurationImageRate))
							{
								fromImageRate = eventDataPassthroughConfigurationImageRate.fromImageRate;
								toImageRate = eventDataPassthroughConfigurationImageRate.toImageRate;
								sb.Clear().Append("xrPollEventInterceptor() XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_CHANGED_HTC")
									.Append(", fromImageRate.srcImageRate: ").Append(fromImageRate.srcImageRate)
									.Append(", fromImageRatesrc.dstImageRate: ").Append(fromImageRate.dstImageRate)
									.Append(", toImageRate.srcImageRate: ").Append(toImageRate.srcImageRate)
								.Append(", toImageRate.dstImageRate: ").Append(toImageRate.dstImageRate);
								Log.D("PollEvent", sb.ToString());
								VivePassthroughImageRateChanged.Send(fromImageRate.srcImageRate, fromImageRate.dstImageRate, toImageRate.srcImageRate, toImageRate.dstImageRate);
							}
							break;
						case XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_CHANGED_HTC:
							if (XrEventDataPassthroughConfigurationImageQualityChangedHTC.Get(eventData, out XrEventDataPassthroughConfigurationImageQualityChangedHTC eventDataPassthroughConfigurationImageQuality))
							{
								fromImageQuality = eventDataPassthroughConfigurationImageQuality.fromImageQuality;
								toImageQuality = eventDataPassthroughConfigurationImageQuality.toImageQuality;
								sb.Clear().Append("xrPollEventInterceptor() XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_CHANGED_HTC")
									.Append(", fromImageQuality: ").Append(fromImageQuality.scale)
									.Append(", toImageQuality: ").Append(toImageQuality.scale);
								Log.D("PollEvent", sb);
								VivePassthroughImageQualityChanged.Send(fromImageQuality.scale, toImageQuality.scale);
							}
							break;
						case XrStructureType.XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB:
							if(XrEventDataDisplayRefreshRateChangedFB.Get(eventData, out XrEventDataDisplayRefreshRateChangedFB eventDataDisplayRefreshRate))
							{
								fromDisplayRefreshRate = eventDataDisplayRefreshRate.fromDisplayRefreshRate;
								toDisplayRefreshRate = eventDataDisplayRefreshRate.toDisplayRefreshRate;
								sb.Clear().Append("xrPollEventInterceptor() XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB")
									.Append(", fromDisplayRefreshRate: ").Append(fromDisplayRefreshRate)
									.Append(", toDisplayRefreshRate: ").Append(toDisplayRefreshRate);
								Log.D("PollEvent", sb);
								ViveDisplayRefreshRateChanged.Send(fromDisplayRefreshRate, toDisplayRefreshRate);
							}
							break;
						case XrStructureType.XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED:
							if (XrEventDataSessionStateChanged.Get(eventData, out XrEventDataSessionStateChanged eventDataSession))
							{
								switch(eventDataSession.state)
								{
									case XrSessionState.XR_SESSION_STATE_READY:
										isUserPresent = true;
										break;
									case XrSessionState.XR_SESSION_STATE_STOPPING:
										isUserPresent = false;
										break;
									default:
										break;
								}
								sb.Clear().Append("xrPollEventInterceptor() XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED")
									.Append(", session: ").Append(eventDataSession.session)
									.Append(", state: ").Append(eventDataSession.state)
									.Append(", isUserPresent: ").Append(isUserPresent);
								Log.D("PollEvent", sb);
							}
							break;
						case XrStructureType.XR_TYPE_EVENT_DATA_USER_PRESENCE_CHANGED_EXT:
							if (XrEventDataUserPresenceChangedEXT.Get(eventData, out XrEventDataUserPresenceChangedEXT eventDataUserPresence))
							{
								isUserPresent = eventDataUserPresence.isUserPresent;
								sb.Clear().Append("xrPollEventInterceptor() XR_TYPE_EVENT_DATA_USER_PRESENCE_CHANGED_EXT")
									.Append(", session: ").Append(eventDataUserPresence.session)
									.Append(", isUserPresent: ").Append(isUserPresent);
								Log.D("PollEvent", sb);
							}
							break;
						default:
							break;
					}
				}

				//sb.Clear().Append("xrPollEventInterceptor() xrPollEvent result: ").Append(result).Append(", isUserPresent: ").Append(isUserPresent); Log.d("PollEvent", sb);
			}
			Profiler.EndSample();

			return result;
		}

		private static readonly xrPollEventDelegate xrPollEvent = new xrPollEventDelegate(xrPollEventInterceptor);
		private static readonly IntPtr xrPollEventPtr = Marshal.GetFunctionPointerForDelegate(xrPollEvent);
		#endregion

		private static bool isUserPresent = true;
		public bool IsUserPresent() { return isUserPresent; }

		private static float fromDisplayRefreshRate, toDisplayRefreshRate;
		public float FromDisplayRefreshRate() { return fromDisplayRefreshRate; }
		public float ToDisplayRefreshRate() { return toDisplayRefreshRate; }

		private static XrPassthroughConfigurationImageRateHTC fromImageRate, toImageRate;
		private static XrPassthroughConfigurationImageQualityHTC fromImageQuality, toImageQuality;
	}
}
