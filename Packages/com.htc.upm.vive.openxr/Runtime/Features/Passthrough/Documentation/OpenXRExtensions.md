# 12.89. XR_HTC_passthrough
## Name String
    XR_HTC_passthrough
## Revision
    1
## New Object Types
- [XrPassthroughHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughHTC)
## New Enum Constants
[XrObjectType](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrObjectType) enumeration is extended with:
- XR_OBJECT_TYPE_PASSTHROUGH_HTC

[XrStructureType](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrStructureType) enumeration is extended with:
- XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC
- XR_TYPE_PASSTHROUGH_COLOR_HTC
- XR_TYPE_PASSTHROUGH_MESH_TRANSFORM_INFO_HTC
- XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_HTC
## New Enums
- [XrPassthroughFormHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughFormHTC)
## New Structures
- [XrPassthroughCreateInfoHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughCreateInfoHTC)
- [XrPassthroughColorHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughColorHTC)
- [XrPassthroughMeshTransformInfoHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughMeshTransformInfoHTC)
- [XrCompositionLayerPassthroughHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrCompositionLayerPassthroughHTC)
## New Functions
- [xrCreatePassthroughHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#xrCreatePassthroughHTC)
- [xrDestroyPassthroughHTC](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#xrDestroyPassthroughHTC)

## VIVE Plugin

Enable "VIVE XR Passthrough" in "Project Settings > XR Plugin-in Management > OpenXR > Android Tab > OpenXR Feature Groups" to use the Passthrough feature provided by VIVE OpenXR plugin.
