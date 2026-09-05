// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace VIVE.OpenXR.UserPresence
{
	// -------------------- 12.39. XR_EXT_user_presence --------------------
	#region New Structures
	/// <summary>
	/// The application can use the XrSystemUserPresencePropertiesEXT event in xrGetSystemProperties to detect if the given system supports the sensing of user presence.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XrSystemUserPresencePropertiesEXT
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
		/// An <see cref="XrBool32"/> value that indicates whether the system supports user presence sensing.
		/// </summary>
		public XrBool32 supportsUserPresence;
	}

	/// <summary>
	/// The XrEventDataUserPresenceChangedEXT event is queued for retrieval using xrPollEvent when the user presence is changed, as well as when a session starts running.<br></br>
	/// Receiving XrEventDataUserPresenceChangedEXT with the isUserPresent is XR_TRUE indicates that the system has detected the presence of a user in the XR experience.For example, this may indicate that the user has put on the headset, or has entered the tracking area of a non-head-worn XR system.<br></br>
	/// Receiving XrEventDataUserPresenceChangedEXT with the isUserPresent is XR_FALSE indicates that the system has detected the absence of a user in the XR experience.For example, this may indicate that the user has removed the headset or has stepped away from the tracking area of a non-head-worn XR system.<br></br>
	/// The runtime must queue this event upon a successful call to the xrBeginSession function, regardless of the value of isUserPresent, so that the application can be in sync on the state when a session begins running.<br></br>
	/// The runtime must return a valid XrSession handle for a running session.<br></br>
	/// After the application calls xrEndSession, a running session is ended and the runtime must not enqueue any more user presence events.Therefore, the application will no longer observe any changes of the isUserPresent until another running session.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XrEventDataUserPresenceChangedEXT
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
		/// The <see cref="XrSession"/> that is receiving the notification.
		/// </summary>
		public XrSession session;
		/// <summary>
		/// An <see cref="XrBool32"/> value for new state of user presence after the change.
		/// </summary>
		public XrBool32 isUserPresent;

		/// <summary>
		/// The XR_EXT_user_presence extension must be enabled prior to using XrEventDataUserPresenceChangedEXT.
		/// </summary>
		public XrEventDataUserPresenceChangedEXT(XrStructureType in_type, IntPtr in_next, XrSession in_session, XrBool32 in_present)
		{
			type = in_type;
			next = in_next;
			session = in_session;
			isUserPresent = in_present;
		}
		/// <summary>
		/// Retrieves the identity value of XrEventDataUserPresenceChangedEXT.
		/// </summary>
		public static XrEventDataUserPresenceChangedEXT identity {
			get {
				return new XrEventDataUserPresenceChangedEXT(XrStructureType.XR_TYPE_EVENT_DATA_USER_PRESENCE_CHANGED_EXT, IntPtr.Zero, 0, true); // user is default present
			}
		}
		public static bool Get(XrEventDataBuffer eventDataBuffer, out XrEventDataUserPresenceChangedEXT eventDataUserPresenceChangedEXT)
		{
			eventDataUserPresenceChangedEXT = identity;

			if (eventDataBuffer.type == XrStructureType.XR_TYPE_EVENT_DATA_USER_PRESENCE_CHANGED_EXT)
			{
				eventDataUserPresenceChangedEXT.next = eventDataBuffer.next;
				eventDataUserPresenceChangedEXT.session = BitConverter.ToUInt64(eventDataBuffer.varying, 0);
				eventDataUserPresenceChangedEXT.isUserPresent = BitConverter.ToUInt32(eventDataBuffer.varying, 8);
				return true;
			}

			return false;
		}
	}
	#endregion
}
