#nullable enable

using System;
using UnityEngine;

namespace MediaProjection.Services
{
    /// <summary>
    /// Unity service implementation for video recording using the MediaProjection + MediaCodec pipeline.
    /// This service communicates with the Android VideoRecordingService via JNI to perform
    /// hardware-accelerated screen recording.
    /// </summary>
    public class VideoRecordingService : IVideoRecordingService
    {
        private AndroidJavaObject? videoRecordingManager;
        private AndroidJavaObject? unityActivity;
        
        private RecordingState currentState = RecordingState.Idle;
        private string? currentOutputFile;
        private float recordingStartTime;
        
        // Events
        public event Action<RecordingState>? OnRecordingStateChanged;
        public event Action<string>? OnRecordingComplete;
        public event Action<string>? OnRecordingError;
        
        // Properties
        public RecordingState CurrentState => currentState;
        public bool IsRecording => currentState == RecordingState.Recording;
        public string? CurrentOutputFile => currentOutputFile;
        
        /// <summary>
        /// Initialize the video recording service
        /// </summary>
        public VideoRecordingService()
        {
            InitializeAndroidObjects();
        }
        
        /// <summary>
        /// Initialize Android JNI objects
        /// </summary>
        private void InitializeAndroidObjects()
        {
            try
            {
                // Get Unity activity
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                }
                
                if (unityActivity == null)
                {
                    Debug.LogError("VideoRecordingService: Failed to get Unity activity");
                    return;
                }
                
                // Create VideoRecordingManager instance
                videoRecordingManager = new AndroidJavaObject(
                    "com.t34400.mediaprojectionlib.recording.VideoRecordingManager",
                    unityActivity);
                
                // Setup callbacks using a callback proxy
                SetupCallbacks();
                
                Debug.Log("VideoRecordingService: Initialized successfully");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Failed to initialize - {e.Message}");
            }
        }
        
        /// <summary>
        /// Setup callbacks from Android to Unity
        /// </summary>
        private void SetupCallbacks()
        {
            if (videoRecordingManager == null) return;
            
            try
            {
                // Create callback proxy object that will handle Android -> Unity communication
                var callbackProxy = new VideoRecordingCallbackProxy(this);
                
                // Set the callback object on the Android side
                // Note: This requires implementing a callback interface on the Android side
                // For now, we'll use polling in Update() as a fallback
                Debug.Log("VideoRecordingService: Callbacks setup completed");
                
            }
            catch (Exception e)
            {
                Debug.LogWarning($"VideoRecordingService: Callback setup failed, will use polling - {e.Message}");
            }
        }
        
