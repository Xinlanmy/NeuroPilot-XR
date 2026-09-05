using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Interaction;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
namespace VIVE.OpenXR.Hand
{
    public class ViveHandSubsystem : XRHandSubsystem
    {
        public const string featureId = "vive.openxr.feature.xrhandsubsystem";
        private static XRHandSubsystem subsystem = null;
        private XRHandProviderUtility.SubsystemUpdater subsystemUpdater = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
            if (!ViveHandTrackingSupport()) { return; }
            bool handInteractionSupport = HandInteractionSupport();

            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = featureId,
                providerType = typeof(ViveHandProvider),
                subsystemTypeOverride = typeof(ViveHandSubsystem),
#if UNITY_XR_HANDS_1_5_0
                supportsAimPose = handInteractionSupport,
                supportsAimActivateValue = handInteractionSupport,
                supportsGraspValue = handInteractionSupport,
                supportsGripPose = handInteractionSupport,
                supportsPinchPose = handInteractionSupport,
                supportsPinchValue = handInteractionSupport,
                supportsPokePose = handInteractionSupport,
#endif
            };
            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartSubsystem()
        {
            List<XRHandSubsystemDescriptor> descriptors = new List<XRHandSubsystemDescriptor>();
            if (subsystem == null || !subsystem.running)
            {
                descriptors.Clear();
                SubsystemManager.GetSubsystemDescriptors(descriptors);
                for (int i = 0; i < descriptors.Count; i++)
                {
                    XRHandSubsystemDescriptor descriptor = descriptors[i];
                    if (descriptor.id == featureId)
                    {
                        subsystem = descriptor.Create();
                        subsystem.Start();
                    }
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (subsystemUpdater == null)
            {
                subsystemUpdater = new XRHandProviderUtility.SubsystemUpdater(subsystem);
            }
            subsystemUpdater.Start();

        }

        protected override void OnStop()
        {
            base.OnStop();
            subsystemUpdater.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            subsystemUpdater.Destroy();
            subsystemUpdater = null;
        }

        private static bool ViveHandTrackingSupport()
        {
            ViveHandTracking viveHand = OpenXRSettings.Instance.GetFeature<ViveHandTracking>();
            return viveHand.enabled;
        }

        private static bool HandInteractionSupport()
        {
            ViveInteractions viveInteractions = OpenXRSettings.Instance.GetFeature<ViveInteractions>();
            if (viveInteractions.enabled)
            {
                return viveInteractions.UseKhrHandInteraction();
            }
            return false;
        }
    }
}
#endif