// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using AOT;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VIVE.OpenXR
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class HookHandlerAttribute : Attribute
    {
        public string xrFuncName { get; }

        /// <summary>
        /// Set this function to handle the hook process in <see cref="ViveInterceptors.XrGetInstanceProcAddrInterceptor" />
        /// </summary>
        /// <param name="xrFuncName">The hooked openxr function name</param>
        public HookHandlerAttribute(string xrFuncName)
        {
            this.xrFuncName = xrFuncName;
        }
    }

    /// <summary>
    /// This class is made for all features that need to intercept OpenXR API calls.
    /// Some APIs will be called by Unity internally, and we need to intercept them in c# to get some information.
    /// Append more interceptable functions for this class by adding a new partial class.
    /// The partial class can help the delegate name be nice to read and search.
    /// Please create per function in one partial class.
    /// 
    /// For all features want to use this class, please call <see cref="HookGetInstanceProcAddr" /> in your feature class.
    /// For example:
    ///     protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
    ///     {
    ///         return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
    ///     }
    /// </summary>

    //  For extending the ViveInterceptors class, create a new partial class and implement the required functions.
    //  For example:
    //  public partial class ViveInterceptors
    //  {
    //      [HookHandler("xrYourFunction")]
    //      private static XrResult OnHookXrYourFunction(XrInstance instance, string name, out IntPtr function)
    //      { ... }
    //  }
    partial class ViveInterceptors
	{
		public const string TAG = "VIVE.OpenXR.ViveInterceptors";
		static StringBuilder m_sb = null;
		static StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}

        public static ViveInterceptors instance = null;
        public static ViveInterceptors Instance
        {
            get
            {
                if (instance == null)
                    instance = new ViveInterceptors();
                return instance;
            }
        }

        public ViveInterceptors()
        {
            Log.D("ViveInterceptors");
            RegisterFunctions();
        }

        delegate XrResult HookHandler(XrInstance instance, string name, out IntPtr function);
        static readonly Dictionary<string, HookHandler> interceptors = new Dictionary<string, HookHandler>();

        private static void RegisterFunctions()
        {
            var methods = typeof(ViveInterceptors).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttributes(typeof(HookHandlerAttribute), false).FirstOrDefault() as HookHandlerAttribute;
                if (attribute != null)
                {
                    Log.I(TAG, $"Registering hook handler {attribute.xrFuncName}");
                    interceptors.Add(attribute.xrFuncName, (HookHandler)method.CreateDelegate(typeof(HookHandler)));
                }
            }
        }

        private static readonly OpenXRHelper.xrGetInstanceProcAddrDelegate hookXrGetInstanceProcAddrHandle = new OpenXRHelper.xrGetInstanceProcAddrDelegate(XrGetInstanceProcAddrInterceptor);
        private static readonly IntPtr hookGetInstanceProcAddrHandlePtr = Marshal.GetFunctionPointerForDelegate(hookXrGetInstanceProcAddrHandle);
        static OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddrOriginal = null;

        [MonoPInvokeCallback(typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate))]
        private static XrResult XrGetInstanceProcAddrInterceptor(XrInstance instance, string name, out IntPtr function)
        {
            // Used to check if the original function is already hooked.
            if (instance == 0 && name == "ViveInterceptorHooked")
            {
                function = IntPtr.Zero;
                return XrResult.XR_SUCCESS;
            }

            // Check if the function is intercepted by other features
            if (interceptors.ContainsKey(name))
            {
                // If no request for this function, call the original function directly.
                if (!requiredFunctions.Contains(name))
                    return XrGetInstanceProcAddrOriginal(instance, name, out function);

                var ret = interceptors[name](instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                    Log.I(TAG, name + " is intercepted");
                return ret;
            }

            return XrGetInstanceProcAddrOriginal(instance, name, out function);
        }

        public IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            Log.D(TAG, "HookGetInstanceProcAddr");
            if (XrGetInstanceProcAddrOriginal == null)
            {
                Log.D(TAG, "registering our own xrGetInstanceProcAddr");
                XrGetInstanceProcAddrOriginal = Marshal.GetDelegateForFunctionPointer<OpenXRHelper.xrGetInstanceProcAddrDelegate>(func);

#if UNITY_EDITOR
                if (Application.isEditor) {
                    // This is a trick to check if the original function is already hooked by this class.  Sometimes, the static XrGetInstanceProcAddrOriginal didn't work as expected.
                    Log.D(TAG, "Check if duplicate hooked by this script with instance=0 and \"ViveInterceptorHooked\" name.  If following a loader error, ignore it.");
                    // E OpenXR-Loader: Error [SPEC | xrGetInstanceProcAddr | VUID-xrGetInstanceProcAddr-instance-parameter] : XR_NULL_HANDLE for instance but query for ViveInterceptorHooked requires a valid instance

                    // Call XrGetInstanceProcAddrOriginal to check if the original function is already hooked by this class
                    if (XrGetInstanceProcAddrOriginal(0, "ViveInterceptorHooked", out IntPtr function) == XrResult.XR_SUCCESS)
                    {
                        // If it is called successfully, it means the original function is already hooked.  So we should return the original function.
                        Log.D(TAG, "Already hooked");
                        return func;
                    }
                }
#endif

                return hookGetInstanceProcAddrHandlePtr;
            }
            else
            {
                // Dont return hookGetInstanceProcAddrHandlePtr again.
                // If this hook function is called by multiple features, it should only work at the first time.
                // If called by other features, it should return the original function.
                return func;
            }
        }

        static readonly List<string> requiredFunctions = new List<string>();

        /// <summary>
        /// Call before <see cref="HookGetInstanceProcAddr" /> to add required functions."/>
        /// </summary>
        /// <param name="name"></param>
        public void AddRequiredFunction(string name)
        {
            Log.D(TAG, $"AddRequiredFunction({name})");
            if (!interceptors.ContainsKey(name))
            {
                Log.E(TAG, $"AddRequiredFunction({name}) failed.  No such function.");
                return;
            }

            if (!requiredFunctions.Contains(name))
                requiredFunctions.Add(name);

            // If your function support unregister, you can add the reference count here.
            if (name == "xrLocateViews")
                xrLocateViewsReferenceCount++;
        }

        /// <summary>
        /// If no need to use this hooked function, call this will remove your requirement.
        /// If all requirements are removed, the original function will be called directly.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveRequiredFunction(string name)
        {
            // If your function support unregister, you can add the reference count here.
            if (requiredFunctions.Contains(name))
            {
                if (name == "xrLocateViews")
                    xrLocateViewsReferenceCount = Mathf.Max(xrLocateViewsReferenceCount--, 0);
            }
        }
    }
}

