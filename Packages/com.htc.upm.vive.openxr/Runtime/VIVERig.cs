// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace VIVE.OpenXR
{
	[DisallowMultipleComponent]
	public sealed class VIVERig : MonoBehaviour
	{
		private static VIVERig m_Instance;
		public static VIVERig Instance => m_Instance;

		#region Log
		const string LOG_TAG = "VIVE.OpenXR.VIVERig";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		#region Inspector
		[SerializeField]
		private GameObject m_CameraOffset = null;
		public GameObject CameraOffset { get { return m_CameraOffset; } set { m_CameraOffset = value; } }

		private TrackingOriginModeFlags m_TrackingOriginEx = TrackingOriginModeFlags.Device;
		[SerializeField]
		private TrackingOriginModeFlags m_TrackingOrigin = TrackingOriginModeFlags.Device;
		public TrackingOriginModeFlags TrackingOrigin { get { return m_TrackingOrigin; } set { m_TrackingOrigin = value; } }

		private Vector3 cameraPosOffset = Vector3.zero;
		[SerializeField]
		private float m_CameraHeight = 1.5f;
		public float CameraHeight { get { return m_CameraHeight; } set { m_CameraHeight = value; } }

#if ENABLE_INPUT_SYSTEM
		[SerializeField]
		private InputActionAsset m_ActionAsset;
		public InputActionAsset actionAsset { get => m_ActionAsset; set => m_ActionAsset = value; }
#endif
		#endregion

		private static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();
		private static readonly object lockObj = new object(); 
		private float m_LastRecenteredTime = 0.0f;

		#region MonoBehaviour
		private void OnEnable()
		{
			UpdateInputSubsystems();
			for (int i = 0; i < s_InputSubsystems.Count; i++)
			{
				s_InputSubsystems[i].trackingOriginUpdated += TrackingOriginUpdated;
			}

#if ENABLE_INPUT_SYSTEM
			if (m_ActionAsset != null)
			{
				m_ActionAsset.Enable();
			}
#endif
		}
		private void OnDisable()
		{
			UpdateInputSubsystems();
			for (int i = 0; i < s_InputSubsystems.Count; i++)
			{
				s_InputSubsystems[i].trackingOriginUpdated -= TrackingOriginUpdated;
			}
		}
		private void Awake()
		{
			if (m_Instance == null)
			{
				m_Instance = this;
			}
			else if (m_Instance != this)
			{
				Destroy(this);
			}
		}
		private void Update()
		{
			TrackingOriginModeFlags mode = GetTrackingOriginMode();
			if ((mode != m_TrackingOrigin || m_TrackingOriginEx != m_TrackingOrigin) &&
				m_TrackingOrigin != TrackingOriginModeFlags.Unknown &&
				SetTrackingOriginMode(m_TrackingOrigin))
			{
				mode = GetTrackingOriginMode();
				sb.Clear().Append("Update() Tracking mode is set to " + mode);
				m_TrackingOriginEx = m_TrackingOrigin;
			}

			if (m_CameraOffset != null && m_TrackingOrigin == TrackingOriginModeFlags.Device)
			{
				cameraPosOffset.x = m_CameraOffset.transform.localPosition.x;
				cameraPosOffset.y = m_CameraHeight;
				cameraPosOffset.z = m_CameraOffset.transform.localPosition.z;

				m_CameraOffset.transform.localPosition = cameraPosOffset;
			}
		}
		#endregion

		private bool SetTrackingOriginMode(TrackingOriginModeFlags value)
		{
			lock (lockObj)
			{
				UpdateInputSubsystems();

				for (int i = 0; i < s_InputSubsystems.Count; i++)
				{
					var subsys = s_InputSubsystems[i]; 
					if (!subsys.running)
					{
						continue;
					}

					if (subsys.TrySetTrackingOriginMode(value))
					{
						return true;
					}
					Debug.LogWarning($"Failed to set TrackingOriginModeFlags({value}) to XRInputSubsystem: {subsys.subsystemDescriptor?.id ?? "Unknown"}");
				}
				return false;
			}
		}

		private TrackingOriginModeFlags GetTrackingOriginMode()
		{
			lock (lockObj)
			{
				UpdateInputSubsystems();

				for(int i=0; i< s_InputSubsystems.Count; i++)
				{
					var subsys = s_InputSubsystems[i];
					if (!subsys.running)
					{
						continue;
					}
					return subsys.GetTrackingOriginMode();
				}
				return TrackingOriginModeFlags.Unknown;
			}
		}

		private void UpdateInputSubsystems()
		{
			s_InputSubsystems.Clear();

#if UNITY_6000_0_OR_NEWER
			SubsystemManager.GetSubsystems(s_InputSubsystems);
#else
			SubsystemManager.GetInstances(s_InputSubsystems);
#endif
		}

		private void TrackingOriginUpdated(XRInputSubsystem obj)
		{
			m_LastRecenteredTime = Time.time;
			sb.Clear().Append("TrackingOriginUpdated() m_LastRecenteredTime: ").Append(m_LastRecenteredTime); DEBUG(sb);
		}
	}
}
