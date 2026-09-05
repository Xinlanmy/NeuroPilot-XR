#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using NeuroPilotXR.Navigation;

namespace NeuroPilotXR.Editor
{
    /// <summary>Play-mode smoke test using input events, not direct select/click invocation.</summary>
    public static class SpatialWindowInteractionVerification
    {
        private const string Layout = "NeuroPilotVerificationController";
        private static readonly List<InputDevice> Devices = new List<InputDevice>();
        private static readonly List<string> Results = new List<string>();
        private static IEnumerator<int> routine;
        private static int nextFrame;
        private static double nextTime;
        private static double deadline;
        private static Transform panel;
        private static Vector3 originalPosition;
        private static Quaternion originalRotation;
        private static string status = "Not run";

        public static string Status() => status;

        public static string Begin()
        {
            if (routine != null)
                return status;
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return status = "FAIL: Enter unpaused Play mode first.";

            Results.Clear();
            InputSystem.RegisterLayout(@"{
                ""name"":""NeuroPilotVerificationController"", ""extend"":""XRController"",
                ""controls"":[
                    {""name"":""triggerButton"",""layout"":""Button"",""format"":""FLT"",""usage"":""TriggerButton""},
                    {""name"":""trigger"",""layout"":""Axis"",""format"":""FLT"",""usage"":""Trigger""}
                ]}");
            routine = Verify();
            nextFrame = Time.frameCount + 1;
            nextTime = EditorApplication.timeSinceStartup;
            deadline = EditorApplication.timeSinceStartup + 120d;
            status = "RUNNING: real input action and XR ray interaction verification";
            EditorApplication.update += Tick;
            return status;
        }

        public static string Stop()
        {
            status = "STOPPED: " + string.Join("; ", Results);
            Cleanup();
            return status;
        }

        private static void Tick()
        {
            try
            {
                Require(EditorApplication.isPlaying, "Play mode ended before verification completed");
                Require(EditorApplication.timeSinceStartup < deadline, "Verification timed out");
                if (Time.frameCount < nextFrame || EditorApplication.timeSinceStartup < nextTime)
                    return;
                if (routine.MoveNext())
                {
                    nextFrame = Time.frameCount + Mathf.Max(1, routine.Current);
                    nextTime = EditorApplication.timeSinceStartup + routine.Current / 60d;
                    return;
                }
                status = "PASS: " + string.Join("; ", Results);
                Debug.Log("[SpatialWindowVerification] " + status);
                Cleanup();
            }
            catch (Exception exception)
            {
                status = "FAIL: " + exception.Message + " | Completed: " + string.Join("; ", Results);
                Debug.LogWarning("[SpatialWindowVerification] " + status);
                Cleanup();
            }
        }

