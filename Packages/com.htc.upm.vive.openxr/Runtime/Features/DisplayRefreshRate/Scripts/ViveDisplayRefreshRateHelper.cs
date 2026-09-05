// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VIVE.OpenXR.DisplayRefreshRate
{
	// -------------------- 12.52. XR_FB_display_refresh_rate --------------------
	#region New Structures
	/// <summary>
	/// On platforms which support dynamically adjusting the display refresh rate, application developers may request a specific display refresh rate in order to improve the overall user experience.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XrEventDataDisplayRefreshRateChangedFB
	{
		/// <summary>
		/// The <see cref="XrStructureType"/> of this structure.
		/// </summary>
		public XrStructureType type;
		/// <summary>
		/// NULL or a pointer to the next structure in a structure chain. No such structures are defined in core OpenXR or this extension.
		/// </summary>
		public IntPtr next;
		/// <summary>
		/// fromDisplayRefreshRate is the previous display refresh rate.
		/// </summary>
		public float fromDisplayRefreshRate;
		/// <summary>
		/// toDisplayRefreshRate is the new display refresh rate.
		/// </summary>
		public float toDisplayRefreshRate;

		/// <summary>
		/// The XR_FB_display_refresh_rate extension must be enabled prior to using XrEventDataDisplayRefreshRateChangedFB.
		/// </summary>
		public XrEventDataDisplayRefreshRateChangedFB(XrStructureType in_type, IntPtr in_next, float in_fromDisplayRefreshRate, float in_toDisplayRefreshRate)
		{
			type = in_type;
			next = in_next;
			fromDisplayRefreshRate = in_fromDisplayRefreshRate;
			toDisplayRefreshRate = in_toDisplayRefreshRate;
		}
		/// <summary>
		/// Retrieves the identity value of XrEventDataDisplayRefreshRateChangedFB.
		/// </summary>
		public static XrEventDataDisplayRefreshRateChangedFB identity
		{
			get
			{
				return new XrEventDataDisplayRefreshRateChangedFB(XrStructureType.XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB, IntPtr.Zero, 0.0f, 0.0f); // user is default present
			}
		}
		public static bool Get(XrEventDataBuffer eventDataBuffer, out XrEventDataDisplayRefreshRateChangedFB eventDataDisplayRefreshRateChangedFB)
		{
			eventDataDisplayRefreshRateChangedFB = identity;

			if (eventDataBuffer.type == XrStructureType.XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB)
			{
				eventDataDisplayRefreshRateChangedFB.next = eventDataBuffer.next;
				eventDataDisplayRefreshRateChangedFB.fromDisplayRefreshRate = BitConverter.ToSingle(eventDataBuffer.varying, 0);
				eventDataDisplayRefreshRateChangedFB.toDisplayRefreshRate = BitConverter.ToSingle(eventDataBuffer.varying, 4);
				return true;
			}

			return false;
		}
	}

	public static class ViveDisplayRefreshRateChanged
	{
		public delegate void OnDisplayRefreshRateChanged(float fromDisplayRefreshRate, float toDisplayRefreshRate);

		public static void Listen(OnDisplayRefreshRateChanged callback)
		{
			if (!allEventListeners.Contains(callback))
				allEventListeners.Add(callback);
		}
		public static void Remove(OnDisplayRefreshRateChanged callback)
		{
			if (allEventListeners.Contains(callback))
				allEventListeners.Remove(callback);
		}
		public static void Send(float fromDisplayRefreshRate, float toDisplayRefreshRate)
		{
			int N = 0;
			if (allEventListeners != null)
			{
				N = allEventListeners.Count;
				for (int i = N - 1; i >= 0; i--)
				{
					OnDisplayRefreshRateChanged single = allEventListeners[i];
					try
					{
						single(fromDisplayRefreshRate, toDisplayRefreshRate);
					}
					catch (Exception e)
					{
						Debug.Log("Event : " + e.ToString());
						allEventListeners.Remove(single);
						Debug.Log("Event : A listener is removed due to exception.");
					}
				}
			}
		}

		private static List<OnDisplayRefreshRateChanged> allEventListeners = new List<OnDisplayRefreshRateChanged>();
	}
	#endregion
}