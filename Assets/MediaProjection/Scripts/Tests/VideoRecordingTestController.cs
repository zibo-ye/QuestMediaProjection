#nullable enable

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MediaProjection.Services;
using MediaProjection.ViewModels;

namespace MediaProjection.Testing
{
    /// <summary>
    /// Comprehensive test controller for video recording functionality.
    /// Provides both manual testing UI and automated test sequences.
    /// </summary>
    public class VideoRecordingTestController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startRecordingButton = null!;
        [SerializeField] private Button stopRecordingButton = null!;
        [SerializeField] private Button runAutomatedTestsButton = null!;
        [SerializeField] private TMP_Dropdown qualityPresetDropdown = null!;
        [SerializeField] private TMP_Text statusText = null!;
        [SerializeField] private TMP_Text durationText = null!;
        [SerializeField] private TMP_Text outputFileText = null!;
        [SerializeField] private TMP_Text logText = null!;
        [SerializeField] private ScrollRect logScrollRect = null!;
        
        [Header("Test Configuration")]
        [SerializeField] private bool enablePerformanceLogging = true;
        [SerializeField] private bool enableDetailedLogs = true;
        [SerializeField] private float testRecordingDuration = 5f;
        
        [Header("Service Dependencies")]
        [SerializeField] private ServiceContainer? serviceContainer;
        [SerializeField] private VideoRecordingViewModel? videoRecordingViewModel;
        
        // Test state
        private IVideoRecordingService? recordingService;
        private Coroutine? automatedTestCoroutine;
        private int testsPassed = 0;
        private int testsFailed = 0;
        private DateTime recordingStartTime;
        
        // Performance tracking
        private float lastFrameTime;
        private int frameCount;
        private float totalFrameTime;

        private void Start()
        {
            InitializeComponents();
            SetupUI();
            StartPerformanceMonitoring();
        }
        
        private void Update()
        {
            UpdatePerformanceMetrics();
            UpdateUI();
        }
        
        /// <summary>
        /// Initialize components and services
        /// </summary>
        private void InitializeComponents()
        {
            try
            {
                // Find ServiceContainer if not assigned
                if (serviceContainer == null)
                {
                    serviceContainer = FindFirstObjectByType<ServiceContainer>();
                }
                
                // Find VideoRecordingViewModel if not assigned
                if (videoRecordingViewModel == null)
                {
                    videoRecordingViewModel = FindFirstObjectByType<VideoRecordingViewModel>();
                }
                
                // Get recording service
                if (serviceContainer != null)
                {
                    recordingService = serviceContainer.VideoRecordingService;
                    
                    // Subscribe to events
                    recordingService.OnRecordingStateChanged += OnRecordingStateChanged;
                    recordingService.OnRecordingComplete += OnRecordingComplete;
                    recordingService.OnRecordingError += OnRecordingError;
                    
                    LogMessage("✅ VideoRecordingService initialized successfully");
                }
                else
                {
                    LogMessage("❌ ServiceContainer not found");
                }
                
            }
            catch (Exception e)
            {
                LogMessage($"❌ Initialization failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Setup UI components and event handlers
        /// </summary>
        private void SetupUI()
        {
            // Button events
            if (startRecordingButton != null)
                startRecordingButton.onClick.AddListener(StartRecording);
            
            if (stopRecordingButton != null)
                stopRecordingButton.onClick.AddListener(StopRecording);
            
            if (runAutomatedTestsButton != null)
                runAutomatedTestsButton.onClick.AddListener(RunAutomatedTests);
            
            // Quality preset dropdown
            if (qualityPresetDropdown != null)
            {
                qualityPresetDropdown.ClearOptions();
                qualityPresetDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Default", "High Quality", "Performance", "Custom"
                });
                qualityPresetDropdown.value = 0;
            }
            
            LogMessage("🎮 UI initialized");
        }
        
        /// <summary>
        /// Start performance monitoring
        /// </summary>
        private void StartPerformanceMonitoring()
        {
            if (enablePerformanceLogging)
            {
                InvokeRepeating(nameof(LogPerformanceMetrics), 1f, 1f);
                LogMessage("📊 Performance monitoring started");
            }
        }
        
        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            if (enablePerformanceLogging)
            {
                frameCount++;
                totalFrameTime += Time.unscaledDeltaTime;
                lastFrameTime = Time.unscaledDeltaTime;
            }
        }
        
