# 12.1. XR_HTC_frame_synchronization
## Overview
The application frame loop relies on xrWaitFrame throttling to synchronize application frame submissions with the display. This extension allows the application to set the frame synchronization mode to adjust the interval between the application frame submission time and the corresponding display time according to the demand of the application. The runtime will return the appropriate XrFrameState::predictedDisplayTime returned by xrWaitFrame to throttle the frame loop approaching to the frame rendering time of the application with the consistent good user experience throughout the session.
## Name String
    XR_HTC_frame_synchronization
## Revision
    1
## New Enum Constants
[XrStructureType](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrStructureType) enumeration is extended with:
- XR_TYPE_FRAME_SYNCHRONIZATION_SESSION_BEGIN_INFO_HTC
## New Enums
- XrFrameSynchronizationModeHTC
## New Structures
- XrFrameSynchronizationSessionBeginInfoHTC

## VIVE Plugin

Enable "VIVE XR Frame Synchronization" in "Project Settings > XR Plugin-in Management > OpenXR > Android Tab > OpenXR Feature Groups" to use the frame synchronization provided by VIVE OpenXR plugin.
