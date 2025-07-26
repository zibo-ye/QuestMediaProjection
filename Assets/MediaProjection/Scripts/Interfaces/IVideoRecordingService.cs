#nullable enable

using System;
using UnityEngine;

namespace MediaProjection.Services
{
    /// <summary>
    /// Recording state enumeration matching the Android VideoRecordingManager states
    /// </summary>
    public enum RecordingState
    {
        Idle,
        Preparing,
        Recording,
        Pausing,
        Stopping,
        Error
    }

    /// <summary>
    /// Video recording configuration
    /// </summary>
    [Serializable]
    public struct VideoRecordingConfig
    {
        [Header("Video Settings")]
        [SerializeField] public int videoBitrate;
        [SerializeField] public int videoFrameRate;
        [SerializeField] public string videoFormat;
        
        [Header("Audio Settings")]
        [SerializeField] public bool audioEnabled;
        
        [Header("Output Settings")]
        [SerializeField] public string outputDirectory;
        [SerializeField] public long maxRecordingDurationMs;
        
        /// <summary>
        /// Creates a default configuration with standard HD recording settings
        /// </summary>
        public static VideoRecordingConfig Default => new VideoRecordingConfig
        {
            videoBitrate = 5000000, // 5 Mbps
            videoFrameRate = 30,    // 30 fps
            videoFormat = "video/avc", // H.264
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L // Unlimited
        };
        
        /// <summary>
        /// Creates a high quality configuration for better video quality
        /// </summary>
        public static VideoRecordingConfig HighQuality => new VideoRecordingConfig
        {
            videoBitrate = 10000000, // 10 Mbps
            videoFrameRate = 60,     // 60 fps
            videoFormat = "video/avc", // H.264
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L
        };
        
        /// <summary>
        /// Creates a performance-optimized configuration for longer recordings
        /// </summary>
        public static VideoRecordingConfig Performance => new VideoRecordingConfig
        {
            videoBitrate = 2000000, // 2 Mbps
            videoFrameRate = 30,    // 30 fps
            videoFormat = "video/avc", // H.264
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L
        };
    }

    /// <summary>
    /// Recording status information
    /// </summary>
    [Serializable]
    public struct RecordingStatus
    {
        public RecordingState state;
        public float recordingDurationSeconds;
        public string? outputFilePath;
        public string? errorMessage;
        
        public bool IsRecording => state == RecordingState.Recording;
        public bool IsIdle => state == RecordingState.Idle;
        public bool HasError => state == RecordingState.Error;
    }

    /// <summary>
    /// Interface for video recording functionality using the MediaProjection + MediaCodec pipeline
    /// </summary>
    public interface IVideoRecordingService : IDisposable
    {
        /// <summary>
        /// Current recording state
        /// </summary>
        RecordingState CurrentState { get; }
        
        /// <summary>
        /// Whether the service is currently recording
        /// </summary>
        bool IsRecording { get; }
        
        /// <summary>
        /// Current output file path (null if not recording or not started)
        /// </summary>
        string? CurrentOutputFile { get; }
        
        /// <summary>
        /// Event fired when recording state changes
        /// </summary>
        event Action<RecordingState>? OnRecordingStateChanged;
        
        /// <summary>
        /// Event fired when recording completes with the output file path
        /// </summary>
        event Action<string>? OnRecordingComplete;
        
        /// <summary>
        /// Event fired when a recording error occurs
        /// </summary>
        event Action<string>? OnRecordingError;
        
        /// <summary>
        /// Start video recording with the specified configuration
        /// </summary>
        /// <param name="config">Recording configuration</param>
        /// <returns>True if recording started successfully, false otherwise</returns>
        bool StartRecording(VideoRecordingConfig config);
        
        /// <summary>
        /// Start video recording with default configuration
        /// </summary>
        /// <returns>True if recording started successfully, false otherwise</returns>
        bool StartRecording() => StartRecording(VideoRecordingConfig.Default);
        
        /// <summary>
        /// Stop the current recording
        /// </summary>
        /// <returns>True if stop command was sent successfully, false otherwise</returns>
        bool StopRecording();
        
        /// <summary>
        /// Get current recording status
        /// </summary>
        /// <returns>Current recording status information</returns>
        RecordingStatus GetRecordingStatus();
        
        /// <summary>
        /// Check if video recording is supported on this device
        /// </summary>
        /// <returns>True if video recording is supported, false otherwise</returns>
        bool IsVideoRecordingSupported();
    }
}