        private static IEnumerator<int> Verify()
        {
            GameObject panelObject = GameObject.Find("GlassNavigationPanel");
            Require(panelObject != null, "Open NeuroPilotNavigation scene first");
            panel = panelObject.transform;
            originalPosition = panel.position;
            originalRotation = panel.rotation;
            XRGrabInteractable grab = panelObject.GetComponent<XRGrabInteractable>();
            Require(grab != null && grab.colliders.Count > 0, "Panel grab collider missing");
            NavigationController navigation = UnityEngine.Object.FindObjectOfType<NavigationController>();
            Require(navigation != null, "Navigation controller missing");
            navigation.ShowWelcome();
            yield return 35;

            foreach (string hand in new[] { "Right", "Left" })
            {
                ActionBasedController controller = UnityEngine.Object.FindObjectsOfType<ActionBasedController>(true)
                    .FirstOrDefault(item => item.name == hand + " Controller");
                Require(controller != null, hand + " hand controller missing");
                XRRayInteractor ray = controller.GetComponentsInChildren<XRRayInteractor>(true)
                    .FirstOrDefault(item => item.name == "Ray Interactor");
                Require(ray != null, hand + " hand ray missing");

                InputDevice device = InputSystem.AddDevice(Layout);
                Devices.Add(device);
                InputSystem.SetDeviceUsage(device, hand == "Right" ? CommonUsages.RightHand : CommonUsages.LeftHand);
                Vector3 target = grab.colliders[0].bounds.center;
                Vector3 position = Camera.main.transform.position + panel.forward * 0.20f;
                Quaternion rotation = Quaternion.LookRotation(target - position, Vector3.up);
                Pose(device, controller.transform.parent, position, rotation, false);
                yield return 20;
                Require(ray.interactablesHovered.Contains(grab), hand + " ray did not hover the drag strip");
                Results.Add(hand + " hover");

                Pose(device, controller.transform.parent, position, rotation, true);
                yield return 14;
                Require(grab.isSelected && ray.interactablesSelected.Contains(grab), hand + " trigger did not select panel");
                Vector3 before = panel.position;
                Vector3 movement = panel.right * 0.22f + Vector3.up * 0.10f + panel.forward * 0.18f;
                position += movement;
                Pose(device, controller.transform.parent, position, rotation, true);
                yield return 24;
                Vector3 actual = panel.position - before;
                Require(actual.magnitude > 0.15f && Vector3.Dot(actual, movement.normalized) > 0.15f,
                    hand + " selected panel did not follow 3D hand movement; delta=" + actual);
                Require(Mathf.Abs(Vector3.Dot(actual, panel.forward)) > 0.07f,
                    hand + " drag had no depth movement; delta=" + actual);
                Results.Add(hand + " trigger + 3D drag (" + actual.magnitude.ToString("F3") + " m)");

                Pose(device, controller.transform.parent, position, rotation, false);
                yield return 12;
                Require(!grab.isSelected, hand + " trigger release did not release panel");
                Vector3 released = panel.position;
                Pose(device, controller.transform.parent, position + Vector3.up * 0.20f, rotation, false);
                yield return 18;
                Require(Vector3.Distance(panel.position, released) < 0.005f, hand + " released panel drifted");
                Results.Add(hand + " release stays fixed");

                Button enter = panel.GetComponentsInChildren<Button>(true).First(item => item.name == "EnterTrainingButton");
                position = Camera.main.transform.position + panel.forward * 0.20f;
                rotation = Quaternion.LookRotation(enter.transform.position - position, Vector3.up);
                Pose(device, controller.transform.parent, position, rotation, false);
                yield return 18;
                UnityEngine.EventSystems.RaycastResult uiHit;
                Require(ray.TryGetCurrentUIRaycastResult(out uiHit) && uiHit.gameObject == enter.gameObject,
                    hand + " ray did not hit Enter button; UI hit=" + (uiHit.gameObject != null ? uiHit.gameObject.name : "none"));
                Pose(device, controller.transform.parent, position, rotation, true);
                yield return 8;
                Require(!grab.isSelected, hand + " normal button press incorrectly grabbed panel");
                Pose(device, controller.transform.parent, position, rotation, false);
                yield return 35;
                Transform difficulty = panel.Find("DifficultyPage");
                Require(difficulty != null && difficulty.gameObject.activeSelf && difficulty.GetComponent<CanvasGroup>().alpha > 0.99f,
                    hand + " trigger click did not open difficulty page; active=" + (difficulty != null && difficulty.gameObject.activeSelf) +
                    " alpha=" + (difficulty != null ? difficulty.GetComponent<CanvasGroup>().alpha : -1f));
                Require(Vector3.Distance(panel.position, released) < 0.005f, hand + " UI click moved panel");
                Results.Add(hand + " button click transitions without grab");

                navigation.ShowWelcome();
                Pose(device, controller.transform.parent, position - panel.forward * 2f, rotation, false);
                yield return 35;
                InputSystem.RemoveDevice(device);
                Devices.Remove(device);
            }
        }

        private static void Pose(InputDevice device, Transform origin, Vector3 position, Quaternion rotation, bool pressed)
        {
            InputSystem.QueueDeltaStateEvent((Vector3Control)device["devicePosition"], origin.InverseTransformPoint(position));
            InputSystem.QueueDeltaStateEvent((QuaternionControl)device["deviceRotation"], Quaternion.Inverse(origin.rotation) * rotation);
            InputSystem.QueueDeltaStateEvent((ButtonControl)device["isTracked"], (byte)1);
            InputSystem.QueueDeltaStateEvent((IntegerControl)device["trackingState"], 3);
            InputSystem.QueueDeltaStateEvent((AxisControl)device["trigger"], pressed ? 1f : 0f);
            InputSystem.QueueDeltaStateEvent((ButtonControl)device["triggerButton"], pressed ? 1f : 0f);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            routine?.Dispose();
            routine = null;
            foreach (InputDevice device in Devices)
                if (device.added)
                    InputSystem.RemoveDevice(device);
            Devices.Clear();
            if (panel != null && EditorApplication.isPlaying)
            {
                panel.SetPositionAndRotation(originalPosition, originalRotation);
                Rigidbody body = panel.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.position = originalPosition;
                    body.rotation = originalRotation;
                }
                NavigationController navigation = UnityEngine.Object.FindObjectOfType<NavigationController>();
                if (navigation != null)
                    navigation.ShowWelcome();
            }
            panel = null;
        }
    }
}
#endif
