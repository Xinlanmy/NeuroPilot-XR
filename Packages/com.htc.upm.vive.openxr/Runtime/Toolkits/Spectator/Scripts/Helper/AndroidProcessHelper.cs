// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public static class AndroidProcessHelper
    {
        private static AndroidJavaObject _activity;

        public static AndroidJavaObject Activity
        {
            get
            {
                if (_activity != null)
                {
                    return _activity;
                }

                var unityPlayer = new AndroidJavaClass(ANDROID_CLASS_UNITY_PLAYER);
                _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                return _activity;
            }
        }

        // Reference: https://stackoverflow.com/q/58728596/10467387
        public static IEnumerator RequestPermission(Dictionary<string, PermissionCallbacks> permissions)
        {
            var permissionGranted = Enumerable.Repeat(false, permissions.Count).ToList();
            var permissionAsked = Enumerable.Repeat(false, permissions.Count).ToList();
            var permissionAction = new List<Action>();

            for (var i = 0; i < permissions.Count; i++)
            {
                int currentCount = i;
                (string permission, PermissionCallbacks permissionCallbacks) = permissions.ElementAt(currentCount);
                permissionAction.Add(() =>
                {
                    permissionGranted[currentCount] = Permission.HasUserAuthorizedPermission(permission);
                    if (permissionGranted[currentCount] || permissionAsked[currentCount])
                    {
                        return;
                    }

                    Permission.RequestUserPermission(permission, permissionCallbacks);
                    permissionAsked[currentCount] = true;
                });
            }

            for (var i = 0; i < permissions.Count;)
            {
                permissionAction[i].Invoke();
                if (permissionAsked[i])
                {
                    i++;
                }

                yield return new WaitForEndOfFrame();
            }
        }

        public const string ANDROID_CLASS_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
        public const string ANDROID_CLASS_MEDIA_STORE_IMAGE_MEDIA = "android.provider.MediaStore$Images$Media";
        public const string ANDROID_CLASS_GRAPHICS_BITMAP_FACTORY = "android.graphics.BitmapFactory";
        public const string ANDROID_CLASS_GRAPHICS_BITMAP_COMPRESS_FORMAT = "android.graphics.Bitmap$CompressFormat";
        public const string ANDROID_CLASS_OS_ENVIRONMENT = "android.os.Environment";
        public const string ANDROID_CLASS_OS_BUILD_VERSION = "android.os.Build$VERSION";
        public const string ANDROID_CLASS_CONTENT_INTENT = "android.content.Intent";
        public const string ANDROID_CLASS_CONTENT_VALUES = "android.content.ContentValues";
        public const string ANDROID_CLASS_NET_URI = "android.net.Uri";
        public const string JAVA_CLASS_IO_FILE = "java.io.File";
        public const string JAVA_CLASS_IO_OUTPUTSTREAM = "java.io.OutputStream";
        public const string JAVA_CLASS_IO_BYTEARRAYOUTPUTSTREAM = "java.io.ByteArrayOutputStream";
        public const string JAVA_CLASS_IO_BYTEARRAYINPUTSTREAM = "java.io.ByteArrayInputStream";
    }
}