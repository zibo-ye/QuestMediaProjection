#nullable enable

using System;
using UnityEngine;
using UnityEngine.Events;
using MediaProjection.Services;

namespace MediaProjection.ViewModels
{
    /// <summary>
    /// ViewModel for controlling video recording functionality.
    /// This provides a Unity-friendly interface for the video recording service
    /// with UI binding support and preset configurations.
    /// </summary>
    public class VideoRecordingViewModel : MonoBehaviour
    {
        [Header("Recording Configuration")]
        [SerializeField] private VideoRecordingPreset recordingPreset = VideoRecordingPreset.Default;
        [SerializeField] private bool useCustomConfig = false;
        
        [Header("Custom Configuration (when useCustomConfig = true)")]
        [SerializeField] private int customVideoBitrate = 5000000;
        [SerializeField] private int customVideoFrameRate = 30;
        [SerializeField] private string customOutputDirectory = "";
        [SerializeField] private long customMaxDurationMs = -1;
        
        [Header("Service Dependencies")]
        [SerializeField] private ServiceContainer? serviceContainer;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<RecordingState> onRecordingStateChanged = new();
        [SerializeField] private UnityEvent<string> onRecordingComplete = new();
        [SerializeField] private UnityEvent<string> onRecordingError = new();
        [SerializeField] private UnityEvent<float> onRecordingProgress = new(); // Duration in seconds
        
        // Public properties for UI binding
        public bool IsRecording => videoRecordingService?.IsRecording ?? false;
        public bool CanStartRecording => videoRecordingService?.CurrentState == RecordingState.Idle;
        public bool CanStopRecording => videoRecordingService?.CurrentState == RecordingState.Recording;
        public RecordingState CurrentState => videoRecordingService?.CurrentState ?? RecordingState.Idle;
        public string? CurrentOutputFile => videoRecordingService?.CurrentOutputFile;
        
        // Public event accessors for testing
        public UnityEvent<RecordingState> OnRecordingStateChanged => onRecordingStateChanged;
        public UnityEvent<string> OnRecordingComplete => onRecordingComplete;
        public UnityEvent<string> OnRecordingError => onRecordingError;
        public UnityEvent<float> OnRecordingProgress => onRecordingProgress;
        
        // Service reference
        private IVideoRecordingService? videoRecordingService;
        
        // Recording state tracking
        private float recordingStartTime;
        private bool wasRecording = false;
        
        /// <summary>
        /// Video recording preset configurations
        /// </summary>
        public enum VideoRecordingPreset
        {
            Default,        // Balanced quality and performance
            HighQuality,    // Maximum quality
            Performance,    // Optimized for performance
            Custom          // Use custom configuration
        }
        
        private void Start()
        {
            InitializeService();
        }
        
        private void Update()
        {
            // Update recording progress
            if (IsRecording)
            {
                if (!wasRecording)
                {
                    recordingStartTime = Time.time;
                    wasRecording = true;
                }
                
                float duration = Time.time - recordingStartTime;
                onRecordingProgress.Invoke(duration);
            }
            else if (wasRecording)
            {
                wasRecording = false;
            }
            
            // Poll recording status (fallback for when callbacks don't work)
            if (videoRecordingService is VideoRecordingService service)
            {
                service.UpdateRecordingStatus();
            }
        }
        
        /// <summary>
        /// Initialize the video recording service
        /// </summary>
        private void InitializeService()
        {
            try
            {
                // Find ServiceContainer if not assigned
                if (serviceContainer == null)
                {
                    serviceContainer = FindFirstObjectByType<ServiceContainer>();
                    if (serviceContainer == null)
                    {
                        Debug.LogError("VideoRecordingViewModel: ServiceContainer not found");
                        return;
                    }
                }
                
                // Get video recording service
                videoRecordingService = serviceContainer.VideoRecordingService;
                
                // Subscribe to events
                videoRecordingService.OnRecordingStateChanged += HandleStateChanged;
                videoRecordingService.OnRecordingComplete += HandleRecordingComplete;
                videoRecordingService.OnRecordingError += HandleRecordingError;
                
                Debug.Log("VideoRecordingViewModel: Service initialized successfully");
                
                // Check if video recording is supported
                if (!videoRecordingService.IsVideoRecordingSupported())
                {
                    Debug.LogWarning("VideoRecordingViewModel: Video recording is not supported on this device");
                    onRecordingError.Invoke("Video recording is not supported on this device");
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError($"VideoRecordingViewModel: Failed to initialize service - {e.Message}");
                onRecordingError.Invoke($"Failed to initialize video recording: {e.Message}");
            }
        }
        
        /// <summary>
        /// Start recording with current configuration
        /// </summary>
        [ContextMenu("Start Recording")]
        public void StartRecording()
        {
            if (videoRecordingService == null)
            {
                Debug.LogError("VideoRecordingViewModel: Video recording service not available");
                onRecordingError.Invoke("Video recording service not available");
                return;
            }
            
            if (!CanStartRecording)
            {
                Debug.LogWarning($"VideoRecordingViewModel: Cannot start recording in state {CurrentState}");
                return;
            }
            
            var config = GetRecordingConfig();
            
            Debug.Log($"VideoRecordingViewModel: Starting recording with preset {recordingPreset}");
            
            bool success = videoRecordingService.StartRecording(config);
            if (!success)
            {
                Debug.LogError("VideoRecordingViewModel: Failed to start recording");
                onRecordingError.Invoke("Failed to start recording");
            }
        }
        
        /// <summary>
        /// Stop current recording
        /// </summary>
        [ContextMenu("Stop Recording")]
        public void StopRecording()
        {
            if (videoRecordingService == null)
            {
                Debug.LogError("VideoRecordingViewModel: Video recording service not available");
                return;
            }
            
            if (!CanStopRecording)
            {
                Debug.LogWarning($"VideoRecordingViewModel: Cannot stop recording in state {CurrentState}");
                return;
            }
            
            Debug.Log("VideoRecordingViewModel: Stopping recording");
            
            bool success = videoRecordingService.StopRecording();
            if (!success)
            {
                Debug.LogError("VideoRecordingViewModel: Failed to stop recording");
                onRecordingError.Invoke("Failed to stop recording");
            }
        }
        
        /// <summary>
        /// Toggle recording on/off
        /// </summary>
        [ContextMenu("Toggle Recording")]
        public void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }
        
