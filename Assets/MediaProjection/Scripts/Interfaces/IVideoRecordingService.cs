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
    /// Supported hardware codecs matching Android VideoRecordingManager
    /// </summary>
    public enum SupportedCodec
    {
        H264,   // video/avc
        H265,   // video/hevc
        VP8,    // video/x-vnd.on2.vp8
        VP9     // video/x-vnd.on2.vp9
    }

    /// <summary>
    /// Codec information with display name and MIME type
    /// </summary>
    [Serializable]
    public struct CodecInfo
    {
        public SupportedCodec codec;
        public string displayName;
        public string mimeType;
        
        public static CodecInfo H264 => new CodecInfo 
        { 
            codec = SupportedCodec.H264, 
            displayName = "H.264/AVC", 
            mimeType = "video/avc" 
        };
        
        public static CodecInfo H265 => new CodecInfo 
        { 
            codec = SupportedCodec.H265, 
            displayName = "H.265/HEVC", 
            mimeType = "video/hevc" 
        };
        
        public static CodecInfo VP8 => new CodecInfo 
        { 
            codec = SupportedCodec.VP8, 
            displayName = "VP8", 
            mimeType = "video/x-vnd.on2.vp8" 
        };
        
        public static CodecInfo VP9 => new CodecInfo 
        { 
            codec = SupportedCodec.VP9, 
            displayName = "VP9", 
            mimeType = "video/x-vnd.on2.vp9" 
        };
    }

    /// <summary>
    /// Standard frame rate presets for VR and high-quality recording
    /// </summary>
    public enum FrameRatePreset
    {
        Standard30 = 30,     // 30 FPS (Standard)
        Cinema36 = 36,       // 36 FPS (Cinema+)
        Smooth60 = 60,       // 60 FPS (Smooth)
        VR72 = 72,           // 72 FPS (VR Standard)
        High80 = 80,         // 80 FPS (High Performance)
        VR90 = 90            // 90 FPS (VR Premium)
    }

    /// <summary>
    /// Frame rate preset information with display name
    /// </summary>
    [Serializable]
    public struct FrameRateInfo
    {
        public FrameRatePreset preset;
        public int fps;
        public string displayName;
        
        public static FrameRateInfo Standard30 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.Standard30, 
            fps = 30, 
            displayName = "30 FPS (Standard)" 
        };
        
        public static FrameRateInfo Cinema36 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.Cinema36, 
            fps = 36, 
            displayName = "36 FPS (Cinema+)" 
        };
        
        public static FrameRateInfo Smooth60 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.Smooth60, 
            fps = 60, 
            displayName = "60 FPS (Smooth)" 
        };
        
        public static FrameRateInfo VR72 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.VR72, 
            fps = 72, 
            displayName = "72 FPS (VR Standard)" 
        };
        
        public static FrameRateInfo High80 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.High80, 
            fps = 80, 
            displayName = "80 FPS (High Performance)" 
        };
        
        public static FrameRateInfo VR90 => new FrameRateInfo 
        { 
            preset = FrameRatePreset.VR90, 
            fps = 90, 
            displayName = "90 FPS (VR Premium)" 
        };
        
        public static FrameRateInfo[] AllPresets => new[]
        {
            Standard30, Cinema36, Smooth60, VR72, High80, VR90
        };
    }

    /// <summary>
    /// Recording resolution preset
    /// </summary>
    [Serializable]
    public struct ResolutionPreset
    {
        public int width;
        public int height;
        public string displayName;
        
        public static ResolutionPreset UHD4K => new ResolutionPreset { width = 3840, height = 2160, displayName = "4K UHD (3840x2160)" };
        public static ResolutionPreset QHD => new ResolutionPreset { width = 2560, height = 1440, displayName = "QHD (2560x1440)" };
        public static ResolutionPreset FHD => new ResolutionPreset { width = 1920, height = 1080, displayName = "FHD (1920x1080)" };
        public static ResolutionPreset HD => new ResolutionPreset { width = 1280, height = 720, displayName = "HD (1280x720)" };
        public static ResolutionPreset UltraWideVR => new ResolutionPreset { width = 2048, height = 1024, displayName = "Ultra-wide VR (2048x1024)" };
    }

    /// <summary>
    /// Video recording configuration with enhanced VR features
    /// </summary>
    [Serializable]
    public struct VideoRecordingConfig
    {
        [Header("Video Settings")]
        [SerializeField] public int videoBitrate;
        [SerializeField] public int videoFrameRate;
        [SerializeField] public string videoFormat;
        [SerializeField] public int videoWidth;  // 0 = use display width
        [SerializeField] public int videoHeight; // 0 = use display height
        
        [Header("Audio Settings")]
        [SerializeField] public bool audioEnabled;
        
        [Header("Output Settings")]
        [SerializeField] public string outputDirectory;
        [SerializeField] public long maxRecordingDurationMs;
        [SerializeField] public bool writeToFileWhileRecording;
        
        /// <summary>
        /// Creates a default configuration with standard HD recording settings
        /// </summary>
        public static VideoRecordingConfig Default => new VideoRecordingConfig
        {
            videoBitrate = 5000000, // 5 Mbps
            videoFrameRate = 30,    // 30 fps
            videoFormat = "video/avc", // H.264
            videoWidth = 0,         // Use display width
            videoHeight = 0,        // Use display height
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L, // Unlimited
            writeToFileWhileRecording = true
        };
        
        /// <summary>
        /// Creates a VR-optimized 4K configuration with 72fps
        /// </summary>
        public static VideoRecordingConfig VR4K => new VideoRecordingConfig
        {
            videoBitrate = 60000000, // 60 Mbps (higher for VR 72fps)
            videoFrameRate = 72,     // 72 fps (VR Standard)
            videoFormat = "video/avc", // H.264
            videoWidth = 3840,       // 4K width
            videoHeight = 2160,      // 4K height
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L,
            writeToFileWhileRecording = true
        };
        
        /// <summary>
        /// Creates a VR-optimized QHD configuration with H.265 and 90fps
        /// </summary>
        public static VideoRecordingConfig VRQHD => new VideoRecordingConfig
        {
            videoBitrate = 45000000, // 45 Mbps (higher for VR 90fps)
            videoFrameRate = 90,     // 90 fps (VR Premium)
            videoFormat = "video/hevc", // H.265
            videoWidth = 2560,       // QHD width
            videoHeight = 1440,      // QHD height
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L,
            writeToFileWhileRecording = true
        };
        
        /// <summary>
        /// Creates a performance-optimized configuration for longer recordings
        /// </summary>
        public static VideoRecordingConfig Performance => new VideoRecordingConfig
        {
            videoBitrate = 2000000, // 2 Mbps
            videoFrameRate = 30,    // 30 fps
            videoFormat = "video/avc", // H.264
            videoWidth = 1280,      // HD width
            videoHeight = 720,      // HD height
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L,
            writeToFileWhileRecording = true
        };
        
        /// <summary>
        /// Creates a high-quality configuration with 1080p resolution and high bitrate
        /// </summary>
        public static VideoRecordingConfig HighQuality => new VideoRecordingConfig
        {
            videoBitrate = 10000000, // 10 Mbps
            videoFrameRate = 30,     // 30 fps
            videoFormat = "video/avc", // H.264
            videoWidth = 1920,       // FHD width
            videoHeight = 1080,      // FHD height
            audioEnabled = false,
            outputDirectory = "",
            maxRecordingDurationMs = -1L,
            writeToFileWhileRecording = true
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
        
        /// <summary>
        /// Get available hardware-accelerated codecs on this device
        /// </summary>
        /// <returns>Array of available codec information</returns>
        CodecInfo[] GetAvailableCodecs();
        
        /// <summary>
        /// Get optimal VR recording resolutions for this device
        /// </summary>
        /// <returns>Array of recommended resolution presets</returns>
        ResolutionPreset[] GetOptimalResolutions();
        
        /// <summary>
        /// Get available frame rate presets for recording
        /// </summary>
        /// <returns>Array of available frame rate presets</returns>
        FrameRateInfo[] GetAvailableFrameRates();
        
        /// <summary>
        /// Get recommended bitrate for the specified resolution and framerate
        /// </summary>
        /// <param name="width">Video width in pixels</param>
        /// <param name="height">Video height in pixels</param>
        /// <param name="frameRate">Target frame rate</param>
        /// <returns>Recommended bitrate in bits per second</returns>
        int GetRecommendedBitrate(int width, int height, int frameRate = 30);
        
        /// <summary>
        /// Create a custom recording configuration
        /// </summary>
        /// <param name="codec">Video codec to use</param>
        /// <param name="resolution">Recording resolution</param>
        /// <param name="bitrate">Video bitrate in bps</param>
        /// <param name="frameRate">Frame rate (default: 30)</param>
        /// <returns>Configured VideoRecordingConfig</returns>
        VideoRecordingConfig CreateCustomConfig(CodecInfo codec, ResolutionPreset resolution, int bitrate, int frameRate = 30);
        
        /// <summary>
        /// Create a recording configuration with frame rate preset
        /// </summary>
        /// <param name="frameRatePreset">Frame rate preset to use</param>
        /// <param name="resolution">Recording resolution (optional)</param>
        /// <param name="codec">Video codec (optional, defaults to H.264)</param>
        /// <returns>Configured VideoRecordingConfig with optimal bitrate</returns>
        VideoRecordingConfig CreateConfigWithFrameRate(FrameRatePreset frameRatePreset, ResolutionPreset? resolution = null, CodecInfo? codec = null);
        
        /// <summary>
        /// Update the recording status (called periodically to sync with native layer)
        /// </summary>
        void UpdateRecordingStatus();
    }
}