        /// <summary>
        /// Start video recording with the specified configuration
        /// </summary>
        public bool StartRecording(VideoRecordingConfig config)
        {
            if (videoRecordingManager == null || unityActivity == null)
            {
                Debug.LogError("VideoRecordingService: Not properly initialized");
                OnRecordingError?.Invoke("Service not initialized");
                return false;
            }
            
            if (currentState != RecordingState.Idle)
            {
                Debug.LogWarning($"VideoRecordingService: Cannot start recording in state {currentState}");
                return false;
            }
            
            try
            {
                Debug.Log($"VideoRecordingService: Starting recording with config - " +
                         $"Bitrate: {config.videoBitrate}, FPS: {config.videoFrameRate}, " +
                         $"Format: {config.videoFormat}, Audio: {config.audioEnabled}");
                
                // Start the Android VideoRecordingService
                using (var serviceIntent = new AndroidJavaObject("android.content.Intent", 
                    unityActivity, 
                    new AndroidJavaClass("com.t34400.mediaprojectionlib.recording.VideoRecordingService")))
                {
                    serviceIntent.Call<AndroidJavaObject>("setAction", 
                        "com.t34400.mediaprojectionlib.recording.VideoRecordingService.ACTION_START_RECORDING");
                    
                    // Add recording configuration as intent extras
                    serviceIntent.Call<AndroidJavaObject>("putExtra", "extra_video_bitrate", config.videoBitrate);
                    serviceIntent.Call<AndroidJavaObject>("putExtra", "extra_video_framerate", config.videoFrameRate);
                    serviceIntent.Call<AndroidJavaObject>("putExtra", "extra_output_directory", config.outputDirectory);
                    serviceIntent.Call<AndroidJavaObject>("putExtra", "extra_max_duration_ms", config.maxRecordingDurationMs);
                    
                    // Start the foreground service
                    using (var contextCompat = new AndroidJavaClass("androidx.core.content.ContextCompat"))
                    {
                        contextCompat.CallStatic<AndroidJavaObject>("startForegroundService", unityActivity, serviceIntent);
                    }
                }
                
                // Update local state
                ChangeState(RecordingState.Preparing);
                recordingStartTime = Time.time;
                currentOutputFile = null;
                
                return true;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Failed to start recording - {e.Message}");
                OnRecordingError?.Invoke($"Failed to start recording: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stop the current recording
        /// </summary>
        public bool StopRecording()
        {
            if (unityActivity == null)
            {
                Debug.LogError("VideoRecordingService: Not properly initialized");
                return false;
            }
            
            if (currentState != RecordingState.Recording)
            {
                Debug.LogWarning($"VideoRecordingService: Cannot stop recording in state {currentState}");
                return false;
            }
            
            try
            {
                Debug.Log("VideoRecordingService: Stopping recording");
                
                // Send stop command to Android service
                using (var serviceIntent = new AndroidJavaObject("android.content.Intent", 
                    unityActivity, 
                    new AndroidJavaClass("com.t34400.mediaprojectionlib.recording.VideoRecordingService")))
                {
                    serviceIntent.Call<AndroidJavaObject>("setAction", 
                        "com.t34400.mediaprojectionlib.recording.VideoRecordingService.ACTION_STOP_RECORDING");
                    
                    unityActivity.Call<AndroidJavaObject>("startService", serviceIntent);
                }
                
                // Update local state
                ChangeState(RecordingState.Stopping);
                
                return true;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Failed to stop recording - {e.Message}");
                OnRecordingError?.Invoke($"Failed to stop recording: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get current recording status
        /// </summary>
        public RecordingStatus GetRecordingStatus()
        {
            var duration = currentState == RecordingState.Recording || currentState == RecordingState.Stopping 
                ? Time.time - recordingStartTime 
                : 0f;
                
            return new RecordingStatus
            {
                state = currentState,
                recordingDurationSeconds = duration,
                outputFilePath = currentOutputFile,
                errorMessage = currentState == RecordingState.Error ? "Recording error occurred" : null
            };
        }
        
        /// <summary>
        /// Check if video recording is supported on this device
        /// </summary>
        public bool IsVideoRecordingSupported()
        {
            try
            {
                // Check Android version (MediaProjection requires API 21+)
                using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
                    if (sdkInt < 21)
                    {
                        Debug.LogWarning("VideoRecordingService: MediaProjection requires Android API 21+");
                        return false;
                    }
                }
                
                // Check if MediaProjection service is available
                if (unityActivity != null)
                {
                    using (var context = unityActivity.Call<AndroidJavaObject>("getApplicationContext"))
                    {
                        var mediaProjectionService = context.Call<AndroidJavaObject>("getSystemService", "media_projection");
                        if (mediaProjectionService == null)
                        {
                            Debug.LogWarning("VideoRecordingService: MediaProjection service not available");
                            return false;
                        }
                    }
                }
                
                return true;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Error checking support - {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Change recording state and notify listeners
        /// </summary>
        private void ChangeState(RecordingState newState)
        {
            if (currentState == newState) return;
            
            var oldState = currentState;
            currentState = newState;
            
            Debug.Log($"VideoRecordingService: State changed {oldState} -> {newState}");
            OnRecordingStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// Handle recording completion (called from Android callback)
        /// </summary>
        internal void HandleRecordingComplete(string outputPath)
        {
            Debug.Log($"VideoRecordingService: Recording completed - {outputPath}");
            
            currentOutputFile = outputPath;
            ChangeState(RecordingState.Idle);
            OnRecordingComplete?.Invoke(outputPath);
        }
        
        /// <summary>
        /// Handle recording error (called from Android callback)
        /// </summary>
        internal void HandleRecordingError(string errorMessage)
        {
            Debug.LogError($"VideoRecordingService: Recording error - {errorMessage}");
            
            ChangeState(RecordingState.Error);
            OnRecordingError?.Invoke(errorMessage);
        }
        
        /// <summary>
        /// Handle recording state change (called from Android callback)
        /// </summary>
        internal void HandleStateChange(string stateString)
        {
            if (Enum.TryParse<RecordingState>(stateString, true, out var newState))
            {
                ChangeState(newState);
            }
            else
            {
                Debug.LogWarning($"VideoRecordingService: Unknown state received - {stateString}");
            }
        }
        
        /// <summary>
        /// Update method to poll recording status (fallback if callbacks don't work)
        /// This should be called from a MonoBehaviour's Update method
        /// </summary>
        public void UpdateRecordingStatus()
        {
            if (videoRecordingManager == null || currentState == RecordingState.Idle || currentState == RecordingState.Error)
                return;
            
            try
            {
                // Poll the Android service for status updates
                var stateString = videoRecordingManager.Call<string>("getRecordingState");
                if (!string.IsNullOrEmpty(stateString))
                {
                    HandleStateChange(stateString);
                }
                
                // Check for completed recording
                if (currentState == RecordingState.Idle && currentOutputFile == null)
                {
                    var outputPath = videoRecordingManager.Call<string>("getOutputFilePath");
                    if (!string.IsNullOrEmpty(outputPath))
                    {
                        HandleRecordingComplete(outputPath);
                    }
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Error polling status - {e.Message}");
            }
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Stop recording if in progress
                if (IsRecording)
                {
                    StopRecording();
                }
                
                // Stop the Android service
                if (unityActivity != null)
                {
                    using (var serviceIntent = new AndroidJavaObject("android.content.Intent", 
                        unityActivity, 
                        new AndroidJavaClass("com.t34400.mediaprojectionlib.recording.VideoRecordingService")))
                    {
                        serviceIntent.Call<AndroidJavaObject>("setAction", 
                            "com.t34400.mediaprojectionlib.recording.VideoRecordingService.ACTION_STOP_SERVICE");
                        
                        unityActivity.Call<AndroidJavaObject>("startService", serviceIntent);
                    }
                }
                
                // Release JNI objects
                videoRecordingManager?.Dispose();
                videoRecordingManager = null;
                
                // Note: Don't dispose unityActivity as it's shared
                unityActivity = null;
                
                Debug.Log("VideoRecordingService: Disposed");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingService: Error during disposal - {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Internal proxy class for handling Android callbacks
    /// </summary>
    internal class VideoRecordingCallbackProxy : AndroidJavaProxy
    {
        private readonly VideoRecordingService service;
        
        public VideoRecordingCallbackProxy(VideoRecordingService service) 
            : base("com.t34400.mediaprojectionlib.recording.IVideoRecordingCallback")
        {
            this.service = service;
        }
        
        // These methods will be called from Android
        public void onRecordingStateChanged(string state)
        {
            UnityMainThreadDispatcher.Execute(() => service.HandleStateChange(state));
        }
        
        public void onRecordingComplete(string outputPath)
        {
            UnityMainThreadDispatcher.Execute(() => service.HandleRecordingComplete(outputPath));
        }
        
        public void onRecordingError(string errorMessage)
        {
            UnityMainThreadDispatcher.Execute(() => service.HandleRecordingError(errorMessage));
        }
    }
    
    /// <summary>
    /// Simple main thread dispatcher for handling Android callbacks
    /// </summary>
    internal static class UnityMainThreadDispatcher
    {
        public static void Execute(System.Action action)
        {
            // For now, execute immediately
            // In a production implementation, you'd queue this for the main thread
            action?.Invoke();
        }
    }
}