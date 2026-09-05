// Copyright HTC Corporation All Rights Reserved.

using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR;

using VIVE.OpenXR.UserPresence;

namespace VIVE.OpenXR
{
	public class XR_EXT_user_presence_impls : XR_EXT_user_presence_defs
	{
		#region Log
		const string LOG_TAG = "VIVE.OpenXR.XR_EXT_user_presence_impls";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		public XR_EXT_user_presence_impls() { sb.Clear().Append("XR_EXT_user_presence_impls()"); DEBUG(sb); }

		private ViveUserPresence feature = null;
		private bool ASSERT_FEATURE(bool init = false)
		{
			if (feature == null) { feature = OpenXRSettings.Instance.GetFeature<ViveUserPresence>(); }
			bool enabled = (feature != null);
			if (init)
			{
				sb.Clear().Append("ViveUserPresence is ").Append((enabled ? "enabled." : "disabled."));
				DEBUG(sb);
			}
			return enabled;
		}

		public override bool IsUserPresent()
		{
			if (ASSERT_FEATURE()) { return feature.IsUserPresent(); }
			return true;
		}
	}
}