// Copyright HTC Corporation All Rights Reserved.

namespace VIVE.OpenXR
{
    public class XR_EXT_user_presence_defs
    {
        /// <summary>
        /// Checks if an user is present. Default is true.
        /// </summary>
        /// <returns>True for present.</returns>
        public virtual bool IsUserPresent() { return true; }
    }

    public static class XR_EXT_user_presence
	{
        static XR_EXT_user_presence_defs m_Instance = null;
        public static XR_EXT_user_presence_defs Interop
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new XR_EXT_user_presence_impls();
                }
                return m_Instance;
            }
        }
    }
}