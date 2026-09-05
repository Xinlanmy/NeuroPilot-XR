// Copyright HTC Corporation All Rights Reserved.

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using System.Text;
// Non android will need UnityEngine
using UnityEngine;

namespace VIVE.OpenXR
{
    public static class Log
    {
        public const string TAG = "VIVE.OpenXR";

#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport("liblog.so")]
        private static extern int __android_log_print(int prio, string tag, string fmt, string msg);
#endif

        // Use ("%s", message) instead of just (message) is because of the following reason:
        // In case message contains special characters like %, \n, \r, etc. It will be treated as format string.
        // This is a little waste of performance, but it's safer.

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void D(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, TAG, "%s", message); // Android Debug
#endif
        }

        public static void I(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, TAG, "%s", message); // Android Info
#else
            Debug.Log(message);
#endif
        }

        public static void W(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, TAG, "%s", message); // Android Warning
#else
            Debug.LogWarning(message);
#endif
        }

        public static void E(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, TAG, "%s", message); // Android Error
#else
            Debug.LogError(message);
#endif
        }

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void D(string tag, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, tag, "%s", message);
#endif
        }

        public static void I(string tag, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, tag, "%s", message);
#else
            Debug.LogFormat("{0}: {1}", tag, message);
#endif
        }

        public static void W(string tag, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, tag, "%s", message); // Android Warning
#else
            Debug.LogWarningFormat("{0}: {1}", tag, message);
#endif
        }

        public static void E(string tag, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, tag, "%s", message); // Android Error
#else
            Debug.LogErrorFormat("{0}: {1}", tag, message);
#endif
        }

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void D(StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, TAG, "%s", message.ToString());
#endif
        }

        public static void I(StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, TAG, "%s", message.ToString());
#else
            Debug.Log(message.ToString());
#endif
        }

        public static void W(StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, TAG, "%s", message.ToString()); // Android Warning
#else
            Debug.LogWarning(message.ToString());
#endif
        }

        public static void E(StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, TAG, "%s", message.ToString()); // Android Error
#else
            Debug.LogError(message.ToString());
#endif
        }

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void D(string tag, StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, tag, "%s", message.ToString());
#endif
        }

        public static void I(string tag, StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, tag, "%s", message.ToString());
#else
            Debug.LogFormat("{0}: {1}", tag, message.ToString());
#endif
        }

        public static void W(string tag, StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, tag, "%s", message.ToString()); // Android Warning
#else
            Debug.LogWarningFormat("{0}: {1}", tag, message.ToString());
#endif
        }

        public static void E(string tag, StringBuilder message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, tag, "%s", message.ToString()); // Android Error
#else
            Debug.LogErrorFormat("{0}: {1}", tag, message.ToString());
#endif
        }

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void DFmt(string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, TAG, "%s", string.Format(fmt, args));
#endif
        }

        public static void IFmt(string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, TAG, "%s", string.Format(fmt, args));
#else
            Debug.LogFormat(fmt, args);
#endif
        }

        public static void WFmt(string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, TAG, "%s", string.Format(fmt, args)); // Android Warning
#else
            Debug.LogWarningFormat(fmt, args);
#endif
        }

        public static void EFmt(string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, TAG, "%s", string.Format(fmt, args)); // Android Error
#else
            Debug.LogErrorFormat(fmt, args);
#endif
        }

        /// <summary>
        /// Not show in Standalone
        /// </summary>
        public static void DFmt(string tag, string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(3, tag, "%s", string.Format(fmt, args));
#endif
        }

        public static void IFmt(string tag, string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(4, tag, "%s", string.Format(fmt, args));
#else
            Debug.LogFormat("{0}: {1}", tag, string.Format(fmt, args));
#endif
        }

        public static void WFmt(string tag, string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(5, tag, "%s", string.Format(fmt, args)); // Android Warning
#else
            Debug.LogWarningFormat("{0}: {1}", tag, fmt, string.Format(fmt, args));
#endif
        }

        public static void EFmt(string tag, string fmt, params object[] args)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            __android_log_print(6, tag, "%s", string.Format(fmt, args)); // Android Error
#else
            Debug.LogErrorFormat("{0}: {1}", tag, fmt, string.Format(fmt, args));
#endif
        }
    }
}