using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VIVE.OpenXR.Toolkits.RealisticHandInteraction
{
	public class PhysicsInteractable : MonoBehaviour
	{
		[SerializeField]
		private float forceMultiplier = 1.0f;

		private const int MIN_POSE_SAMPLES = 2;
		private const int MAX_POSE_SAMPLES = 10;
		private readonly float MIN_VELOCITY = 0.5f;

		private Rigidbody interactableRigidbody;
		private Pose[] movementPoses = new Pose[MAX_POSE_SAMPLES];
		private float[] timestamps = new float[MAX_POSE_SAMPLES];
		private int currentPoseIndex = 0;
		private int poseCount = 0;
		private bool isBegin = false;
		private bool isEnd = false;
		private object lockVel = new object();

		private void Update()
		{
			if (interactableRigidbody == null) { return; }

			if (isBegin)
			{
				RecordMovement();
			}
		}

		private void FixedUpdate()
		{
			if (interactableRigidbody == null) { return; }

			if (isEnd)
			{
#if UNITY_6000_0_OR_NEWER
				interactableRigidbody.linearVelocity = Vector3.zero;
#else
				interactableRigidbody.velocity = Vector3.zero;
#endif
				interactableRigidbody.angularVelocity = Vector3.zero;

				Vector3 velocity = CalculateVelocity();
				if (velocity.magnitude > MIN_VELOCITY)
				{
					interactableRigidbody.AddForce(velocity * forceMultiplier, ForceMode.Impulse);
				}
				interactableRigidbody = null;

				Array.Clear(movementPoses, 0, MAX_POSE_SAMPLES);
				Array.Clear(timestamps, 0, MAX_POSE_SAMPLES);
				currentPoseIndex = 0;
				poseCount = 0;
				isEnd = false;
			}
		}

		private void RecordMovement()
		{
			float time = Time.time;

			int lastIndex = (currentPoseIndex + poseCount - 1) % MAX_POSE_SAMPLES;
			if (poseCount == 0 || timestamps[lastIndex] != time)
			{
				movementPoses[currentPoseIndex] = new Pose(interactableRigidbody.position, interactableRigidbody.rotation);
				timestamps[currentPoseIndex] = time;

				if (poseCount < MAX_POSE_SAMPLES)
				{
					poseCount++;
				}
				currentPoseIndex = (currentPoseIndex + 1) % MAX_POSE_SAMPLES;
			}
		}

		private Vector3 CalculateVelocity()
		{
			if (poseCount >= MIN_POSE_SAMPLES)
			{
				List<Vector3> velocities = new List<Vector3>();
				for (int i = 0; i < poseCount - 1; i++)
				{
					for (int j = i + 1; j < poseCount; j++)
					{
						velocities.Add(GetVelocity(i, j));
					}
				}
				Vector3 finalVelocity = FindBestVelocity(velocities);
				return finalVelocity;
			}
			return Vector3.zero;
		}

		private Vector3 GetVelocity(int idx1, int idx2)
		{
			if (idx1 < 0 || idx1 >= poseCount
				|| idx2 < 0 || idx2 >= poseCount
				|| poseCount < MIN_POSE_SAMPLES)
			{
				return Vector3.zero;
			}

			if (idx2 < idx1)
			{
				(idx1, idx2) = (idx2, idx1);
			}

			Vector3 currentPos = movementPoses[idx2].position;
			Vector3 previousPos = movementPoses[idx1].position;
			float currentTime = timestamps[idx2];
			float previousTime = timestamps[idx1];
			float timeDelta = currentTime - previousTime;
			if (currentPos == null || previousPos == null || timeDelta == 0)
			{
				return Vector3.zero;
			}

			Vector3 velocity = (currentPos - previousPos) / timeDelta;
			return velocity;
		}

		private Vector3 FindBestVelocity(List<Vector3> velocities)
		{
			Vector3 bestVelocity = Vector3.zero;
			float bestScore = float.PositiveInfinity;

			Parallel.For(0, velocities.Count, i =>
			{
				float score = 0f;
				for (int j = 0; j < velocities.Count; j++)
				{
					if (i != j)
					{
						score += (velocities[i] - velocities[j]).magnitude;
					}
				}

				lock (lockVel)
				{
					if (score < bestScore)
					{
						bestVelocity = velocities[i];
						bestScore = score;
					}
				}
			});

			return bestVelocity;
		}

		public void OnBeginInteractabled(IGrabbable grabbable)
		{
			if (grabbable is HandGrabInteractable handGrabbable)
			{
				interactableRigidbody = handGrabbable.rigidbody;
			}
			isBegin = true;
		}

		public void OnEndInteractabled(IGrabbable grabbable)
		{
			isBegin = false;
			isEnd = true;
		}
	}
}
