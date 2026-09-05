# XR_EXT_plane_detection
## Name String
    XR_EXT_plane_detection
## Revision
    1
## Overview

The PlaneDetectionManager class provides functionalities for managing plane detection using the VIVE XR SDK. It includes methods to check feature support, create and destroy plane detectors, and helper functions for interacting with the plane detection extension.

## Plane Detection Workflow

1. Check Feature Support:

	```csharp
	bool isSupported = PlaneDetectionManager.IsSupported();
	```

	Ensure the plane detection feature is supported before attempting to create a plane detector.

1. Create Plane Detector:

	```csharp
	PlaneDetector planeDetector = PlaneDetectionManager.CreatePlaneDetector();
	```

	Create a plane detector instance to begin detecting planes.

1. Begin Plane Detection:

	```csharp
	XrResult result = planeDetector.BeginPlaneDetection();
	```

	Start the plane detection process.

1. Get Plane Detection State:

	```csharp
	XrPlaneDetectionStateEXT state = planeDetector.GetPlaneDetectionState();
	```

	Check the current state of the plane detection process.

1. Retrieve Plane Detections:

	```csharp
	List<PlaneDetectorLocation> locations;
	XrResult result = planeDetector.GetPlaneDetections(out locations);
	```

	Retrieve the detected planes.

1. Get Plane Vertices:

	```csharp
	Plane plane = planeDetector.GetPlane(planeId);
	```

	Retrieve the vertices of a specific plane.

1. Destroy Plane Detector:

	```csharp
	PlaneDetectionManager.DestroyPlaneDetector(planeDetector);
	```

	Destroy the plane detector to release resources.


## Example Usage

Here's a basic example of how to use the PlaneDetectionManager to detect planes:

```csharp

if (PlaneDetectionManager.IsSupported())
{
    var planeDetector = PlaneDetectionManager.CreatePlaneDetector();
    if (planeDetector != null)
    {
        planeDetector.BeginPlaneDetection();
        
        XrPlaneDetectionStateEXT state = planeDetector.GetPlaneDetectionState();
        if (state == XrPlaneDetectionStateEXT.DONE_EXT)
        {
            List<PlaneDetectorLocation> locations;
            if (planeDetector.GetPlaneDetections(out locations) == XrResult.XR_SUCCESS)
            {
                foreach (var location in locations)
                {
                    // Process detected planes
                }
            }
        }
        
        PlaneDetectionManager.DestroyPlaneDetector(planeDetector);
    }
}
```

This example checks if the plane detection feature is supported, creates a plane detector, begins the plane detection process, retrieves the detected planes, and finally destroys the plane detector to release resources.