using System.Collections;
using UnityEngine;

namespace VIVE.OpenXR.Toolkits.RealisticHandInteraction
{
	public class RealHandPose : HandPose
	{
		[SerializeField]
		private Handedness m_Handedness;
		public bool isLeft => m_Handedness == Handedness.Left;
		private bool keepUpdate = false;

		protected override void OnEnable()
		{
			StartCoroutine(WaitInit());
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			keepUpdate = false;
		}

		public override void SetType(HandPoseType poseType)
		{
			if (poseType == HandPoseType.HAND_LEFT)
			{
				m_Handedness = Handedness.Left;
			}
			else if (poseType == HandPoseType.HAND_RIGHT)
			{
				m_Handedness = Handedness.Right;
			}

			base.SetType(poseType);
		}

		private IEnumerator WaitInit()
		{
			yield return new WaitUntil(() => m_Initialized);
			base.OnEnable();
			keepUpdate = true;
		}

		private void Update()
		{
			if (!keepUpdate) { return; }
			HandData handData = CachedHand.Get(isLeft);
			m_IsTracked = handData.isTracked;
			if (!m_IsTracked) { return; }

			Vector3 position = Vector3.zero;
			Quaternion rotation = Quaternion.identity;
			for (int i = 0; i < poseCount; i++)
			{
				if (handData.GetJointPosition((JointType)i, ref position) && handData.GetJointRotation((JointType)i, ref rotation))
				{
					m_Position[i] = transform.position + transform.rotation * position;
					m_Rotation[i] = transform.rotation * rotation;
					m_LocalPosition[i] = position;
					m_LocalRotation[i] = rotation;
				}
				else
				{
					m_Position[i] = Vector3.zero;
					m_Rotation[i] = Quaternion.identity;
					m_LocalPosition[i] = Vector3.zero;
					m_LocalRotation[i] = Quaternion.identity;
				}
			}
		}
	}
}
