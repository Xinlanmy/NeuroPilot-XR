// ===================== 2022 HTC Corporation. All Rights Reserved. ===================

using System;
using System.Runtime.InteropServices;
using UnityEngine;

using UnityEngine.XR.OpenXR;

using VIVE.OpenXR.Passthrough;


namespace VIVE.OpenXR
{
	public class XR_HTC_passthrough_configuration_impls : XR_HTC_passthrough_configuration_defs
	{
		const string LOG_TAG = "VIVE.OpenXR.XR_HTC_passthrough_configuration_impls";
		void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }

		private VivePassthrough feature = null;
		private void ASSERT_FEATURE()
		{
			if (feature == null) { feature = OpenXRSettings.Instance.GetFeature<VivePassthrough>(); }
		}

		public override XrResult SetPassthroughConfigurationHTC(IntPtr config)
		{
			DEBUG("SetPassthroughConfigurationHTC");
			XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;

			ASSERT_FEATURE();
			if (feature)
			{
				if (!feature.SupportsImageQuality() || !feature.SupportsImageRate())
				    return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;

				result = (XrResult)feature.SetPassthroughConfigurationHTC(config);
			}

			return result;
		}

		public override XrResult GetPassthroughConfigurationHTC(IntPtr config)
		{
			DEBUG("GetPassthroughConfigurationHTC");
			XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;

			ASSERT_FEATURE();
			if (feature)
			{
				if (!feature.SupportsImageQuality() || !feature.SupportsImageRate())
					return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
				result = (XrResult)feature.GetPassthroughConfigurationHTC(config);
			}

			return result;
		}

		public override XrResult EnumeratePassthroughImageRatesHTC([In] UInt32 imageRateCapacityInput, ref UInt32 imageRateCountOutput, [In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates)
		{
			DEBUG("EnumeratePassthroughImageRatesHTC");
			XrResult result = XrResult.XR_ERROR_VALIDATION_FAILURE;

			ASSERT_FEATURE();
			if (feature)
			{
				if (!feature.SupportsImageQuality() || !feature.SupportsImageRate())
					return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
				result = (XrResult)feature.EnumeratePassthroughImageRatesHTC(imageRateCapacityInput, ref imageRateCountOutput, imageRates);
			}

			return result;
		}
	}
}

