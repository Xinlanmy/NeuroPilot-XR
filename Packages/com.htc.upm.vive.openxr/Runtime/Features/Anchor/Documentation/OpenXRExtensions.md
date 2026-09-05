# XR_HTC_anchor XR_HTC_anchor_persistence
## Name String
    XR_htc_anchor XR_HTC_anchor_persistence
## Revision
    1
## Overview

This document provides an overview of how to use the AnchorManager to manage anchors in an OpenXR application, specifically using the XR_HTC_anchor and XR_HTC_anchor_persistence extensions.
Introduction

Anchors in OpenXR allow applications to track specific points in space over time. The XR_HTC_anchor extension provides the basic functionality for creating and managing anchors, while the XR_HTC_anchor_persistence extension allows anchors to be persisted across sessions. The AnchorManager class simplifies the use of these extensions by providing high-level methods for common operations.
Checking Extension Support

Before using any anchor-related functions, it's important to check if the extensions are supported on the current system.

```csharp

bool isAnchorSupported = AnchorManager.IsSupported();
bool isPersistedAnchorSupported = AnchorManager.IsPersistedAnchorSupported();

```

## Creating and Managing Anchors
### Creating an Anchor

To create a new anchor, use the CreateAnchor method. This method requires a Pose representing the anchor's position and orientation relative to the tracking space, and a name for the anchor.

```csharp

Pose anchorPose = new Pose(new Vector3(0, 0, 0), Quaternion.identity);
AnchorManager.Anchor newAnchor = AnchorManager.CreateAnchor(anchorPose, "MyAnchor");

```

### Getting an Anchor's Name

To retrieve the name of an existing anchor, use the GetSpatialAnchorName method.

```csharp

string anchorName;
bool success = AnchorManager.GetSpatialAnchorName(newAnchor, out anchorName);
if (success) {
    Debug.Log("Anchor name: " + anchorName);
}

```

### Tracking Space and Pose

To get the current tracking space, use the GetTrackingSpace method. To retrieve the pose of an anchor relative to the current tracking space, use the GetTrackingSpacePose method.

```csharp

XrSpace trackingSpace = AnchorManager.GetTrackingSpace();
Pose anchorPose;
bool poseValid = AnchorManager.GetTrackingSpacePose(newAnchor, out anchorPose);
if (poseValid) {
    Debug.Log("Anchor pose: " + anchorPose.position + ", " + anchorPose.rotation);
}

```

## Persisting Anchors
### Creating a Persisted Anchor Collection

To enable anchor persistence, create a persisted anchor collection using the CreatePersistedAnchorCollection method.

```csharp

Task createCollectionTask = AnchorManager.CreatePersistedAnchorCollection();
createCollectionTask.Wait();

```

### Persisting an Anchor

To persist an anchor, use the PersistAnchor method with the anchor and a unique name for the persisted anchor.

```csharp

string persistedAnchorName = "MyPersistedAnchor";
XrResult result = AnchorManager.PersistAnchor(newAnchor, persistedAnchorName);
if (result == XrResult.XR_SUCCESS) {
    Debug.Log("Anchor persisted successfully.");
}

```

### Unpersisting an Anchor

To remove a persisted anchor, use the UnpersistAnchor method with the name of the persisted anchor.

```csharp

XrResult result = AnchorManager.UnpersistAnchor(persistedAnchorName);
if (result == XrResult.XR_SUCCESS) {
    Debug.Log("Anchor unpersisted successfully.");
}

```

### Enumerating Persisted Anchors

To get a list of all persisted anchors, use the EnumeratePersistedAnchorNames method.

```csharp

string[] persistedAnchorNames;
XrResult result = AnchorManager.EnumeratePersistedAnchorNames(out persistedAnchorNames);
if (result == XrResult.XR_SUCCESS) {
    foreach (var name in persistedAnchorNames) {
        Debug.Log("Persisted anchor: " + name);
    }
}

```

### Creating an Anchor from a Persisted Anchor

To create an anchor from a persisted anchor, use the CreateSpatialAnchorFromPersistedAnchor method.

```csharp

AnchorManager.Anchor trackableAnchor;
XrResult result = AnchorManager.CreateSpatialAnchorFromPersistedAnchor(persistedAnchorName, "NewAnchor", out trackableAnchor);
if (result == XrResult.XR_SUCCESS) {
    Debug.Log("Anchor created from persisted anchor.");
}

```

## Exporting and Importing Persisted Anchors
### Exporting a Persisted Anchor

To export a persisted anchor to a buffer, use the ExportPersistedAnchor method.

```csharp

Task<(XrResult, string, byte[])> exportTask = AnchorManager.ExportPersistedAnchor(persistedAnchorName);
exportTask.Wait();
var (exportResult, exportName, buffer) = exportTask.Result;
if (exportResult == XrResult.XR_SUCCESS) {
    // Save buffer to a file or use as needed
    File.WriteAllBytes("anchor.pa", buffer);
}

```

### Importing a Persisted Anchor

To import a persisted anchor from a buffer, use the ImportPersistedAnchor method.

```csharp

byte[] buffer = File.ReadAllBytes("anchor.pa");
Task<XrResult> importTask = AnchorManager.ImportPersistedAnchor(buffer);
importTask.Wait();
if (importTask.Result == XrResult.XR_SUCCESS) {
    Debug.Log("Anchor imported successfully.");
}

```

### Clearing Persisted Anchors

To clear all persisted anchors, use the ClearPersistedAnchors method.

```csharp

XrResult result = AnchorManager.ClearPersistedAnchors();
if (result == XrResult.XR_SUCCESS) {
    Debug.Log("All persisted anchors cleared.");
}

```

## Conclusion

The AnchorManager class simplifies the management of anchors in OpenXR applications. By using the methods provided, you can easily create, persist, and manage anchors, ensuring that spatial data can be maintained across sessions. This document covers the basic operations; for more advanced usage, refer to the OpenXR specification and the implementation details of the AnchorManager class.