        /// <summary>
        /// Log performance metrics periodically
        /// </summary>
        private void LogPerformanceMetrics()
        {
            if (!enablePerformanceLogging) return;
            
            float avgFPS = frameCount / totalFrameTime;
            float currentFPS = 1f / lastFrameTime;
            long memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            
            if (enableDetailedLogs)
            {
                LogMessage($"📊 FPS: {currentFPS:F1} (avg: {avgFPS:F1}) | Memory: {memoryUsage}MB");
            }
            
            // Reset counters periodically
            if (frameCount > 300) // Reset every ~5 seconds at 60fps
            {
                frameCount = 0;
                totalFrameTime = 0;
            }
        }
        
        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (recordingService == null) return;
            
            var status = recordingService.GetRecordingStatus();
            
            // Update status text
            if (statusText != null)
            {
                statusText.text = $"Status: {status.state}";
                statusText.color = status.state switch
                {
                    RecordingState.Recording => Color.red,
                    RecordingState.Error => Color.red,
                    RecordingState.Idle => Color.green,
                    _ => Color.yellow
                };
            }
            
            // Update duration text
            if (durationText != null)
            {
                if (status.IsRecording)
                {
                    var duration = (DateTime.Now - recordingStartTime).TotalSeconds;
                    durationText.text = $"Duration: {duration:F1}s";
                }
                else
                {
                    durationText.text = $"Duration: {status.recordingDurationSeconds:F1}s";
                }
            }
            
            // Update output file text
            if (outputFileText != null)
            {
                outputFileText.text = status.outputFilePath != null 
                    ? $"File: {System.IO.Path.GetFileName(status.outputFilePath)}"
                    : "File: None";
            }
            
            // Update button states
            if (startRecordingButton != null)
                startRecordingButton.interactable = status.IsIdle;
            
