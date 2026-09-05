// ===================== 2022 HTC Corporation. All Rights Reserved. ===================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using VIVE.OpenXR.Passthrough;

namespace VIVE.OpenXR
{
	public class XR_HTC_passthrough_configuration_defs
	{
		public virtual XrResult SetPassthroughConfigurationHTC(IntPtr config)
		{
			return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
		}

		public virtual XrResult GetPassthroughConfigurationHTC(IntPtr config)
		{
			return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
		}

		public virtual XrResult EnumeratePassthroughImageRatesHTC([In] UInt32 imageRateCapacityInput, ref UInt32 imageRateCountOutput, [In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates)
		{
			return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
		}
	}

	public class XR_HTC_passthrough_configuration
	{
		static XR_HTC_passthrough_configuration_defs m_Instance = null;
		public static XR_HTC_passthrough_configuration_defs Interop
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new XR_HTC_passthrough_configuration_impls();
				}
				return m_Instance;
			}
		}

		public static XrResult SetPassthroughConfigurationHTC(IntPtr config)
		{
			return Interop.SetPassthroughConfigurationHTC(config);
		}

		public static XrResult GetPassthroughConfigurationHTC(IntPtr config)
		{
			return Interop.GetPassthroughConfigurationHTC(config);
		}

		public static XrResult EnumeratePassthroughImageRatesHTC([In] UInt32 imageRateCapacityInput, ref UInt32 imageRateCountOutput, [In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates)
		{
			return Interop.EnumeratePassthroughImageRatesHTC(imageRateCapacityInput, ref imageRateCountOutput, imageRates);
		}
	}
}
