// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public static class IOProcessHelper
    {
#if UNITY_ANDROID
        /// <summary>
        /// Get the path of pictures folder in external storage in Android.
        /// </summary>
        /// <returns>Return Pictures folder directory if get successfully, otherwise return empty string</returns>
        public static string GetAndroidExternalStoragePicturesDirectory()
        {
            Debug.Log("GetAndroidExternalStoragePicturesDirectory called");

            string path = string.Empty;

            try
            {
                // Init the class in java code
                using (AndroidJavaClass environment =
                       new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_ENVIRONMENT))
                {
                    // Call static method to get the path of pictures folder in external storage
                    path = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory",
                            environment.GetStatic<string>("DIRECTORY_PICTURES"))
                        .Call<string>("getAbsolutePath");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error on getting the path of pictures folder in external storage in Android: {e}");
            }

            Debug.Log($"Get path in GetAndroidExternalStoragePicturesDirectory: {path}");

            return path;
        }

        public static string GetAndroidPrimaryExternalStorageDirectory()
        {
            Debug.Log("GetAndroidPrimaryExternalStorageDirectory called");

            string path = string.Empty;

            try
            {
                using (AndroidJavaClass environment =
                       new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_ENVIRONMENT))
                {
                    path = environment.CallStatic<AndroidJavaObject>("getExternalStorageDirectory")
                        .Call<string>("getAbsolutePath");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error on getting the path of pictures folder in external storage in Android: {e}");
            }

            Debug.Log($"Get path in GetAndroidPrimaryExternalStorageDirectory: {path}");

            return path;
        }

        public static Dictionary<ExternalStorageType, string> GetAndroidAllExternalStorageDirectory()
        {
            Debug.Log("GetAndroidAllExternalStorageDirectory called");

            // Get all available external file directories (emulated or removable (aka sd card))
            AndroidJavaObject[] externalFilesDirectories =
                AndroidProcessHelper.Activity.Call<AndroidJavaObject[]>("getExternalFilesDirs", (object)null);

            var result = new Dictionary<ExternalStorageType, string>();
            using (var environment = new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_ENVIRONMENT))
            {
                foreach (var item in externalFilesDirectories)
                {
                    string directory = item.Call<string>("getAbsolutePath");
                    Debug.Log($"Find the path in GetAndroidExternalStorageDirectory: {directory}");

                    if (environment.CallStatic<bool>("isExternalStorageRemovable", item))
                    {
                        result.Add(ExternalStorageType.Removable, directory);
                    }
                    else if (environment.CallStatic<bool>("isExternalStorageEmulated", item))
                    {
                        result.Add(ExternalStorageType.Emulated, directory);
                    }
                }
            }

            return result;
        }

        public enum ExternalStorageType
        {
            Removable,
            Emulated
        }
#endif

        public static Task SaveByteDataToDisk(byte[] bytes, string saveDirectory, string fileNameWithFileExtension)
        {
            Directory.CreateDirectory(saveDirectory);

            try
            {
                string fullPath = Path.Combine(saveDirectory, fileNameWithFileExtension);
                System.IO.File.WriteAllBytes(fullPath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error on writing byte data to disk: {e}");
            }

            return Task.CompletedTask;
        }

        public static byte[] OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("File not exist: " + path);
                return null;
            }

            byte[] data = File.ReadAllBytes(path);
            if (data.Length == 0)
            {
                Debug.LogError("File is empty: " + path);
                return null;
            }

            return data;
        }

        public static byte[] OpenJpeg(string path)
        {
            byte[] data = OpenFile(path);

            if (data == null)
            {
                Debug.LogError("Open Jpeg error");
                return null;
            }

            if (data[0] != 0xFF || data[1] != 0xD8)
            {
                Debug.LogError("File is not JPEG: " + path);
                return null;
            }

            return data;
        }
    }
}