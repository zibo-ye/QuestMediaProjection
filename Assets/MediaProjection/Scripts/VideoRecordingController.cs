#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MediaProjection.Services;

namespace MediaProjection
{
    /// <summary>
    /// Unity MonoBehaviour controller for video recording with configurable codec, resolution, and bitrate.
    /// This script provides a complete UI interface for VR screen recording with hardware acceleration.
    /// </summary>
    public class VideoRecordingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startRecordingButton = null!;
        [SerializeField] private Button stopRecordingButton = null!;
        [SerializeField] private Button queryCodecsButton = null!;
        [SerializeField] private Dropdown codecDropdown = null!;
        [SerializeField] private Dropdown resolutionDropdown = null!;
        [SerializeField] private Dropdown bitrateDropdown = null!;
        [SerializeField] private Text statusText = null!;
        [SerializeField] private Text outputPathText = null!;
        [SerializeField] private Text durationText = null!;
        
        [Header("Recording Settings")]
        [SerializeField] private bool autoQueryCodecsOnStart = true;
        [SerializeField] private int frameRate = 30;
        
        // Service and state
        private IVideoRecordingService? videoRecordingService;
        private CodecInfo[] availableCodecs = Array.Empty<CodecInfo>();
        private ResolutionPreset[] availableResolutions = Array.Empty<ResolutionPreset>();
        private int[] availableBitrates = Array.Empty<int>();
        
        // Recording state
        private float recordingStartTime;
        private bool isInitialized = false;
        
        private void Start()
        {
            InitializeVideoRecordingService();
            SetupUI();
            
            if (autoQueryCodecsOnStart)
            {
                QueryAvailableCodecs();
            }
        }
        
        private void Update()
        {
            // Update recording status
            videoRecordingService?.UpdateRecordingStatus();
            
            // Update UI if recording
            if (videoRecordingService?.IsRecording == true)
            {
                UpdateRecordingDuration();
            }
        }
        
        private void InitializeVideoRecordingService()
        {
            try
            {
                videoRecordingService = new VideoRecordingService();
                
                // Subscribe to events
                videoRecordingService.OnRecordingStateChanged += OnRecordingStateChanged;
                videoRecordingService.OnRecordingComplete += OnRecordingComplete;
                videoRecordingService.OnRecordingError += OnRecordingError;
                
                isInitialized = videoRecordingService.IsVideoRecordingSupported();
                
                if (isInitialized)
                {
                    UpdateStatusText("Video recording service initialized successfully");
                    Debug.Log("VideoRecordingController: Initialized successfully");
                }
                else
                {
                    UpdateStatusText("Video recording not supported on this device");
                    Debug.LogWarning("VideoRecordingController: Video recording not supported");
                }
            }
            catch (Exception e)
            {
                UpdateStatusText($"Failed to initialize: {e.Message}");
                Debug.LogError($"VideoRecordingController: Initialization failed - {e.Message}");
            }
        }
        
        private void SetupUI()
        {
            // Setup button listeners
            if (startRecordingButton != null)
            {
                startRecordingButton.onClick.AddListener(StartRecording);
            }
            
            if (stopRecordingButton != null)
            {
                stopRecordingButton.onClick.AddListener(StopRecording);
                stopRecordingButton.interactable = false;
            }
            
            if (queryCodecsButton != null)
            {
                queryCodecsButton.onClick.AddListener(QueryAvailableCodecs);
            }
            
            // Setup dropdown listeners
            if (codecDropdown != null)
            {
                codecDropdown.onValueChanged.AddListener(OnCodecChanged);
            }
            
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
            
            if (bitrateDropdown != null)
            {
                bitrateDropdown.onValueChanged.AddListener(OnBitrateChanged);
            }
            
            UpdateUI();
        }
        
        /// <summary>
        /// Query available codecs from the device and populate UI
        /// </summary>
        public void QueryAvailableCodecs()
        {
            if (videoRecordingService == null || !isInitialized)
            {
                UpdateStatusText("Service not initialized");
                return;
            }
            
            try
            {
                UpdateStatusText("Querying available codecs...");
                
                // Get available codecs and resolutions
                availableCodecs = videoRecordingService.GetAvailableCodecs();
                availableResolutions = videoRecordingService.GetOptimalResolutions();
                
                // Setup bitrate options (1-50 Mbps)
                availableBitrates = new int[]
                {
                    1_000_000,   // 1 Mbps
                    2_500_000,   // 2.5 Mbps
                    5_000_000,   // 5 Mbps
                    10_000_000,  // 10 Mbps
                    15_000_000,  // 15 Mbps
                    25_000_000,  // 25 Mbps
                    50_000_000   // 50 Mbps
                };
                
                // Populate dropdowns
                PopulateCodecDropdown();
                PopulateResolutionDropdown();
                PopulateBitrateDropdown();
                
                UpdateStatusText($"Found {availableCodecs.Length} codecs, {availableResolutions.Length} resolutions");
                
                Debug.Log($"VideoRecordingController: Found {availableCodecs.Length} codecs, {availableResolutions.Length} resolutions");
            }
            catch (Exception e)
            {
                UpdateStatusText($"Error querying codecs: {e.Message}");
                Debug.LogError($"VideoRecordingController: Error querying codecs - {e.Message}");
            }
        }
        
