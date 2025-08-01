#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediaProjection.Services
{
    public class ServiceContainer : MonoBehaviour
    {
        [SerializeField] private bool enableImageProcessing = true;
        [SerializeField] private bool enableWebRtc = false;
        [SerializeField] private bool enableVideoRecording = true;

        private AndroidJavaObject? mediaProjectionCallback;
        private AndroidJavaObject? mediaProjectionManager;
        private AndroidJavaObject? imageProcessManager;
        private AndroidJavaObject? bitmapSaver;

        private MediaProjectionService? mediaProjectionService = null;
        private VideoRecordingService? videoRecordingService = null;


        private string imageSaverFilenamePrefix = "";

        public IMediaProjectionService MediaProjectionService
        {
            get
            {
                mediaProjectionService ??= new MediaProjectionService(imageProcessManager);
                return mediaProjectionService;
            }
        }

        public IVideoRecordingService VideoRecordingService
        {
            get
            {
                if (enableVideoRecording)
                {
                    videoRecordingService ??= new VideoRecordingService();
                    return videoRecordingService;
                }
                throw new InvalidOperationException("Video recording is not enabled. Set enableVideoRecording to true.");
            }
        }

        public AndroidJavaObject? MediaProjectionManager => mediaProjectionManager;
        public event Action<AndroidJavaObject?>? MediaProjectionManagerChanged;


        public void RequestImageSaver(string filenamePrefix)
        {
            imageSaverFilenamePrefix = filenamePrefix;

            if (bitmapSaver != null || string.IsNullOrEmpty(filenamePrefix) || imageProcessManager == null)
            {
                return;
            }

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    bitmapSaver = new AndroidJavaObject(
                            "com.t34400.mediaprojectionlib.io.BitmapSaver", 
                            activity,
                            imageProcessManager,
                            filenamePrefix);
                }
            }
        }

        private void OnEnable()
        {
            mediaProjectionCallback = new AndroidJavaObject("com.t34400.mediaprojectionlib.core.MediaProjectionCallback");

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    var mediaProjectionManagerClassName = 
                        enableWebRtc ? "com.t34400.mediaprojectionlib.webrtc.WebRtcMediaProjectionManager" 
                            : "com.t34400.mediaprojectionlib.core.MediaProjectionManager";
                    Debug.Log("MediaProjectionManagerClassName: " + mediaProjectionManagerClassName);
                    
                    mediaProjectionManager = new AndroidJavaObject(
                            mediaProjectionManagerClassName, 
                            activity, 
                            mediaProjectionCallback);

                    if (enableImageProcessing)
                    {
                        imageProcessManager = new AndroidJavaObject(
                                "com.t34400.mediaprojectionlib.core.ScreenImageProcessManager", 
                                mediaProjectionManager);
                    }
                }
            }
            
            mediaProjectionService?.SetMediaProjectionManager(imageProcessManager);

            RequestImageSaver(imageSaverFilenamePrefix);

            MediaProjectionManagerChanged?.Invoke(mediaProjectionManager);
        }

        private void OnDisable()
        {
            Debug.Log("ServiceContainer.OnDisable");

            mediaProjectionService?.SetMediaProjectionManager(null);

            Debug.Log("ServiceContainer.OnDisable: Disposing services");

            bitmapSaver?.Dispose();
            bitmapSaver = null;

            imageProcessManager?.Dispose();
            imageProcessManager = null;

            Debug.Log("ServiceContainer.OnDisable: Stopping media projection");

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    mediaProjectionManager?.Call("stopMediaProjection", activity);
                }
            }

            Debug.Log("ServiceContainer.OnDisable: Disposing media projection manager");

            MediaProjectionManagerChanged?.Invoke(null);

            Debug.Log("ServiceContainer.OnDisable: MediaProjectionManagerChanged invoked");

            mediaProjectionManager?.Dispose();
            mediaProjectionManager = null;

            mediaProjectionCallback?.Dispose();
            mediaProjectionCallback = null;

            Debug.Log("ServiceContainer.OnDisable: Disposed");
        }

        private void OnDestroy()
        {

            videoRecordingService?.Dispose();
            videoRecordingService = null;

            bitmapSaver?.Dispose();
            bitmapSaver = null;

            imageProcessManager?.Dispose();
            imageProcessManager = null;

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    mediaProjectionManager?.Call("stopMediaProjection", activity);
                }
            }
            mediaProjectionService?.Dispose();
            mediaProjectionService = null;
        }
    }
}