// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace VIVE.OpenXR.FrameSynchronization
{
	/// <summary>
	/// The enum alias of <see cref="XrFrameSynchronizationModeHTC"/>.
	/// </summary>
	public enum SynchronizationModeHTC : UInt32
	{
		Stablized = XrFrameSynchronizationModeHTC.XR_FRAME_SYNCHRONIZATION_MODE_STABILIZED_HTC,
		Prompt = XrFrameSynchronizationModeHTC.XR_FRAME_SYNCHRONIZATION_MODE_PROMPT_HTC,
		//Adaptive = XrFrameSynchronizationModeHTC.XR_FRAME_SYNCHRONIZATION_MODE_ADAPTIVE_HTC,
	}

	// -------------------- 12.1. XR_HTC_frame_synchronization --------------------
	#region New Enums
	public enum XrFrameSynchronizationModeHTC : UInt32
	{
		XR_FRAME_SYNCHRONIZATION_MODE_STABILIZED_HTC = 1,
		XR_FRAME_SYNCHRONIZATION_MODE_PROMPT_HTC = 2,
		XR_FRAME_SYNCHRONIZATION_MODE_ADAPTIVE_HTC = 3,
		XR_FRAME_SYNCHRONIZATION_MODE_MAX_ENUM_HTC = 0x7FFFFFFF
	}
	#endregion

	#region New Structures
	/// <summary>
	/// Traditional, runtime will use the latest frame which will cost jitter. With Frame Synchronization, the render frame will not be discarded for smooth gameplay experience.
	/// However, if the GPU cannot consistently finish rendering on time(rendering more than one vsync at a time), jitter will still occur.Therefore, reducing GPU load is key to smooth gameplay.
	/// The application can use Frame Synchronization by passing XrFrameSynchronizationSessionBeginInfoHTC at next of <see cref="XrSessionBeginInfo"/>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XrFrameSynchronizationSessionBeginInfoHTC
	{
		/// <summary>
		/// The XrStructureType of this structure. It must be XR_TYPE_FRAME_SYNCHRONIZATION_SESSION_BEGIN_INFO_HTC.
		/// </summary>
		public XrStructureType type;
		/// <summary>
		/// NULL or a pointer to the next structure in a structure chain. No such structures are defined in core OpenXR or this extension.
		/// </summary>
		public IntPtr next;
		/// <summary>
		/// The frame synchronization mode to be used in this session.
		/// </summary>
		public XrFrameSynchronizationModeHTC mode;

		public XrFrameSynchronizationSessionBeginInfoHTC(XrStructureType in_type, IntPtr in_next, XrFrameSynchronizationModeHTC in_mode)
		{
			type = in_type;
			next = in_next;
			mode = in_mode;
		}
		public static XrFrameSynchronizationSessionBeginInfoHTC identity {
			get {
				return new XrFrameSynchronizationSessionBeginInfoHTC(
					XrStructureType.XR_TYPE_FRAME_SYNCHRONIZATION_SESSION_BEGIN_INFO_HTC,
					IntPtr.Zero,
					XrFrameSynchronizationModeHTC.XR_FRAME_SYNCHRONIZATION_MODE_STABILIZED_HTC);
			}
		}
	}
	#endregion
}