            if (stopRecordingButton != null)
                stopRecordingButton.interactable = status.IsRecording;
        }
        
        /// <summary>
        /// Start recording with selected quality preset
        /// </summary>
        public void StartRecording()
        {
            if (recordingService == null)
            {
                LogMessage("❌ Recording service not available");
                return;
            }
            
            try
            {
                var config = GetSelectedConfig();
                LogMessage($"🎬 Starting recording with {GetPresetName()} preset");
                LogMessage($"   - Bitrate: {config.videoBitrate / 1000000f:F1} Mbps");
                LogMessage($"   - Frame Rate: {config.videoFrameRate} fps");
                
                recordingStartTime = DateTime.Now;
                
                bool success = recordingService.StartRecording(config);
                if (success)
                {
                    LogMessage("✅ Recording start command sent");
                }
                else
                {
                    LogMessage("❌ Failed to start recording");
                }
            }
            catch (Exception e)
            {
                LogMessage($"❌ Exception starting recording: {e.Message}");
            }
        }
        
        /// <summary>
        /// Stop current recording
        /// </summary>
        public void StopRecording()
        {
            if (recordingService == null)
            {
                LogMessage("❌ Recording service not available");
                return;
            }
            
            try
            {
                LogMessage("⏹️ Stopping recording...");
                bool success = recordingService.StopRecording();
                if (success)
                {
                    LogMessage("✅ Recording stop command sent");
                }
                else
                {
                    LogMessage("❌ Failed to stop recording");
                }
            }
            catch (Exception e)
            {
                LogMessage($"❌ Exception stopping recording: {e.Message}");
            }
        }
        
        /// <summary>
        /// Run automated test sequence
        /// </summary>
        public void RunAutomatedTests()
        {
            if (automatedTestCoroutine != null)
            {
                StopCoroutine(automatedTestCoroutine);
            }
            
            automatedTestCoroutine = StartCoroutine(AutomatedTestSequence());
        }
        
        /// <summary>
        /// Automated test sequence
        /// </summary>
        private IEnumerator AutomatedTestSequence()
        {
            LogMessage("🤖 Starting automated test sequence");
            testsPassed = 0;
            testsFailed = 0;
            
            // Test 1: Service availability
            yield return StartCoroutine(TestServiceAvailability());
            
            // Test 2: Device compatibility
            yield return StartCoroutine(TestDeviceCompatibility());
            
            // Test 3: Configuration validation
            yield return StartCoroutine(TestConfigurationValidation());
            
            // Test 4: Recording lifecycle
            yield return StartCoroutine(TestRecordingLifecycle());
            
            // Test 5: Error handling
            yield return StartCoroutine(TestErrorHandling());
            
            // Test summary
            LogMessage($"🏁 Automated tests completed: {testsPassed} passed, {testsFailed} failed");
            
            automatedTestCoroutine = null;
        }
        
        /// <summary>
        /// Test service availability
        /// </summary>
        private IEnumerator TestServiceAvailability()
        {
            LogMessage("🧪 Test 1: Service Availability");
            
            try
            {
                if (recordingService != null)
                {
                    LogMessage("✅ Recording service is available");
                    testsPassed++;
                }
                else
                {
                    LogMessage("❌ Recording service is null");
                    testsFailed++;
                }
            }
            catch (Exception e)
            {
                LogMessage($"❌ Service availability test failed: {e.Message}");
                testsFailed++;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// Test device compatibility
        /// </summary>
        private IEnumerator TestDeviceCompatibility()
        {
            LogMessage("🧪 Test 2: Device Compatibility");
            
            try
            {
                if (recordingService != null)
                {
                    bool isSupported = recordingService.IsVideoRecordingSupported();
                    if (isSupported)
                    {
                        LogMessage("✅ Video recording is supported on this device");
                        testsPassed++;
                    }
                    else
                    {
                        LogMessage("❌ Video recording is not supported on this device");
                        testsFailed++;
                    }
                }
                else
                {
                    LogMessage("❌ Cannot test compatibility - service unavailable");
                    testsFailed++;
                }
            }
            catch (Exception e)
            {
                LogMessage($"❌ Compatibility test failed: {e.Message}");
                testsFailed++;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// Test configuration validation
        /// </summary>
        private IEnumerator TestConfigurationValidation()
        {
            LogMessage("🧪 Test 3: Configuration Validation");
            
            try
            {
                // Test different presets including frame rate validation
                var configs = new[]
                {
                    VideoRecordingConfig.Default,
                    VideoRecordingConfig.HighQuality,
                    VideoRecordingConfig.Performance,
                    VideoRecordingConfig.VR4K,
                    VideoRecordingConfig.VRQHD
                };
                
                foreach (var config in configs)
                {
                    if (config.videoBitrate > 0 && config.videoFrameRate > 0)
                    {
                        LogMessage($"✅ Config valid: {config.videoBitrate/1000000f:F1}Mbps, {config.videoFrameRate}fps");
                    }
                    else
                    {
                        LogMessage($"❌ Invalid config: {config.videoBitrate}, {config.videoFrameRate}");
                        testsFailed++;
                        yield break;
                    }
                }
                
                // Test frame rate presets
                LogMessage("📊 Testing Frame Rate Presets:");
                var frameRatePresets = FrameRateInfo.AllPresets;
                foreach (var preset in frameRatePresets)
                {
                    if (preset.fps > 0 && !string.IsNullOrEmpty(preset.displayName))
                    {
                        LogMessage($"✅ Frame Rate: {preset.fps}fps - {preset.displayName}");
                    }
                    else
                    {
                        LogMessage($"❌ Invalid frame rate preset: {preset.fps}fps");
                        testsFailed++;
                        yield break;
                    }
                }
                
                // Validate VR frame rates are high enough
                var vrPresets = frameRatePresets.Where(p => p.displayName.Contains("VR")).ToArray();
                if (vrPresets.All(p => p.fps >= 72))
                {
                    LogMessage($"✅ VR frame rates meet minimum 72fps requirement");
                }
                else
                {
                    LogMessage($"❌ VR frame rates below 72fps requirement");
                    testsFailed++;
                    yield break;
                }
                
                testsPassed++;
            }
            catch (Exception e)
            {
                LogMessage($"❌ Configuration validation failed: {e.Message}");
                testsFailed++;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// Test recording lifecycle
        /// </summary>
        private IEnumerator TestRecordingLifecycle()
        {
            LogMessage($"🧪 Test 4: Recording Lifecycle ({testRecordingDuration}s recording)");
            
            if (recordingService == null)
            {
                LogMessage("❌ Cannot test lifecycle - service unavailable");
                testsFailed++;
                yield break;
            }
            
            // Test start recording  
            var initialState = recordingService.CurrentState;
            if (initialState != RecordingState.Idle)
            {
                LogMessage($"❌ Expected Idle state, got {initialState}");
                testsFailed++;
                yield break;
            }
            
            bool startSuccess = recordingService.StartRecording(VideoRecordingConfig.Performance);
            if (!startSuccess)
            {
                LogMessage("❌ Failed to start recording");
                testsFailed++;
                yield break;
            }
            
            // Wait for recording to start
            float timeout = 10f;
            float elapsed = 0f;
            while (elapsed < timeout && recordingService.CurrentState != RecordingState.Recording)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (recordingService.CurrentState != RecordingState.Recording)
            {
                LogMessage($"❌ Recording didn't start within {timeout}s. State: {recordingService.CurrentState}");
                testsFailed++;
                yield break;
            }
            
            LogMessage("✅ Recording started successfully");
            
            // Record for specified duration
            yield return new WaitForSeconds(testRecordingDuration);
            
            // Test stop recording
            bool stopSuccess = recordingService.StopRecording();
            if (!stopSuccess)
            {
                LogMessage("❌ Failed to stop recording");
                testsFailed++;
                yield break;
            }
            
            // Wait for recording to stop
            elapsed = 0f;
            while (elapsed < timeout && recordingService.CurrentState != RecordingState.Idle)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (recordingService.CurrentState != RecordingState.Idle)
            {
                LogMessage($"❌ Recording didn't stop within {timeout}s. State: {recordingService.CurrentState}");
                testsFailed++;
                yield break;
            }
            
            LogMessage("✅ Recording stopped successfully");
            
            // Check if output file exists
            var outputFile = recordingService.CurrentOutputFile;
            if (!string.IsNullOrEmpty(outputFile))
            {
                LogMessage($"✅ Output file created: {System.IO.Path.GetFileName(outputFile)}");
                testsPassed++;
            }
            else
            {
                LogMessage("❌ No output file created");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// Test error handling
        /// </summary>
        private IEnumerator TestErrorHandling()
        {
            LogMessage("🧪 Test 5: Error Handling");
            
            if (recordingService == null)
            {
                LogMessage("❌ Cannot test error handling - service unavailable");
                testsFailed++;
                yield break;
            }
            
            // Test double start (should fail gracefully)
            recordingService.StartRecording();
            yield return new WaitForSeconds(0.5f);
            
            bool doubleStartResult = recordingService.StartRecording();
            if (!doubleStartResult)
            {
                LogMessage("✅ Double start properly rejected");
            }
            else
            {
                LogMessage("❌ Double start should have been rejected");
            }
            
            // Clean up
            recordingService.StopRecording();
            yield return new WaitForSeconds(1f);
            
            testsPassed++;
        }
        
        /// <summary>
        /// Get selected recording configuration
        /// </summary>
        private VideoRecordingConfig GetSelectedConfig()
        {
            if (qualityPresetDropdown == null)
                return VideoRecordingConfig.Default;
            
            return qualityPresetDropdown.value switch
            {
                0 => VideoRecordingConfig.Default,
                1 => VideoRecordingConfig.HighQuality,
                2 => VideoRecordingConfig.Performance,
                3 => new VideoRecordingConfig
                {
                    videoBitrate = 3000000,    // 3 Mbps custom
                    videoFrameRate = 24,       // 24 fps custom
                    videoFormat = "video/avc",
                    audioEnabled = false,
                    outputDirectory = "",
                    maxRecordingDurationMs = -1L
                },
                _ => VideoRecordingConfig.Default
            };
        }
        
        /// <summary>
        /// Get preset name
        /// </summary>
        private string GetPresetName()
        {
            if (qualityPresetDropdown == null)
                return "Default";
            
            return qualityPresetDropdown.value switch
            {
                0 => "Default",
                1 => "High Quality",
                2 => "Performance",
                3 => "Custom",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Event handlers
        /// </summary>
        private void OnRecordingStateChanged(RecordingState state)
        {
            LogMessage($"📱 State changed: {state}");
        }
        
        private void OnRecordingComplete(string outputPath)
        {
            var duration = (DateTime.Now - recordingStartTime).TotalSeconds;
            LogMessage($"✅ Recording completed in {duration:F1}s");
            LogMessage($"📁 Output: {System.IO.Path.GetFileName(outputPath)}");
        }
        
        private void OnRecordingError(string errorMessage)
        {
            LogMessage($"❌ Recording error: {errorMessage}");
        }
        
        /// <summary>
        /// Log message to UI and console
        /// </summary>
        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            
            Debug.Log(logEntry);
            
            if (logText != null)
            {
                logText.text += logEntry + "\n";
                
                // Limit log length
                var lines = logText.text.Split('\n');
                if (lines.Length > 50)
                {
                    logText.text = string.Join("\n", lines, lines.Length - 50, 50);
                }
                
                // Auto-scroll to bottom
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        /// <summary>
        /// Clear log display
        /// </summary>
        public void ClearLog()
        {
            if (logText != null)
            {
                logText.text = "";
            }
        }
        
        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (recordingService != null)
            {
                recordingService.OnRecordingStateChanged -= OnRecordingStateChanged;
                recordingService.OnRecordingComplete -= OnRecordingComplete;
                recordingService.OnRecordingError -= OnRecordingError;
            }
            
            if (automatedTestCoroutine != null)
            {
                StopCoroutine(automatedTestCoroutine);
            }
        }
    }
}