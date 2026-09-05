// Copyright HTC Corporation All Rights Reserved.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public static class TextureProcessHelper
    {
        private const RenderTextureFormat Photo360RenderTextureFormatDefault = RenderTextureFormat.ARGB32;
        private const int Photo360RenderTextureCubemapFaceMaskDefault = 63; // All faces

        /// <summary>
        /// Convert the RenderTexture to Texture2D
        /// </summary>
        /// <param name="renderTexture">The RenderTexture source</param>
        /// <param name="texture2D">The Texture2D</param>
        public static void RenderTexture2Texture2D(
            in RenderTexture renderTexture,
            out Texture2D texture2D)
        {
            RenderTexture.active = renderTexture;

            texture2D = new Texture2D(renderTexture.width, renderTexture.height);
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = null;
        }

        /// <summary>
        /// Convert the Texture2D to image general format (PNG or JPG) byte array
        /// </summary>
        /// <param name="texture2D">The Texture2D source</param>
        /// <param name="imageOutputFormat">The image format you want to convert to</param>
        /// <returns>The byte array of image general format (PNG or JPG)</returns>
        public static byte[] Texture2DToByteArray(
            in Texture2D texture2D,
            in PictureOutputFormat imageOutputFormat)
        {
            byte[] byteArray;
            switch (imageOutputFormat)
            {
                case PictureOutputFormat.JPG:
                    byteArray = texture2D.EncodeToJPG();
                    break;
                case PictureOutputFormat.PNG:
                    byteArray = texture2D.EncodeToPNG();
                    break;
                default:
                    Debug.LogWarning("The output format is not supported, will use PNG as default.");
                    byteArray = texture2D.EncodeToPNG();
                    break;
            }

            return byteArray;
        }

#if UNITY_ANDROID
        /// <summary>
        /// Convert the Unity Texture2D to Android Bitmap.
        /// </summary>
        /// <param name="texture">Unity Texture2D</param>
        /// <param name="imageOutputFormat">The image format you want to convert to</param>
        /// <returns>Android Bitmap</returns>
        public static AndroidJavaObject Texture2DToAndroidBitmap(
            in Texture2D texture,
            in PictureOutputFormat imageOutputFormat)
        {
            byte[] encodedTexture;

            switch (imageOutputFormat)
            {
                case PictureOutputFormat.JPG:
                {
                    encodedTexture = texture.EncodeToJPG();
                    if (texture.width / texture.height == 2)
                    {
                        encodedTexture = InsertGPanoXmpInJpg(encodedTexture, texture.width, texture.height);
                    }
                }
                    break;
                case PictureOutputFormat.PNG:
                {
                    encodedTexture = texture.EncodeToPNG();
                }
                    break;
                default:
                {
                    Debug.LogError("The output format is not supported, will use PNG as default.");
                    encodedTexture = texture.EncodeToPNG();
                }
                    break;
            }

            using (var bitmapFactory =
                   new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_GRAPHICS_BITMAP_FACTORY))
            {
                return bitmapFactory.CallStatic<AndroidJavaObject>(
                    "decodeByteArray",
                    encodedTexture,
                    0,
                    encodedTexture.Length);
            }
        }

        /// <summary>
        /// Save the Texture2D data to the Android gallery. Please note that this function is <b>only supported on
        /// Android API levels less than 29</b> and has some limitations for saving the image:
        /// <b><para>
        /// 1) It does not support saving the image to the specific album.
        /// </para>
        /// 2) It only supports saving the image to JPG format.
        /// </b>
        /// </summary>
        /// <param name="texture2D">The Texture2D you want to save to</param>
        /// <param name="imageTitle">The image name without file extension</param>
        /// <param name="imageDescription">The image description</param>
        /// <returns>Return the URL of the newly created image if an image is saved successfully. Otherwise, return null.</returns>
        public static string SaveImageToAndroidGallery(
            in Texture2D texture2D,
            in string imageTitle,
            in string imageDescription)
        {
            const int apiLevelOfAndroidQ = 29;
            int buildVersionSdkInt = 0;
            using (var buildVersionClass = new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_BUILD_VERSION))
            {
                buildVersionSdkInt = buildVersionClass.GetStatic<int>("SDK_INT");
            }

            if (buildVersionSdkInt >= apiLevelOfAndroidQ)
            {
                Debug.LogWarning(
                    "This function is only support on Android API level < 29. Will ignore this operation.");
                return null;
            }

            if (texture2D == null || string.IsNullOrEmpty(imageTitle) || string.IsNullOrEmpty(imageDescription))
            {
                Debug.LogError("The input data is not valid. Please check the data and try again.");
                return null;
            }

            using (var mediaClass =
                   new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_MEDIA_STORE_IMAGE_MEDIA))
            using (var contentResolver =
                   AndroidProcessHelper.Activity.Call<AndroidJavaObject>("getContentResolver"))
            {
                // Limitation: The Android API "insertImage" will always save
                // the image to the default album and save the image as JPG format.
                AndroidJavaObject image = Texture2DToAndroidBitmap(texture2D, PictureOutputFormat.JPG);
                return mediaClass.CallStatic<string>(
                    "insertImage",
                    contentResolver,
                    image,
                    imageTitle,
                    imageDescription);
            }
        }

        /// <summary>
        /// Save the image byte data to the Android gallery.
        /// The byte data should be encoded to the format that is noted in the enum "PictureOutputFormat."
        /// Please note this function <b>needs to be granted the permission of "WRITE_EXTERNAL_STORAGE"
        /// if Android API levels are less than 29.</b>
        /// </summary>
        /// <param name="imageData">The image data which is encoded</param>
        /// <param name="imageNameWithFileExtension">The image name included its extension</param>
        /// <param name="imageAlbumName">The album name that the photo will be saved to</param>
        /// <returns>The result of image saving. True if saved successfully. Otherwise, return false.</returns>
        public static bool SaveImageToAndroidGallery(
            in byte[] imageData,
            in string imageNameWithFileExtension,
            in string imageAlbumName = "")
        {
            const string mediaColumnsTitle = "title";
            const string mediaColumnsDisplayName = "_display_name";
            const string mediaColumnsMimeType = "mime_type";
            const string mediaColumnsRelativePath = "relative_path";
            const string mediaColumnsIsPending = "is_pending";
            const string mediaColumnsDate = "_data";
            const int androidQ = 29;

            int buildVersionSdkInt;
            using (var buildVersionClass = new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_BUILD_VERSION))
            {
                buildVersionSdkInt = buildVersionClass.GetStatic<int>("SDK_INT");
            }

            Debug.Log("You are using Android API level: " + buildVersionSdkInt);

            if (imageData == null || string.IsNullOrEmpty(imageNameWithFileExtension))
            {
                Debug.LogError(
                    "The input data is not valid. Will ignore this operation and please check the data and try again.");
                return false;
            }

            AndroidJavaObject currentActivity = AndroidProcessHelper.Activity;

            int fileExtensionIndex = imageNameWithFileExtension.LastIndexOf(".", StringComparison.Ordinal);
            if (fileExtensionIndex <= 0)
            {
                Debug.LogError(
                    "The file extension get failed. Will ignore this operation and please check the data and try again.");
                return false;
            }

            string imageNameWithoutFileExtension = imageNameWithFileExtension.Substring(0, fileExtensionIndex);
            string fileExtension = imageNameWithFileExtension.Substring(fileExtensionIndex + 1).ToUpper();

            var mimeType = string.Empty;
            foreach (string item in Enum.GetNames(typeof(PictureOutputFormat)))
            {
                if (fileExtension != item)
                {
                    continue;
                }

                mimeType = $"image/{item}";
                break;
            }

            if (string.IsNullOrEmpty(mimeType))
            {
                Debug.LogError(
                    "The output format is not supported. Will ignore this operation and please check the data and try again.");
                return false;
            }

            using (var contentValues = new AndroidJavaObject(AndroidProcessHelper.ANDROID_CLASS_CONTENT_VALUES))
            {
                contentValues.Call("put", mediaColumnsTitle, imageNameWithoutFileExtension);
                contentValues.Call("put", mediaColumnsDisplayName, imageNameWithFileExtension);
                contentValues.Call("put", mediaColumnsMimeType, mimeType);

                using (var contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver"))
                {
                    if (contentResolver == null)
                    {
                        Debug.LogError("Cannot get the Android content resolver.");
                        return false;
                    }

                    var mediaClass =
                        new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_MEDIA_STORE_IMAGE_MEDIA);
                    using (var externalContentUri = mediaClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
                    {
                        mediaClass.Dispose();
                        if (externalContentUri == null)
                        {
                            Debug.LogError("The Android uri of external content is not valid.");
                            return false;
                        }

                        using (var androidEnvClass =
                               new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_OS_ENVIRONMENT))
                        {
                            var imageDirectory = androidEnvClass.GetStatic<string>("DIRECTORY_PICTURES");
                            if (string.IsNullOrEmpty(imageDirectory))
                            {
                                Debug.LogError("The directory for saving the image is not valid.");
                                return false;
                            }

                            string relativePath = string.IsNullOrEmpty(imageAlbumName)
                                ? imageDirectory
                                : Path.Combine(imageDirectory, imageAlbumName);

                            if (buildVersionSdkInt >= androidQ)
                            {
                                // New method of Android system (API level >= 29)
                                Debug.Log("Save image to Android gallery by new method.");

                                contentValues.Call("put", mediaColumnsRelativePath, relativePath);

                                // An error will occur on some brand of Android system.
                                // To avoid an error, the part that adds "is_pending" is placed in the try-catch block.
                                try
                                {
                                    contentValues.Call("put", mediaColumnsIsPending, 1);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning($"Error on calling put \"is_pending\": {e}.");
                                }

                                using (var insertImageUri =
                                       contentResolver.Call<AndroidJavaObject>("insert", externalContentUri,
                                           contentValues))
                                {
                                    if (insertImageUri == null)
                                    {
                                        Debug.LogError("Insert the image to external content uri failed.");
                                        return false;
                                    }

                                    try
                                    {
                                        using (var imageFileUri =
                                               contentResolver.Call<AndroidJavaObject>("openOutputStream",
                                                   insertImageUri))
                                        {
                                            imageFileUri.Call("write", imageData);
                                            imageFileUri.Call("flush");
                                            imageFileUri.Call("close");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"The image file URI could not be opened: {e}");
                                        return false;
                                    }

                                    // An error will occur on some brand of Android system.
                                    // To avoid an error, the part that adds "is_pending" is placed in the try-catch block.
                                    try
                                    {
                                        contentValues.Call("put", mediaColumnsIsPending, 0);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"Error on calling put \"is_pending\": {e}.");
                                    }

                                    contentResolver.Call<int>("update", insertImageUri, contentValues, null, null);
                                }
                            }
                            else
                            {
                                // Old method of Android system (API level < 29)
                                Debug.Log("Save image to Android gallery by old method.");

                                try
                                {
                                    var imageDirectoryFile = androidEnvClass.CallStatic<AndroidJavaObject>(
                                        "getExternalStoragePublicDirectory",
                                        imageDirectory);
                                    var imageDirectoryWithAlbumFile = new AndroidJavaObject(
                                        AndroidProcessHelper.JAVA_CLASS_IO_FILE,
                                        imageDirectoryFile, imageAlbumName);
                                    imageDirectoryWithAlbumFile.Call<bool>("mkdirs");
                                    var imageFile = new AndroidJavaObject(
                                        AndroidProcessHelper.JAVA_CLASS_IO_FILE,
                                        imageDirectoryWithAlbumFile, imageNameWithFileExtension);
                                    var imageFileAbsolutePath = imageFile.Call<string>("getAbsolutePath");

                                    var fileSourceStream = new MemoryStream(imageData);
                                    FileStream fileOutputStream = System.IO.File.Create(imageFileAbsolutePath);
                                    fileSourceStream.CopyTo(fileOutputStream);
                                    fileOutputStream.Close();
                                    fileSourceStream.Close();

                                    contentValues.Call("put", mediaColumnsDate, imageFileAbsolutePath);
                                    contentResolver.Call<AndroidJavaObject>("insert", externalContentUri,
                                        contentValues);

                                    var androidContentIntentClass =
                                        new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_CONTENT_INTENT);
                                    var mediaScanIntent = new AndroidJavaObject(
                                        AndroidProcessHelper.ANDROID_CLASS_CONTENT_INTENT,
                                        androidContentIntentClass.GetStatic<string>("ACTION_MEDIA_SCANNER_SCAN_FILE"));
                                    var androidUriClass =
                                        new AndroidJavaClass(AndroidProcessHelper.ANDROID_CLASS_NET_URI);
                                    var imageFileUri =
                                        androidUriClass.CallStatic<AndroidJavaObject>("fromFile", imageFile);
                                    mediaScanIntent.Call<AndroidJavaObject>("setData", imageFileUri);
                                    currentActivity.Call("sendBroadcast", mediaScanIntent);

                                    // Release all the resources related to Android/Java object
                                    imageDirectoryFile.Dispose();
                                    imageDirectoryWithAlbumFile.Dispose();
                                    imageFile.Dispose();
                                    androidContentIntentClass.Dispose();
                                    mediaScanIntent.Dispose();
                                    androidUriClass.Dispose();
                                    imageFileUri.Dispose();
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError("Error on saving the image to external storage: " + e);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
#endif

        /// <summary>
        /// Capture the 360 photo via the input camera and then output the result to the input RenderTexture.
        /// For capture the monoscopic 360 photo, just assign the leftEyeCubemapResult is enough.
        /// </summary>
        /// <param name="camera">The camera that you want to capture 360 photo source</param>
        /// <param name="panoramaType">The 360 photo type</param>
        /// <param name="panoramicPhotoResult">Save panoramic photo result</param>
        /// <param name="leftEyeCubemapResult">Save left eye cubemap result</param>
        /// <param name="rightEyeCubemapResult">Save right eye cubemap result</param>
        public static void Capture360RenderTexture(
            in Camera camera,
            in PanoramaType panoramaType,
            in RenderTexture panoramicPhotoResult,
            in RenderTexture leftEyeCubemapResult,
            in RenderTexture rightEyeCubemapResult = null)
        {
            if (panoramaType == PanoramaType.Stereoscopic &&
                (leftEyeCubemapResult == null || rightEyeCubemapResult == null))
            {
                Debug.LogError(
                    "PanoramaType.Stereoscopic mode need left and right eye RenderTexture but one of them or both is null.");
                return;
            }

            if (panoramaType == PanoramaType.Monoscopic && leftEyeCubemapResult == null)
            {
                Debug.LogError("PanoramaType.Monoscopic mode need left eye RenderTexture but it is null.");
                return;
            }

            switch (panoramaType)
            {
                case PanoramaType.Stereoscopic:
                {
                    # region 3D 360 photo processing (stereoscopic)

                    try
                    {
                        // output the camera view to RenderTexture
                        camera.RenderToCubemap(leftEyeCubemapResult, Photo360RenderTextureCubemapFaceMaskDefault,
                            Camera.MonoOrStereoscopicEye.Left);
                        // for the 360 photo, we need to do the equirectangular projection in order to
                        // convert the 360 photo (cubemap) to panoramic photo
                        leftEyeCubemapResult.ConvertToEquirect(panoramicPhotoResult, Camera.MonoOrStereoscopicEye.Left);

                        camera.RenderToCubemap(rightEyeCubemapResult, Photo360RenderTextureCubemapFaceMaskDefault,
                            Camera.MonoOrStereoscopicEye.Right);
                        rightEyeCubemapResult.ConvertToEquirect(panoramicPhotoResult,
                            Camera.MonoOrStereoscopicEye.Right);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to capture 360 photo, error message: {e.Message}.");
                    }

                    #endregion
                }
                    break;
                case PanoramaType.Monoscopic:
                {
                    #region General 3D photo processing (monoscopic)

                    try
                    {
                        // output the camera view to RenderTexture
                        camera.RenderToCubemap(leftEyeCubemapResult);
                        // for the 360 photo, we need to do the equirectangular projection in order to
                        // convert the 360 photo (cubemap) to panoramic photo
                        leftEyeCubemapResult.ConvertToEquirect(panoramicPhotoResult);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to capture 360 photo, error message: {e.Message}.");
                    }

                    #endregion
                }
                    break;
                default:
                {
                    Debug.LogError("The panorama type is not supported.");
                }
                    break;
            }
        }

        /// <summary>
        /// Capture the 360 photo via the input camera.
        /// </summary>
        /// <param name="camera">The camera that you want to capture 360 photo source</param>
        /// <param name="resolution">The 360 photo resolution. The value must power two</param>
        /// <param name="panoramaType">The panorama type of the 360 photo</param>
        /// <returns>The 360 photo</returns>
        public static RenderTexture Capture360RenderTexture(
            in Camera camera,
            in int resolution,
            in PanoramaType panoramaType)
        {
            const int deepBufferDefault = 0;

            if (!IsPowerOfTwo((ulong)resolution))
            {
                Debug.LogError("The resolution of the capture 360 photo should be power of two.");
                return null;
            }

            // create a Temporary RenderTexture for saving 360 photo after equirectangular projection
            var capture360ResultEquirect = RenderTexture.GetTemporary(
                resolution,
                panoramaType == PanoramaType.Stereoscopic ? resolution : resolution / 2,
                deepBufferDefault,
                Photo360RenderTextureFormatDefault);
            capture360ResultEquirect.dimension = TextureDimension.Tex2D;
            capture360ResultEquirect.name = "Panoramic photo";

            switch (panoramaType)
            {
                case PanoramaType.Stereoscopic:
                {
                    # region 3D 360 photo processing (stereoscopic)

                    // left eye
                    try
                    {
                        // create a Temporary RenderTexture for saving the 360 capture photo (stereoscopic)
                        var capture360ResultLeft = RenderTexture.GetTemporary(
                            resolution, // TODO: the resolution of the capture photo should be set by user
                            resolution,
                            deepBufferDefault,
                            Photo360RenderTextureFormatDefault);
                        capture360ResultLeft.dimension = TextureDimension.Cube;
                        capture360ResultLeft.name = "Left eye cubemap";

                        // output the camera RenderTexture to capture360Result (Temporary RenderTexture)
                        camera.RenderToCubemap(capture360ResultLeft, Photo360RenderTextureCubemapFaceMaskDefault,
                            Camera.MonoOrStereoscopicEye.Left);

                        // for the 360 photo, we need to do the equirectangular projection in order to
                        // convert the 360 photo (cubemap) to panoramic photo
                        capture360ResultLeft.ConvertToEquirect(capture360ResultEquirect,
                            Camera.MonoOrStereoscopicEye.Left);

                        RenderTexture.ReleaseTemporary(capture360ResultLeft);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Failed to create a Temporary RenderTexture for saving the 360 photo (left eye): {e.Message}.");
                        RenderTexture.ReleaseTemporary(capture360ResultEquirect);
                        return null;
                    }

                    // right eye
                    try
                    {
                        var capture360ResultRight = RenderTexture.GetTemporary(
                            resolution, // TODO: the resolution of the capture photo should be set by user
                            resolution,
                            deepBufferDefault,
                            Photo360RenderTextureFormatDefault);
                        capture360ResultRight.dimension = TextureDimension.Cube;
                        capture360ResultRight.name = "Right eye cubemap";

                        camera.RenderToCubemap(capture360ResultRight, Photo360RenderTextureCubemapFaceMaskDefault,
                            Camera.MonoOrStereoscopicEye.Right);

                        capture360ResultRight.ConvertToEquirect(capture360ResultEquirect,
                            Camera.MonoOrStereoscopicEye.Right);

                        RenderTexture.ReleaseTemporary(capture360ResultRight);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Failed to create a Temporary RenderTexture for saving the 360 photo (right eye): {e.Message}.");
                        RenderTexture.ReleaseTemporary(capture360ResultEquirect);
                        return null;
                    }

                    #endregion
                }
                    break;
                case PanoramaType.Monoscopic:
                {
                    #region General 3D photo processing (monoscopic)

                    try
                    {
                        // create a Temporary RenderTexture for saving the 360 capture photo (monoscopic)
                        var capture360Result = RenderTexture.GetTemporary(
                            resolution, // TODO: the resolution of the capture photo should be set by user
                            resolution,
                            deepBufferDefault,
                            Photo360RenderTextureFormatDefault);
                        capture360Result.dimension = TextureDimension.Cube;
                        capture360Result.name = "CubeMap";

                        // output the camera RenderTexture to capture360Result (Temporary RenderTexture)
                        camera.RenderToCubemap(capture360Result);

                        // for the 360 photo, we need to do the equirectangular projection in order to
                        // convert the 360 photo (cubemap) to panoramic photo
                        capture360Result.ConvertToEquirect(capture360ResultEquirect);

                        RenderTexture.ReleaseTemporary(capture360Result);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to capture 360 photo, error message: {e.Message}.");
                        RenderTexture.ReleaseTemporary(capture360ResultEquirect);
                        return null;
                    }

                    #endregion
                }
                    break;
                default:
                {
                    Debug.LogWarning("The panorama type is not supported. Will ignore this operation.");
                    RenderTexture.ReleaseTemporary(capture360ResultEquirect);
                }
                    return null;
            }

            return capture360ResultEquirect;

            // Input resolution validation
            bool IsPowerOfTwo(ulong x) => (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Convert the RenderTexture to image general format (such as PNG or JPG) and then save it on the disk
        /// </summary>
        /// <param name="imageAlbumName">The album that the photo will be saved to</param>
        /// <param name="imageNameWithoutFileExtension">The image file name without file extension</param>
        /// <param name="imageOutputFormat">The image format you want to convert to</param>
        /// <param name="sourceRenderTexture">The RenderTexture source</param>
        /// <param name="yawRotation">The camera rotation on the yaw axis</param>
        /// <param name="saveDirectory">The directory that the image is saved to. This only affects and applies to non-Android platforms.</param>
        public static void SaveRenderTextureToDisk(
            string imageAlbumName,
            string imageNameWithoutFileExtension,
            PictureOutputFormat imageOutputFormat,
            RenderTexture sourceRenderTexture,
            float yawRotation = 0,
            string saveDirectory = "")
        {
            if (string.IsNullOrEmpty(imageNameWithoutFileExtension) || sourceRenderTexture == null)
            {
                Debug.LogError("The input data is not valid. Please check the data and try again.");
                return;
            }

#if !UNITY_ANDROID || UNITY_EDITOR
            if (string.IsNullOrEmpty(imageAlbumName))
            {
                Debug.LogWarning("The name of image album is null, will use the \"Snapshot\" as default album name.");
                saveDirectory = "Snapshot";
            }

            if (string.IsNullOrEmpty(saveDirectory))
            {
                Debug.LogWarning(
                    "The save directory is null, will use the \"Application.persistentDataPath\" as default path.");
                saveDirectory = Application.persistentDataPath;
            }
#endif

            RenderTexture referenceRenderTexture = RenderTexture.GetTemporary(
                sourceRenderTexture.width,
                sourceRenderTexture.height,
                sourceRenderTexture.depth,
                sourceRenderTexture.format);
            TextureWrapMode originalTextureWrapMode = sourceRenderTexture.wrapMode;
            sourceRenderTexture.wrapMode = TextureWrapMode.Repeat;
            Graphics.Blit(
                sourceRenderTexture,
                referenceRenderTexture,
                Vector2.one,
                new Vector2(yawRotation / 360f, 0));
            sourceRenderTexture.wrapMode = originalTextureWrapMode;

            // For saving photo to disk, we first convert the RenderTexture
            // to byte array which is already encoded to the format we want
            // (PNG or JPG), then we can save the byte array to disk.
            //
            // Because the above task is heavy, we prefer to do it by async-method.
            // However, the async-method is only supported on Unity 2021.3 or newer.
            // For the older version, we only can do it by sync-method.
#if !UNITY_2021_3_OR_NEWER
			#region Sync save image to disk

			RenderTexture2Texture2D(referenceRenderTexture, out Texture2D texture2D);
			byte[] byteArray = Texture2DToByteArray(texture2D, imageOutputFormat);

			string imageNameWithFileExtension;
			switch (imageOutputFormat)
			{
				case PictureOutputFormat.PNG:
				case PictureOutputFormat.JPG:
				{
					imageNameWithFileExtension =
						$"{imageNameWithoutFileExtension}.{imageOutputFormat.ToString().ToLower()}";
				}
					break;
				default:
				{
					Debug.LogWarning("The output format is not supported, will use PNG as default.");
					imageNameWithFileExtension = $"{imageNameWithoutFileExtension}.png";
				}
					break;
			}

			// Write "encode data" to disk
#if UNITY_ANDROID && !UNITY_EDITOR
			bool isSaveSuccess = SaveImageToAndroidGallery(byteArray, imageNameWithFileExtension, imageAlbumName);
			Debug.Log(isSaveSuccess
				? $"Save photo in album {imageAlbumName} in gallery successfully"
				: $"Save photo in album {imageAlbumName} in gallery failed.");
#else
			string saveDirectoryWithAlbumName = Path.Combine(saveDirectory, imageAlbumName);
			_ = Task.Run(() =>
					IOProcessHelper.SaveByteDataToDisk(byteArray, saveDirectoryWithAlbumName,
						imageNameWithFileExtension))
				.ContinueWith(
					_ => { Debug.Log($"Save photo in disk {saveDirectoryWithAlbumName} finished."); });
#endif

			RenderTexture.ReleaseTemporary(referenceRenderTexture);

			#endregion

#else

            #region Async save image to disk

            AsyncGPUReadback.Request(referenceRenderTexture, 0, request =>
            {
                Debug.Log("Receive GPU signal, start to get data from a GPU resource.");

                if (request.hasError)
                {
                    Debug.LogError("Error on retrieves the data on GPU.");
                    RenderTexture.ReleaseTemporary(referenceRenderTexture);
                    return;
                }

                // Get native data from GPU
                NativeArray<Color32> rtColorNativeArray;
                try
                {
                    rtColorNativeArray = request.GetData<Color32>();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error on getting data from GPU: {e}.");
                    RenderTexture.ReleaseTemporary(referenceRenderTexture);
                    return;
                }

                // Encode to byte array from native data by CPU
                byte[] byteArray;
                try
                {
                    NativeArray<byte> byteNativeArray;
                    switch (imageOutputFormat)
                    {
                        case PictureOutputFormat.JPG:
                            byteNativeArray = ImageConversion.EncodeNativeArrayToJPG(
                                rtColorNativeArray,
                                GraphicsFormat.R8G8B8A8_UNorm,
                                (uint)referenceRenderTexture.width,
                                (uint)referenceRenderTexture.height);
                            byteArray = byteNativeArray.ToArray();
                            byteArray = InsertGPanoXmpInJpg(byteArray, referenceRenderTexture.width,
                                referenceRenderTexture.height);
                            break;
                        case PictureOutputFormat.PNG:
                            byteNativeArray = ImageConversion.EncodeNativeArrayToPNG(
                                rtColorNativeArray,
                                GraphicsFormat.R8G8B8A8_UNorm,
                                (uint)referenceRenderTexture.width,
                                (uint)referenceRenderTexture.height);
                            byteArray = byteNativeArray.ToArray();
                            break;
                        default:
                            Debug.LogWarning("The output format is not supported, will use PNG as default.");
                            byteNativeArray = ImageConversion.EncodeNativeArrayToPNG(
                                rtColorNativeArray,
                                GraphicsFormat.R8G8B8A8_UNorm,
                                (uint)referenceRenderTexture.width,
                                (uint)referenceRenderTexture.height);
                            byteArray = byteNativeArray.ToArray();
                            break;
                    }

                    // Once we get the encoded data (byte[]) by CPU, we can release the (byte) native array
                    byteNativeArray.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error on calling EncodeNativeArrayToJPG: {e}.");
                    return;
                }
                finally
                {
                    // Once we get the encoded data (byte[]) by CPU, we can release the (color) native array
                    // and RenderTexture
                    rtColorNativeArray.Dispose();
                    RenderTexture.ReleaseTemporary(referenceRenderTexture);
                }

                string imageNameWithFileExtension;
                switch (imageOutputFormat)
                {
                    case PictureOutputFormat.PNG:
                    case PictureOutputFormat.JPG:
                        imageNameWithFileExtension =
                            $"{imageNameWithoutFileExtension}.{imageOutputFormat.ToString().ToLower()}";
                        break;
                    default:
                        Debug.LogWarning("The output format is not supported, will use PNG as default.");
                        imageNameWithFileExtension = $"{imageNameWithoutFileExtension}.png";
                        break;
                }

                // Write "encode data" to disk
#if UNITY_ANDROID && !UNITY_EDITOR
				bool isSaveSuccess = SaveImageToAndroidGallery(byteArray, imageNameWithFileExtension, imageAlbumName);
				Debug.Log(isSaveSuccess
					? $"Save photo in album {imageAlbumName} in gallery successfully"
					: $"Save photo in album {imageAlbumName} in gallery failed.");
#else
                string saveDirectoryWithAlbumName = Path.Combine(saveDirectory, imageAlbumName);
                _ = Task.Run(() =>
                        IOProcessHelper.SaveByteDataToDisk(byteArray, saveDirectoryWithAlbumName,
                            imageNameWithFileExtension))
                    .ContinueWith(
                        _ => { Debug.Log($"Save photo in disk {saveDirectoryWithAlbumName} finished."); });
#endif
            });

            #endregion

#endif
        }

        /// <summary>
        /// Create pure color Texture2D for showing
        /// </summary>
        /// <param name="width">Int. Texture2D width.</param>
        /// <param name="height">Int. Texture2D height.</param>
        /// <param name="color">UnityEngine.Color. Texture2D color.</param>
        /// <returns>Texture2D</returns>
        public static Texture2D CreatePureTexture2DWithColor(in int width, in int height, in Color color)
        {
            var texture = new Texture2D(width, height);
            var pixels = Enumerable.Repeat(color, width * height).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Add the shader to the "Always Included Shaders" list in Unity graphics setting.
        /// </summary>
        /// <param name="shaderName">The name of the shader that you want to</param>
        public static void AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError(
                    $"Shader \"{shaderName}\" not found. Fail to add it to the \"Always Included Shaders\" list in graphics setting.");
                return;
            }

            var graphicsSettingsObj =
                AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var serializedObject = new SerializedObject(graphicsSettingsObj);
            var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
            bool hasShader = false;
            for (int i = 0; i < arrayProp.arraySize; ++i)
            {
                var arrayElem = arrayProp.GetArrayElementAtIndex(i);
                if (shader == arrayElem.objectReferenceValue)
                {
                    hasShader = true;
                    break;
                }
            }

            if (!hasShader)
            {
                int arrayIndex = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(arrayIndex);
                var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
                arrayElem.objectReferenceValue = shader;

                serializedObject.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();

                Debug.Log($"Add \"{shaderName}\" shader to the \"Always Included Shaders\" list in graphics setting");
            }
        }
#endif

        /// <summary>
        /// Insert panorama flag into the JPG meta data (Hardcode)
        /// </summary>
        /// <param name="jpeg">The JPG image represent by byte array</param>
        /// <param name="width">The JPG image width</param>
        /// <param name="height">The JPG image height</param>
        /// <returns></returns>
        public static byte[] InsertGPanoXmpInJpg(byte[] jpeg, int width, int height)
        {
            if (jpeg == null || jpeg.Length == 0)
            {
                Debug.LogError("Empty data.");
                return null;
            }

            if (jpeg[0] != 0xFF || jpeg[1] != 0xD8)
            {
                Debug.LogError("Not JPEG.");
                return null;
            }

            // Find the old jpeg's ap0 marker and insert xmp after it.
            int start = 2;
            {
                FindExistXmp(jpeg, start, out int idx, out int size);
                if (idx >= 0 && size >= 0)
                {
                    // We don't have ability to parse XMP data now.
                    Debug.LogWarning("We cannot create XMP now because it already exists.");
                    Debug.Log(Encoding.ASCII.GetString(jpeg, idx, size));
                    return null;
                }
            }

            {
                FindFirstApp0(jpeg, start, out int idx, out int size);
                if (idx >= start && size >= 0)
                    start = idx + 2 + size;
            }
            int oldStart = start;

            string xmpHeadStr = "http://ns.adobe.com/xap/1.0/ ";
            byte[] xmpHead = Encoding.ASCII.GetBytes(xmpHeadStr);
            xmpHead[xmpHead.Length - 1] = 0; // Make zero ending

            // Insert XMP after SOI
            string xmpDataStr =
                @"<?xpacket begin='BOM' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x='adobe:ns:meta/' x:xmptk='Image::ExifTool 12.47'>
  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'> 
    <rdf:Description rdf:about='' xmlns:GPano='http://ns.google.com/photos/1.0/panorama/'>
      <GPano:CroppedAreaImageHeightPixels>" + height + @"</GPano:CroppedAreaImageHeightPixels>
      <GPano:CroppedAreaImageWidthPixels>" + width + @"</GPano:CroppedAreaImageWidthPixels>
      <GPano:CroppedAreaLeftPixels>0</GPano:CroppedAreaLeftPixels>
      <GPano:CroppedAreaTopPixels>0</GPano:CroppedAreaTopPixels>
      <GPano:FullPanoHeightPixels>" + height + @"</GPano:FullPanoHeightPixels>
      <GPano:FullPanoWidthPixels>" + width + @"</GPano:FullPanoWidthPixels>
      <GPano:PoseHeadingDegrees>0.0</GPano:PoseHeadingDegrees>
      <GPano:PosePitchDegrees>0.0</GPano:PosePitchDegrees>
      <GPano:PoseRollDegrees>0.0</GPano:PoseRollDegrees>
      <GPano:ProjectionType>equirectangular</GPano:ProjectionType>
      <GPano:UsePanoramaViewer>True</GPano:UsePanoramaViewer>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta> 
<?xpacket end='w'?>";
            byte[] xmpData = Encoding.ASCII.GetBytes(xmpDataStr);
            // Replace BOM in xmpDataStr as 0xEF, 0xBB, 0xBF (UTF-8 BOM)
            xmpData[17] = 0xEF;
            xmpData[18] = 0xBB;
            xmpData[19] = 0xBF;

            byte[] newJpeg = new byte[jpeg.Length + xmpHead.Length + xmpData.Length + 4 /*marker and size*/];

            // Insert SOI
            System.Array.Copy(jpeg, 0, newJpeg, 0, start);
            // Insert APP1 marker
            newJpeg[start++] = 0xFF;
            newJpeg[start++] = 0xE1;
            // Insert APP1 marker size
            newJpeg[start++] = (byte)((xmpHead.Length + xmpData.Length + 2) >> 8);
            newJpeg[start++] = (byte)((xmpHead.Length + xmpData.Length + 2) & 0xFF);
            // Insert XMP head
            System.Array.Copy(xmpHead, 0, newJpeg, start, xmpHead.Length);
            start += xmpHead.Length;
            // Insert XMP data
            System.Array.Copy(xmpData, 0, newJpeg, start, xmpData.Length);
            start += xmpData.Length;
            // Insert JPEG data
            System.Array.Copy(jpeg, oldStart, newJpeg, start, jpeg.Length - oldStart);
            return newJpeg;
        }

        public static void FindExistXmp(byte[] jpeg, int start, out int idx, out int size)
        {
            string xmpHeadStr = "http://ns.adobe.com/xap/1.0/ ";
            byte[] xmpHead = Encoding.ASCII.GetBytes(xmpHeadStr);
            xmpHead[xmpHead.Length - 1] = 0; // Make zero ending
            idx = -1;
            size = 0;

            int n = jpeg.Length;
            for (int i = start; i < n;)
            {
                int mkSize = 0;
                if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xE1)
                {
                    idx = i + 4;
                    mkSize = (jpeg[i + 2] << 8) + jpeg[i + 3];
                }

                if (mkSize < xmpHead.Length)
                {
                    i += mkSize + 2;
                    continue;
                }

                int n2 = xmpHead.Length;
                bool isXmpHead = true;
                for (int j = 0; j < n2; j++)
                {
                    if (jpeg[i + 4 + j] != xmpHead[j])
                    {
                        isXmpHead = false;
                        break;
                    }
                }

                if (!isXmpHead)
                {
                    i += mkSize + 2;
                    continue;
                }

                idx = i + 4 + xmpHead.Length;
                size = mkSize;
                return;
            }
        }

        public static void FindFirstApp0(byte[] jpeg, int start, out int idx, out int size)
        {
            string jfifHeadStr = "JFIF ";
            byte[] jfifHead = Encoding.ASCII.GetBytes(jfifHeadStr);
            jfifHead[jfifHead.Length - 1] = 0; // Make zero ending
            idx = -1;
            size = 0;

            int n = jpeg.Length;
            for (int i = start; i < n;)
            {
                int mkSize = 0;
                if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xE0)
                {
                    mkSize = (jpeg[i + 2] << 8) + jpeg[i + 3];
                }

                if (mkSize < jfifHead.Length)
                {
                    i += mkSize + 2;
                    continue;
                }

                int n2 = jfifHead.Length;
                bool isJfifHead = true;
                for (int j = 0; j < n2; j++)
                {
                    if (jpeg[i + 4 + j] != jfifHead[j])
                    {
                        isJfifHead = false;
                        break;
                    }
                }

                if (!isJfifHead)
                {
                    i += mkSize + 2;
                    continue;
                }

                idx = i;
                size = mkSize;
                return;
            }
        }

        public enum PictureOutputFormat
        {
            PNG,
            JPG
        }

        public enum PanoramaType
        {
            Stereoscopic,
            Monoscopic
        }
    }
}