        /// <summary>
        /// Start recording with current configuration
        /// </summary>
        public void StartRecording()
        {
            if (videoRecordingService == null || !isInitialized)
            {
                UpdateStatusText("Service not initialized");
                return;
            }
            
            if (videoRecordingService.IsRecording)
            {
                UpdateStatusText("Already recording");
                return;
            }
            
            try
            {
                var config = GetCurrentConfiguration();
                
                UpdateStatusText($"Starting recording: {config.videoWidth}x{config.videoHeight}, {config.videoBitrate / 1_000_000}Mbps, {GetSelectedCodec().displayName}");
                
                bool success = videoRecordingService.StartRecording(config);
                
                if (success)
                {
                    recordingStartTime = Time.time;
                    Debug.Log($"VideoRecordingController: Recording started with config - {config.videoWidth}x{config.videoHeight}, {config.videoBitrate}bps, {config.videoFormat}");
                }
                else
                {
                    UpdateStatusText("Failed to start recording");
                }
            }
            catch (Exception e)
            {
                UpdateStatusText($"Error starting recording: {e.Message}");
                Debug.LogError($"VideoRecordingController: Error starting recording - {e.Message}");
            }
        }
        
        /// <summary>
        /// Stop the current recording
        /// </summary>
        public void StopRecording()
        {
            if (videoRecordingService == null)
                return;
            
            try
            {
                UpdateStatusText("Stopping recording...");
                
                bool success = videoRecordingService.StopRecording();
                
                if (!success)
                {
                    UpdateStatusText("Failed to stop recording");
                }
            }
            catch (Exception e)
            {
                UpdateStatusText($"Error stopping recording: {e.Message}");
                Debug.LogError($"VideoRecordingController: Error stopping recording - {e.Message}");
            }
        }
        
        /// <summary>
        /// Get current recording configuration from UI selections
        /// </summary>
        private VideoRecordingConfig GetCurrentConfiguration()
        {
            var selectedCodec = GetSelectedCodec();
            var selectedResolution = GetSelectedResolution();
            var selectedBitrate = GetSelectedBitrate();
            
            return videoRecordingService!.CreateCustomConfig(selectedCodec, selectedResolution, selectedBitrate, frameRate);
        }
        
        private CodecInfo GetSelectedCodec()
        {
            int index = codecDropdown?.value ?? 0;
            return availableCodecs.Length > index ? availableCodecs[index] : CodecInfo.H264;
        }
        
        private ResolutionPreset GetSelectedResolution()
        {
            int index = resolutionDropdown?.value ?? 0;
            return availableResolutions.Length > index ? availableResolutions[index] : ResolutionPreset.FHD;
        }
        
        private int GetSelectedBitrate()
        {
            int index = bitrateDropdown?.value ?? 0;
            return availableBitrates.Length > index ? availableBitrates[index] : 5_000_000;
        }
        
        private void PopulateCodecDropdown()
        {
            if (codecDropdown == null) return;
            
            codecDropdown.ClearOptions();
            var options = new List<string>();
            
            foreach (var codec in availableCodecs)
            {
                options.Add(codec.displayName);
            }
            
            codecDropdown.AddOptions(options);
            codecDropdown.value = 0;
        }
        
