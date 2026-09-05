// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VIVE.OpenXR.Passthrough
{
    /// <summary>
    /// The forms of passthrough layer.
    /// </summary>
	public enum PassthroughLayerForm
    {
        ///<summary> Fullscreen Passthrough Form</summary>
        Planar = 0,
        ///<summary> Projected Passthrough Form</summary>
        Projected = 1
    }

    /// <summary>
    /// The types of passthrough space.
    /// </summary>
    public enum ProjectedPassthroughSpaceType
    {
        ///<summary>
        /// XR_REFERENCE_SPACE_TYPE_VIEW at (0,0,0) with orientation (0,0,0,1)
        ///</summary>
        Headlock = 0,
        ///<summary>
        /// When TrackingOriginMode is TrackingOriginModeFlags.Floor:
        /// XR_REFERENCE_SPACE_TYPE_STAGE at (0,0,0) with orientation (0,0,0,1)
        ///
        /// When TrackingOriginMode is TrackingOriginModeFlags.Device:
        /// XR_REFERENCE_SPACE_TYPE_LOCAL at (0,0,0) with orientation (0,0,0,1)
        ///
        ///</summary>
        Worldlock = 1
    }

    // -------------------- 12.88. XR_HTC_passthrough --------------------

    #region New Object Types
    /// <summary>
    /// An application can create an <see href="https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrPassthroughHTC">XrPassthroughHTC</see> handle by calling <see href="https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#xrCreatePassthroughHTC">xrCreatePassthroughHTC</see>. The returned passthrough handle can be subsequently used in API calls.
    /// </summary>
    public struct XrPassthroughHTC : IEquatable<UInt64>
    {
        private readonly UInt64 value;

        public XrPassthroughHTC(UInt64 u)
        {
            value = u;
        }

        public static implicit operator UInt64(XrPassthroughHTC equatable)
        {
            return equatable.value;
        }
        public static implicit operator XrPassthroughHTC(UInt64 u)
        {
            return new XrPassthroughHTC(u);
        }

        public bool Equals(XrPassthroughHTC other)
        {
            return value == other.value;
        }
        public bool Equals(UInt64 other)
        {
            return value == other;
        }
        public override bool Equals(object obj)
        {
            return obj is XrPassthroughHTC && Equals((XrPassthroughHTC)obj);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static bool operator ==(XrPassthroughHTC a, XrPassthroughHTC b) { return a.Equals(b); }
        public static bool operator !=(XrPassthroughHTC a, XrPassthroughHTC b) { return !a.Equals(b); }
        public static bool operator >=(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value >= b.value; }
        public static bool operator <=(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value <= b.value; }
        public static bool operator >(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value > b.value; }
        public static bool operator <(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value < b.value; }
        public static XrPassthroughHTC operator +(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value + b.value; }
        public static XrPassthroughHTC operator -(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value - b.value; }
        public static XrPassthroughHTC operator *(XrPassthroughHTC a, XrPassthroughHTC b) { return a.value * b.value; }
        public static XrPassthroughHTC operator /(XrPassthroughHTC a, XrPassthroughHTC b)
        {
            if (b.value == 0)
            {
                throw new DivideByZeroException();
            }
            return a.value / b.value;
        }

    }
    #endregion

    #region New Enums
    /// <summary>
    /// The XrPassthroughFormHTC enumeration identifies the form of the passthrough, presenting the passthrough fill the full screen or project onto a specified mesh.
    /// </summary>
    public enum XrPassthroughFormHTC
    {
        /// <summary>
        /// Presents the passthrough with full of the entire screen..
        /// </summary>
        XR_PASSTHROUGH_FORM_PLANAR_HTC = 0,
        /// <summary>
        /// Presents the passthrough projecting onto a custom mesh.
        /// </summary>
        XR_PASSTHROUGH_FORM_PROJECTED_HTC = 1,
    };
    #endregion

    #region New Structures
    /// <summary>
    /// The XrPassthroughCreateInfoHTC structure describes the information to create an <see cref="XrPassthroughCreateInfoHTC">XrPassthroughCreateInfoHTC</see> handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XrPassthroughCreateInfoHTC
    {
        /// <summary>
        /// The <see cref="XrStructureType">XrStructureType</see> of this structure.
        /// </summary>
        public XrStructureType type;
        /// <summary>
        /// NULL or a pointer to the next structure in a structure chain. No such structures are defined in core OpenXR or this extension.
        /// </summary>
        public IntPtr next;
        /// <summary>
        /// The form specifies the form of passthrough.
        /// </summary>
        public XrPassthroughFormHTC form;

        /// <param name="in_type">The <see cref="XrStructureType">XrStructureType</see> of this structure.</param>
        /// <param name="in_next">NULL or a pointer to the next structure in a structure chain. No such structures are defined in core OpenXR or this extension.</param>
        /// <param name="in_facialTrackingType">An XrFacialTrackingTypeHTC which describes which type of facial tracking should be used for this handle.</param>
        public XrPassthroughCreateInfoHTC(XrStructureType in_type, IntPtr in_next, XrPassthroughFormHTC in_form)
        {
            type = in_type;
            next = in_next;
            form = in_form;
        }
    };

    /// <summary>
    /// The application can specify the XrPassthroughColorHTC to adjust the alpha value of the passthrough. The range is between 0.0f and 1.0f, 1.0f means opaque.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XrPassthroughColorHTC
    {
        /// <summary>
        /// The XrStructureType of this structure.
        /// </summary>
        public XrStructureType type;
        /// <summary>
        /// Next is NULL or a pointer to the next structure in a structure chain, such as XrPassthroughMeshTransformInfoHTC.
        /// </summary>
        public IntPtr next;
        /// <summary>
        /// The alpha value of the passthrough in the range [0, 1].
        /// </summary>
        public float alpha;
        public XrPassthroughColorHTC(float in_alpha)
        {
            type = XrStructureType.XR_TYPE_PASSTHROUGH_COLOR_HTC;
            next = IntPtr.Zero;
            alpha = in_alpha;
        }
    };

    /// <summary>
    /// The XrPassthroughMeshTransformInfoHTC structure describes the mesh and transformation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XrPassthroughMeshTransformInfoHTC
    {
        /// <summary>
        /// The XrStructureType of this structure.
        /// </summary>
        public XrStructureType type;
        /// <summary>
        /// Next is NULL or a pointer to the next structure in a structure chain.
        /// </summary>
        public IntPtr next;
        /// <summary>
        /// The count of vertices array in the mesh.
        /// </summary>
        public UInt32 vertexCount;
        /// <summary>
        /// An array of XrVector3f. The size of the array must be equal to vertexCount.
        /// </summary>
        public IntPtr vertices;  // XrVector3f
        /// <summary>
        /// The count of indices array in the mesh.
        /// </summary>
        public UInt32 indexCount;
        /// <summary>
        /// An array of triangle indices. The size of the array must be equal to indexCount.
        /// </summary>
        public IntPtr indices; // UInt32[]
        /// <summary>
        /// The XrSpace that defines the projected passthrough's base space for transformations.
        /// </summary>
        public XrSpace baseSpace;
        /// <summary>
        /// The XrTime that defines the time at which the transform is applied.
        /// </summary>
        public XrTime time;
        /// <summary>
        /// The XrPosef that defines the pose of the mesh
        /// </summary>
        public XrPosef pose;
        /// <summary>
        /// The XrVector3f that defines the scale of the mesh
        /// </summary>
        public XrVector3f scale;
    };

    /// <summary>
    /// A pointer to XrCompositionLayerPassthroughHTC may be submitted in xrEndFrame as a pointer to the base structure XrCompositionLayerBaseHeader, in the desired layer order, to request the runtime to composite a passthrough layer into the final frame output.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XrCompositionLayerPassthroughHTC
    {
        /// <summary>
        /// The XrStructureType of this structure.
        /// </summary>
        public XrStructureType type;
        /// <summary>
        /// Next is NULL or a pointer to the next structure in a structure chain, such as XrPassthroughMeshTransformInfoHTC.
        /// </summary>
        public IntPtr next;
        /// <summary>
        /// A bitmask of XrCompositionLayerFlagBits describing flags to apply to the layer.
        /// </summary>
        public XrCompositionLayerFlags layerFlags;
        /// <summary>
        /// The XrSpace that specifies the layer¡¦s space - must be XR_NULL_HANDLE.
        /// </summary>
        public XrSpace space;
        /// <summary>
        /// The XrPassthroughHTC previously created by xrCreatePassthroughHTC.
        /// </summary>
        public XrPassthroughHTC passthrough;
        /// <summary>
        /// The XrPassthroughColorHTC describing the color information with the alpha value of the passthrough layer.
        /// </summary>
        public XrPassthroughColorHTC color;

        public XrCompositionLayerPassthroughHTC(XrCompositionLayerFlags in_layerFlags,
            XrSpace in_space, XrPassthroughHTC in_passthrough, XrPassthroughColorHTC in_color)
        {
            type = XrStructureType.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_HTC;
            next = IntPtr.Zero;
            layerFlags = in_layerFlags;
            space = in_space;
            passthrough = in_passthrough;
            color = in_color;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XrPassthroughConfigurationBaseHeaderHTC
    {
        public XrStructureType type;
        public IntPtr next;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct XrPassthroughConfigurationImageRateHTC
    {
        public XrStructureType type;
        public IntPtr next;
        public float srcImageRate;
        public float dstImageRate;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct XrPassthroughConfigurationImageQualityHTC
    {
        public XrStructureType type;
        public IntPtr next;
        public float scale;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XrEventDataPassthroughConfigurationImageRateChangedHTC
    {
        public XrStructureType type;
        public IntPtr next;
        public XrPassthroughConfigurationImageRateHTC fromImageRate;
        public XrPassthroughConfigurationImageRateHTC toImageRate;

        public XrEventDataPassthroughConfigurationImageRateChangedHTC(XrStructureType in_type, IntPtr in_next, XrPassthroughConfigurationImageRateHTC in_fromImageRate, XrPassthroughConfigurationImageRateHTC in_toImageRate)
        {
            type = in_type;
            next = in_next;
            fromImageRate = in_fromImageRate;
            toImageRate = in_toImageRate;
        }
        public static XrEventDataPassthroughConfigurationImageRateChangedHTC identity
        {
            get
            {
                return new XrEventDataPassthroughConfigurationImageRateChangedHTC(
                    XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_CHANGED_HTC,
                    IntPtr.Zero,
                    new XrPassthroughConfigurationImageRateHTC { type = XrStructureType.XR_TYPE_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_HTC, next = IntPtr.Zero },
                    new XrPassthroughConfigurationImageRateHTC { type = XrStructureType.XR_TYPE_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_HTC, next = IntPtr.Zero }); // user is default present
            }
        }
        public static bool Get(XrEventDataBuffer eventDataBuffer, out XrEventDataPassthroughConfigurationImageRateChangedHTC eventDataPassthroughConfigurationImageRate)
		{
            eventDataPassthroughConfigurationImageRate = identity;
            if (eventDataBuffer.type == XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_RATE_CHANGED_HTC)
            {
                eventDataPassthroughConfigurationImageRate.next = eventDataBuffer.next;
                eventDataPassthroughConfigurationImageRate.fromImageRate.type = (XrStructureType)BitConverter.ToUInt32(eventDataBuffer.varying, 0);
                eventDataPassthroughConfigurationImageRate.fromImageRate.next = (IntPtr)BitConverter.ToInt64(eventDataBuffer.varying, 8);
                eventDataPassthroughConfigurationImageRate.fromImageRate.srcImageRate = BitConverter.ToSingle(eventDataBuffer.varying, 16);
                eventDataPassthroughConfigurationImageRate.fromImageRate.dstImageRate = BitConverter.ToSingle(eventDataBuffer.varying, 20);
                eventDataPassthroughConfigurationImageRate.toImageRate.type = (XrStructureType)BitConverter.ToUInt32(eventDataBuffer.varying, 24);
                eventDataPassthroughConfigurationImageRate.toImageRate.next = (IntPtr)BitConverter.ToInt64(eventDataBuffer.varying, 32);
                eventDataPassthroughConfigurationImageRate.toImageRate.srcImageRate = BitConverter.ToSingle(eventDataBuffer.varying, 40);
                eventDataPassthroughConfigurationImageRate.toImageRate.dstImageRate = BitConverter.ToSingle(eventDataBuffer.varying, 44);
                return true;
            }

            return false;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XrEventDataPassthroughConfigurationImageQualityChangedHTC
    {
        public XrStructureType type;
        public IntPtr next;
        public XrPassthroughConfigurationImageQualityHTC fromImageQuality;
        public XrPassthroughConfigurationImageQualityHTC toImageQuality;

        public XrEventDataPassthroughConfigurationImageQualityChangedHTC(XrStructureType in_type, IntPtr in_next, XrPassthroughConfigurationImageQualityHTC in_fromImageQuality, XrPassthroughConfigurationImageQualityHTC in_toImageQuality)
        {
            type = in_type;
            next = in_next;
            fromImageQuality = in_fromImageQuality;
            toImageQuality = in_toImageQuality;
        }
        public static XrEventDataPassthroughConfigurationImageQualityChangedHTC identity
        {
            get
            {
                return new XrEventDataPassthroughConfigurationImageQualityChangedHTC(
                    XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_CHANGED_HTC,
                    IntPtr.Zero,
                    new XrPassthroughConfigurationImageQualityHTC { type = XrStructureType.XR_TYPE_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_HTC, next = IntPtr.Zero },
                    new XrPassthroughConfigurationImageQualityHTC { type = XrStructureType.XR_TYPE_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_HTC, next = IntPtr.Zero }); // user is default present
            }
        }
        public static bool Get(XrEventDataBuffer eventDataBuffer, out XrEventDataPassthroughConfigurationImageQualityChangedHTC ventDataPassthroughConfigurationImageQuality)
        {
            ventDataPassthroughConfigurationImageQuality = identity;
            if (eventDataBuffer.type == XrStructureType.XR_TYPE_EVENT_DATA_PASSTHROUGH_CONFIGURATION_IMAGE_QUALITY_CHANGED_HTC)
            {
                ventDataPassthroughConfigurationImageQuality.next = eventDataBuffer.next;
                ventDataPassthroughConfigurationImageQuality.fromImageQuality.type = (XrStructureType)BitConverter.ToUInt32(eventDataBuffer.varying, 0);
                ventDataPassthroughConfigurationImageQuality.fromImageQuality.next = (IntPtr)BitConverter.ToInt64(eventDataBuffer.varying, 8);
                ventDataPassthroughConfigurationImageQuality.fromImageQuality.scale = BitConverter.ToSingle(eventDataBuffer.varying, 16);
                ventDataPassthroughConfigurationImageQuality.toImageQuality.type = (XrStructureType)BitConverter.ToUInt32(eventDataBuffer.varying, 24);
                ventDataPassthroughConfigurationImageQuality.toImageQuality.next = (IntPtr)BitConverter.ToInt64(eventDataBuffer.varying, 32);
                ventDataPassthroughConfigurationImageQuality.toImageQuality.scale = BitConverter.ToSingle(eventDataBuffer.varying, 40);
                return true;
            }

            return false;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XrSystemPassthroughConfigurationPropertiesHTC
    {
        public XrStructureType type;
        public IntPtr next;
        public XrBool32 supportsImageRate;
        public XrBool32 supportsImageQuality;
    };

    #endregion

    #region New Functions
    public static class VivePassthroughHelper
    {
        /// <summary>
        /// The delegate function of <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrCreatePassthroughHTC">xrCreatePassthroughHTC</see>.
        /// </summary>
        /// <param name="session">An <see cref="XrSession">XrSession</see>  in which the passthrough will be active.</param>
        /// <param name="createInfo">createInfo is a pointer to an <see cref="XrPassthroughCreateInfoHTC">XrPassthroughCreateInfoHTC</see> structure containing information about how to create the passthrough.</param>
        /// <param name="passthrough">passthrough is a pointer to a handle in which the created <see cref="XrPassthroughHTC">XrPassthroughHTC</see> is returned.</param>
        /// <returns>XR_SUCCESS for success.</returns>
        public delegate XrResult xrCreatePassthroughHTCDelegate(
            XrSession session,
            XrPassthroughCreateInfoHTC createInfo,
            out XrPassthroughHTC passthrough);

        /// <summary>
        /// The delegate function of <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrDestroyPassthroughHTC">xrDestroyFacialTrackerHTC</see>.
        /// </summary>
        /// <param name="passthrough">passthrough is the <see cref="XrPassthroughHTC">XrPassthroughHTC</see> to be destroyed..</param>
        /// <returns>XR_SUCCESS for success.</returns>
        public delegate XrResult xrDestroyPassthroughHTCDelegate(
            XrPassthroughHTC passthrough);

        public delegate XrResult xrEnumeratePassthroughImageRatesHTCDelegate(
            XrSession session,
            [In] UInt32 imageRateCapacityInput,
            ref UInt32 imageRateCountOutput,
            [In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates);

        public delegate XrResult xrGetPassthroughConfigurationHTCDelegate(
            XrSession session,
            IntPtr/*ref XrPassthroughConfigurationBaseHeaderHTC*/ config);

        public delegate XrResult xrSetPassthroughConfigurationHTCDelegate(
            XrSession session,
            IntPtr/*ref XrPassthroughConfigurationBaseHeaderHTC*/ config);
    }

    public static class VivePassthroughImageQualityChanged
	{
        public delegate void OnImageQualityChanged(float fromQuality, float toQuality);

        public static void Listen(OnImageQualityChanged callback)
		{
            if (!allEventListeners.Contains(callback))
                allEventListeners.Add(callback);
        }
        public static void Remove(OnImageQualityChanged callback)
        {
            if (allEventListeners.Contains(callback))
                allEventListeners.Remove(callback);
        }
        public static void Send(float fromQuality, float toQuality)
        {
            int N = 0;
            if (allEventListeners != null)
            {
                N = allEventListeners.Count;
                for (int i = N - 1; i >= 0; i--)
                {
                    OnImageQualityChanged single = allEventListeners[i];
                    try
                    {
                        single(fromQuality, toQuality);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Event : " + e.ToString());
                        allEventListeners.Remove(single);
                        Debug.Log("Event : A listener is removed due to exception.");
                    }
                }
            }
        }

        private static List<OnImageQualityChanged> allEventListeners = new List<OnImageQualityChanged>();
    }

    public static class VivePassthroughImageRateChanged
    {
        public delegate void OnImageRateChanged(float fromSrcImageRate, float fromDestImageRate, float toSrcImageRate, float toDestImageRate);

        public static void Listen(OnImageRateChanged callback)
        {
            if (!allEventListeners.Contains(callback))
                allEventListeners.Add(callback);
        }
        public static void Remove(OnImageRateChanged callback)
        {
            if (allEventListeners.Contains(callback))
                allEventListeners.Remove(callback);
        }
        public static void Send(float fromSrcImageRate, float fromDestImageRate, float toSrcImageRate, float toDestImageRate)
        {
            int N = 0;
            if (allEventListeners != null)
            {
                N = allEventListeners.Count;
                for (int i = N - 1; i >= 0; i--)
                {
                    OnImageRateChanged single = allEventListeners[i];
                    try
                    {
                        single(fromSrcImageRate, fromDestImageRate, toSrcImageRate, toDestImageRate);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Event : " + e.ToString());
                        allEventListeners.Remove(single);
                        Debug.Log("Event : A listener is removed due to exception.");
                    }
                }
            }
        }

        private static List<OnImageRateChanged> allEventListeners = new List<OnImageRateChanged>();
    }
    #endregion
}
