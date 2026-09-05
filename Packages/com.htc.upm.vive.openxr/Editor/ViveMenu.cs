// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;

namespace VIVE.OpenXR.Editor
{
	public class ViveMenu : UnityEditor.Editor
	{
		private const string kMenuXR = "VIVE/XR/Convert Main Camera to ViveRig";

		[MenuItem(kMenuXR, priority = 101)]
		private static void ConvertToViveRig()
		{
			// 1. Removes default Camera
			Camera cam = FindObjectOfType<Camera>();
			if (cam != null && cam.transform.parent == null)
			{
				Debug.Log("ConvertToViveRig() remove " + cam.gameObject.name);
				DestroyImmediate(cam.gameObject);
			}

			// 2. Loads ViveRig
			if (GameObject.Find("ViveRig") == null && GameObject.Find("ViveRig(Clone)") == null)
			{
				GameObject prefab = Resources.Load<GameObject>("Prefabs/ViveRig");
				if (prefab != null)
				{
					Debug.Log("ConvertToViveRig() load " + prefab.name);
					GameObject inst = Instantiate(prefab, null);
					if (inst != null)
					{
						inst.name = "ViveRig";
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
					}
				}
			}
		}
	}
}
#endif