        private void PopulateResolutionDropdown()
        {
            if (resolutionDropdown == null) return;
            
            resolutionDropdown.ClearOptions();
            var options = new List<string>();
            
            foreach (var resolution in availableResolutions)
            {
                options.Add(resolution.displayName);
            }
            
            resolutionDropdown.AddOptions(options);
            
            // Select 1920x1080 as default if available
            for (int i = 0; i < availableResolutions.Length; i++)
            {
                if (availableResolutions[i].width == 1920 && availableResolutions[i].height == 1080)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }
        
        private void PopulateBitrateDropdown()
        {
            if (bitrateDropdown == null) return;
            
            bitrateDropdown.ClearOptions();
            var options = new List<string>();
            
            foreach (var bitrate in availableBitrates)
            {
                var mbps = bitrate / 1_000_000;
                options.Add($"{mbps} Mbps");
            }
            
            bitrateDropdown.AddOptions(options);
            bitrateDropdown.value = 2; // Default to 5 Mbps
        }
        
        private void OnCodecChanged(int index)
        {
            if (availableCodecs.Length > index)
            {
                Debug.Log($"VideoRecordingController: Codec changed to {availableCodecs[index].displayName}");
            }
        }
        
        private void OnResolutionChanged(int index)
        {
            if (availableResolutions.Length > index && videoRecordingService != null)
            {
                var resolution = availableResolutions[index];
                var recommendedBitrate = videoRecordingService.GetRecommendedBitrate(resolution.width, resolution.height, frameRate);
                
                Debug.Log($"VideoRecordingController: Resolution changed to {resolution.displayName}, recommended bitrate: {recommendedBitrate / 1_000_000}Mbps");
                
                // Update bitrate dropdown to show recommended value
                UpdateBitrateForResolution(recommendedBitrate);
            }
        }
        
        private void OnBitrateChanged(int index)
        {
            if (availableBitrates.Length > index)
            {
                Debug.Log($"VideoRecordingController: Bitrate changed to {availableBitrates[index] / 1_000_000}Mbps");
            }
        }
        
        private void UpdateBitrateForResolution(int recommendedBitrate)
        {
            // Find closest bitrate option
            int closestIndex = 0;
            int closestDiff = Math.Abs(availableBitrates[0] - recommendedBitrate);
            
            for (int i = 1; i < availableBitrates.Length; i++)
            {
                int diff = Math.Abs(availableBitrates[i] - recommendedBitrate);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIndex = i;
                }
            }
            
            if (bitrateDropdown != null)
            {
                bitrateDropdown.value = closestIndex;
            }
        }
        
        private void UpdateRecordingDuration()
        {
            if (durationText != null && videoRecordingService != null)
            {
                float duration = Time.time - recordingStartTime;
                int minutes = Mathf.FloorToInt(duration / 60);
                int seconds = Mathf.FloorToInt(duration % 60);
                durationText.text = $"Recording: {minutes:00}:{seconds:00}";
            }
        }
        
        private void UpdateUI()
        {
            bool isRecording = videoRecordingService?.IsRecording ?? false;
            
            if (startRecordingButton != null)
                startRecordingButton.interactable = isInitialized && !isRecording;
            
            if (stopRecordingButton != null)
                stopRecordingButton.interactable = isRecording;
            
            if (queryCodecsButton != null)
                queryCodecsButton.interactable = isInitialized && !isRecording;
            
            // Disable dropdowns during recording
            if (codecDropdown != null)
                codecDropdown.interactable = !isRecording;
            
            if (resolutionDropdown != null)
                resolutionDropdown.interactable = !isRecording;
            
            if (bitrateDropdown != null)
                bitrateDropdown.interactable = !isRecording;
        }
        
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
            
            Debug.Log($"VideoRecordingController: {message}");
        }
        
        private void UpdateOutputPath(string path)
        {
            if (outputPathText != null)
            {
                var fileName = System.IO.Path.GetFileName(path);
                outputPathText.text = $"Output: {fileName}";
            }
        }
        
        // Event handlers
        private void OnRecordingStateChanged(RecordingState state)
        {
            switch (state)
            {
                case RecordingState.Preparing:
                    UpdateStatusText("Preparing to record...");
                    break;
                case RecordingState.Recording:
                    UpdateStatusText("Recording in progress");
                    recordingStartTime = Time.time;
                    break;
                case RecordingState.Stopping:
                    UpdateStatusText("Stopping recording...");
                    break;
                case RecordingState.Idle:
                    UpdateStatusText("Recording stopped");
                    if (durationText != null)
                        durationText.text = "";
                    break;
                case RecordingState.Error:
                    UpdateStatusText("Recording error occurred");
                    break;
            }
            
            UpdateUI();
        }
        
        private void OnRecordingComplete(string outputPath)
        {
            UpdateStatusText("Recording completed successfully");
            UpdateOutputPath(outputPath);
            
            Debug.Log($"VideoRecordingController: Recording completed - {outputPath}");
        }
        
        private void OnRecordingError(string errorMessage)
        {
            UpdateStatusText($"Recording error: {errorMessage}");
            Debug.LogError($"VideoRecordingController: Recording error - {errorMessage}");
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (videoRecordingService != null)
            {
                videoRecordingService.OnRecordingStateChanged -= OnRecordingStateChanged;
                videoRecordingService.OnRecordingComplete -= OnRecordingComplete;
                videoRecordingService.OnRecordingError -= OnRecordingError;
                
                videoRecordingService.Dispose();
            }
        }
    }
}