        /// <summary>
        /// Get current recording status
        /// </summary>
        public RecordingStatus GetRecordingStatus()
        {
            return videoRecordingService?.GetRecordingStatus() ?? new RecordingStatus
            {
                state = RecordingState.Idle,
                recordingDurationSeconds = 0,
                outputFilePath = null,
                errorMessage = "Service not available"
            };
        }
        
        /// <summary>
        /// Set recording preset
        /// </summary>
        public void SetRecordingPreset(VideoRecordingPreset preset)
        {
            recordingPreset = preset;
            useCustomConfig = preset == VideoRecordingPreset.Custom;
            
            Debug.Log($"VideoRecordingViewModel: Recording preset set to {preset}");
        }
        
        /// <summary>
        /// Set custom recording configuration
        /// </summary>
        public void SetCustomConfig(int bitrate, int frameRate, string outputDirectory = "", long maxDurationMs = -1)
        {
            customVideoBitrate = bitrate;
            customVideoFrameRate = frameRate;
            customOutputDirectory = outputDirectory;
            customMaxDurationMs = maxDurationMs;
            
            recordingPreset = VideoRecordingPreset.Custom;
            useCustomConfig = true;
            
            Debug.Log($"VideoRecordingViewModel: Custom config set - {bitrate} bps, {frameRate} fps");
        }
        
        /// <summary>
        /// Get recording configuration based on current settings
        /// </summary>
        private VideoRecordingConfig GetRecordingConfig()
        {
            if (useCustomConfig || recordingPreset == VideoRecordingPreset.Custom)
            {
                return new VideoRecordingConfig
                {
                    videoBitrate = customVideoBitrate,
                    videoFrameRate = customVideoFrameRate,
                    videoFormat = "video/avc", // H.264
                    audioEnabled = false,
                    outputDirectory = customOutputDirectory,
                    maxRecordingDurationMs = customMaxDurationMs
                };
            }
            
            return recordingPreset switch
            {
                VideoRecordingPreset.HighQuality => VideoRecordingConfig.HighQuality,
                VideoRecordingPreset.Performance => VideoRecordingConfig.Performance,
                VideoRecordingPreset.Default => VideoRecordingConfig.Default,
                _ => VideoRecordingConfig.Default
            };
        }
        
        /// <summary>
        /// Handle recording state changes
        /// </summary>
        private void HandleStateChanged(RecordingState state)
        {
            Debug.Log($"VideoRecordingViewModel: State changed to {state}");
            onRecordingStateChanged.Invoke(state);
        }
        
        /// <summary>
        /// Handle recording completion
        /// </summary>
        private void HandleRecordingComplete(string outputPath)
        {
            Debug.Log($"VideoRecordingViewModel: Recording completed - {outputPath}");
            onRecordingComplete.Invoke(outputPath);
        }
        
        /// <summary>
        /// Handle recording errors
        /// </summary>
        private void HandleRecordingError(string errorMessage)
        {
            Debug.LogError($"VideoRecordingViewModel: Recording error - {errorMessage}");
            onRecordingError.Invoke(errorMessage);
        }
        
        /// <summary>
        /// Get formatted recording duration string
        /// </summary>
        public string GetFormattedDuration()
        {
            var status = GetRecordingStatus();
            var duration = status.recordingDurationSeconds;
            
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            
            return $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// Get recording info for UI display
        /// </summary>
        public string GetRecordingInfo()
        {
            var status = GetRecordingStatus();
            
            return status.state switch
            {
                RecordingState.Idle => "Ready to record",
                RecordingState.Preparing => "Preparing...",
                RecordingState.Recording => $"Recording {GetFormattedDuration()}",
                RecordingState.Stopping => "Stopping...",
                RecordingState.Error => $"Error: {status.errorMessage}",
                _ => "Unknown state"
            };
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (videoRecordingService != null)
            {
                videoRecordingService.OnRecordingStateChanged -= HandleStateChanged;
                videoRecordingService.OnRecordingComplete -= HandleRecordingComplete;
                videoRecordingService.OnRecordingError -= HandleRecordingError;
            }
        }
        
        /// <summary>
        /// Validate configuration in editor
        /// </summary>
        private void OnValidate()
        {
            // Ensure bitrate is reasonable
            if (customVideoBitrate < 100000) customVideoBitrate = 100000; // Min 100 kbps
            if (customVideoBitrate > 50000000) customVideoBitrate = 50000000; // Max 50 Mbps
            
            // Ensure frame rate is reasonable
            if (customVideoFrameRate < 1) customVideoFrameRate = 1;
            if (customVideoFrameRate > 120) customVideoFrameRate = 120;
        }
    }
}