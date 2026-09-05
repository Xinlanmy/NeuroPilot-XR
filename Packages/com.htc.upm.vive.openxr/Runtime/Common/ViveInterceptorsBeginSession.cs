// Copyright HTC Corporation All Rights Reserved.

#define DEBUG

using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using VIVE.OpenXR.FrameSynchronization;

namespace VIVE.OpenXR
{
	partial class ViveInterceptors
	{
		[HookHandler("xrBeginSession")]
		private static XrResult OnHookXrBeginSession(XrInstance instance, string name, out IntPtr function)
        {
            if (xrBeginSessionOrigin == null)
            {
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret != XrResult.XR_SUCCESS)
                    return ret;
                xrBeginSessionOrigin = Marshal.GetDelegateForFunctionPointer<xrBeginSessionDelegate>(function);
            }
            function = xrBeginSessionPtr;
            return XrResult.XR_SUCCESS;
        }


		#region xrBeginSession
		public delegate XrResult xrBeginSessionDelegate(XrSession session, ref XrSessionBeginInfo beginInfo);
		private static xrBeginSessionDelegate xrBeginSessionOrigin = null;

		[MonoPInvokeCallback(typeof(xrBeginSessionDelegate))]
		private static XrResult xrBeginSessionInterceptor(XrSession session, ref XrSessionBeginInfo beginInfo)
		{
			Profiler.BeginSample("VI:BeginSession");
			XrResult result = XrResult.XR_ERROR_FUNCTION_UNSUPPORTED;

			if (xrBeginSessionOrigin != null)
			{
				if (m_EnableFrameSynchronization)
				{
					frameSynchronizationSessionBeginInfo.mode = m_FrameSynchronizationMode;
                    frameSynchronizationSessionBeginInfo.next = beginInfo.next;
                    beginInfo.next = Marshal.AllocHGlobal(Marshal.SizeOf(frameSynchronizationSessionBeginInfo));

					long offset = 0;
					if (IntPtr.Size == 4)
						offset = beginInfo.next.ToInt32();
					else
						offset = beginInfo.next.ToInt64();

					IntPtr frame_synchronization_session_begin_info_ptr = new IntPtr(offset);
					Marshal.StructureToPtr(frameSynchronizationSessionBeginInfo, frame_synchronization_session_begin_info_ptr, false);

#if DEBUG
					if (IntPtr.Size == 4)
						offset = beginInfo.next.ToInt32();
					else
						offset = beginInfo.next.ToInt64();

					IntPtr fs_begin_info_ptr = new IntPtr(offset);
					XrFrameSynchronizationSessionBeginInfoHTC fsBeginInfo = (XrFrameSynchronizationSessionBeginInfoHTC)Marshal.PtrToStructure(fs_begin_info_ptr, typeof(XrFrameSynchronizationSessionBeginInfoHTC));

					sb.Clear().Append("xrBeginSessionInterceptor() beginInfo.next = (").Append(fsBeginInfo.type).Append(", ").Append(fsBeginInfo.mode).Append(")");
					Log.D(sb);
#endif
				}

				result = xrBeginSessionOrigin(session, ref beginInfo);
			}
			else
			{
				Log.E("xrBeginSessionInterceptor() Not assign xrBeginSession!");
			}
			Profiler.EndSample();

			return result;
		}

		private static readonly xrBeginSessionDelegate xrBeginSession = new xrBeginSessionDelegate(xrBeginSessionInterceptor);
		private static readonly IntPtr xrBeginSessionPtr = Marshal.GetFunctionPointerForDelegate(xrBeginSession);
		#endregion

		private static XrFrameSynchronizationSessionBeginInfoHTC frameSynchronizationSessionBeginInfo = XrFrameSynchronizationSessionBeginInfoHTC.identity;
		private static bool m_EnableFrameSynchronization = false;
		private static XrFrameSynchronizationModeHTC m_FrameSynchronizationMode = XrFrameSynchronizationModeHTC.XR_FRAME_SYNCHRONIZATION_MODE_STABILIZED_HTC;
		/// <summary>
		/// Activate or deactivate the Frame Synchronization feature.
		/// </summary>
		/// <param name="active">True for activate</param>
		/// <param name="mode">The <see cref="XrFrameSynchronizationModeHTC"/> used for Frame Synchronization.</param>
		public void ActivateFrameSynchronization(bool active, XrFrameSynchronizationModeHTC mode)
		{
			m_EnableFrameSynchronization = active;
			m_FrameSynchronizationMode = mode;
			sb.Clear().Append("ActivateFrameSynchronization() ").Append(active ? "enable " : "disable ").Append(mode);
			Log.D(sb);
		}
	}
}
