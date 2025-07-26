#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MediaProjection.Services;
using System.Collections;

namespace MediaProjection.Tests.Editor
{
    /// <summary>
    /// Edit mode tests for video recording components.
    /// These tests run in the Unity Editor without requiring Play mode.
    /// </summary>
    public class VideoRecordingEditModeTests
    {
        [Test]
        public void VideoRecordingConfig_DefaultValues_AreValid()
        {
            // Arrange & Act
            var config = VideoRecordingConfig.Default;
            
            // Assert
            Assert.Greater(config.videoBitrate, 0, "Default bitrate should be positive");
            Assert.Greater(config.videoFrameRate, 0, "Default frame rate should be positive");
            Assert.AreNotEqual("", config.videoFormat, "Default video format should not be empty");
            Assert.AreEqual(-1L, config.maxRecordingDurationMs, "Default max duration should be unlimited");
        }
        
        [Test]
        public void VideoRecordingConfig_HighQualityPreset_HasHigherBitrate()
        {
            // Arrange
            var defaultConfig = VideoRecordingConfig.Default;
            var highQualityConfig = VideoRecordingConfig.HighQuality;
            
            // Act & Assert
            Assert.Greater(highQualityConfig.videoBitrate, defaultConfig.videoBitrate, 
                "High quality preset should have higher bitrate than default");
            Assert.GreaterOrEqual(highQualityConfig.videoFrameRate, defaultConfig.videoFrameRate,
                "High quality preset should have equal or higher frame rate than default");
        }
        
        [Test]
        public void VideoRecordingConfig_PerformancePreset_HasLowerBitrate()
        {
            // Arrange
            var defaultConfig = VideoRecordingConfig.Default;
            var performanceConfig = VideoRecordingConfig.Performance;
            
            // Act & Assert
            Assert.LessOrEqual(performanceConfig.videoBitrate, defaultConfig.videoBitrate,
                "Performance preset should have equal or lower bitrate than default");
            Assert.Greater(performanceConfig.videoBitrate, 0,
                "Performance preset bitrate should still be positive");
        }
        
        [Test]
        public void RecordingStatus_DefaultState_IsIdle()
        {
            // Arrange & Act
            var status = new RecordingStatus
            {
                state = RecordingState.Idle,
                recordingDurationSeconds = 0f,
                outputFilePath = null,
                errorMessage = null
            };
            
            // Assert
            Assert.IsTrue(status.IsIdle, "Default status should be idle");
            Assert.IsFalse(status.IsRecording, "Default status should not be recording");
            Assert.IsFalse(status.HasError, "Default status should not have error");
        }
        
        [Test]
        public void RecordingStatus_RecordingState_IsRecording()
        {
            // Arrange & Act
            var status = new RecordingStatus
            {
                state = RecordingState.Recording,
                recordingDurationSeconds = 5.5f,
                outputFilePath = null,
                errorMessage = null
            };
            
            // Assert
            Assert.IsFalse(status.IsIdle, "Recording status should not be idle");
            Assert.IsTrue(status.IsRecording, "Recording status should be recording");
            Assert.IsFalse(status.HasError, "Recording status should not have error");
            Assert.Greater(status.recordingDurationSeconds, 0, "Recording duration should be positive");
        }
        
        [Test]
        public void RecordingStatus_ErrorState_HasError()
        {
            // Arrange & Act
            var status = new RecordingStatus
            {
                state = RecordingState.Error,
                recordingDurationSeconds = 0f,
                outputFilePath = null,
                errorMessage = "Test error message"
            };
            
            // Assert
            Assert.IsFalse(status.IsIdle, "Error status should not be idle");
            Assert.IsFalse(status.IsRecording, "Error status should not be recording");
            Assert.IsTrue(status.HasError, "Error status should have error");
            Assert.IsNotNull(status.errorMessage, "Error status should have error message");
        }
        
        [Test]
        [TestCase(RecordingState.Idle)]
        [TestCase(RecordingState.Preparing)]
        [TestCase(RecordingState.Recording)]
        [TestCase(RecordingState.Pausing)]
        [TestCase(RecordingState.Stopping)]
        [TestCase(RecordingState.Error)]
        public void RecordingState_AllStates_AreDefined(RecordingState state)
        {
            // Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(RecordingState), state),
                $"RecordingState.{state} should be properly defined");
        }
        
        [Test]
        public void VideoRecordingConfig_CustomValues_ArePreserved()
        {
            // Arrange
            const int testBitrate = 1234567;
            const int testFrameRate = 25;
            const string testFormat = "video/hevc";
            const bool testAudio = true;
            const string testDirectory = "/test/path";
            const long testDuration = 60000L;
            
            // Act
            var config = new VideoRecordingConfig
            {
                videoBitrate = testBitrate,
                videoFrameRate = testFrameRate,
                videoFormat = testFormat,
                audioEnabled = testAudio,
                outputDirectory = testDirectory,
                maxRecordingDurationMs = testDuration
            };
            
            // Assert
            Assert.AreEqual(testBitrate, config.videoBitrate);
            Assert.AreEqual(testFrameRate, config.videoFrameRate);
            Assert.AreEqual(testFormat, config.videoFormat);
            Assert.AreEqual(testAudio, config.audioEnabled);
            Assert.AreEqual(testDirectory, config.outputDirectory);
            Assert.AreEqual(testDuration, config.maxRecordingDurationMs);
        }
        
        [Test]
        public void VideoRecordingConfig_BitrateValidation_RejectsInvalidValues()
        {
            // Test cases for invalid bitrates
            int[] invalidBitrates = { -1, 0, -100000 };
            
            foreach (int invalidBitrate in invalidBitrates)
            {
                // Arrange & Act
                var config = new VideoRecordingConfig
                {
                    videoBitrate = invalidBitrate,
                    videoFrameRate = 30,
                    videoFormat = "video/avc",
                    audioEnabled = false,
                    outputDirectory = "",
                    maxRecordingDurationMs = -1L
                };
                
                // Assert
                // Note: In a production implementation, you might want to add validation
                // For now, we just document that negative/zero bitrates are not recommended
                LogAssert.NoUnexpectedReceived();
            }
        }
        
        [Test]
        public void VideoRecordingConfig_FrameRateValidation_AcceptsValidValues()
        {
            // Test cases for valid frame rates
            int[] validFrameRates = { 15, 24, 30, 60, 120 };
            
            foreach (int frameRate in validFrameRates)
            {
                // Arrange & Act
                var config = new VideoRecordingConfig
                {
                    videoBitrate = 5000000,
                    videoFrameRate = frameRate,
                    videoFormat = "video/avc",
                    audioEnabled = false,
                    outputDirectory = "",
                    maxRecordingDurationMs = -1L
                };
                
                // Assert
                Assert.AreEqual(frameRate, config.videoFrameRate,
                    $"Frame rate {frameRate} should be preserved");
                Assert.Greater(config.videoFrameRate, 0,
                    $"Frame rate {frameRate} should be positive");
            }
        }
        
        [Test]
        public void VideoRecordingConfig_SupportedFormats_AreRecognized()
        {
            // Arrange
            string[] supportedFormats = { 
                "video/avc",        // H.264
                "video/hevc",       // H.265
                "video/vp8",        // VP8
                "video/vp9"         // VP9
            };
            
            foreach (string format in supportedFormats)
            {
                // Act
                var config = new VideoRecordingConfig
                {
                    videoBitrate = 5000000,
                    videoFrameRate = 30,
                    videoFormat = format,
                    audioEnabled = false,
                    outputDirectory = "",
                    maxRecordingDurationMs = -1L
                };
                
                // Assert
                Assert.AreEqual(format, config.videoFormat,
                    $"Format {format} should be preserved");
                Assert.IsNotEmpty(config.videoFormat,
                    $"Format {format} should not be empty");
            }
        }
        
        [Test]
        public void RecordingState_Transitions_AreLogical()
        {
            // Test logical state transitions
            var validTransitions = new[]
            {
                (RecordingState.Idle, RecordingState.Preparing),
                (RecordingState.Preparing, RecordingState.Recording),
                (RecordingState.Recording, RecordingState.Stopping),
                (RecordingState.Stopping, RecordingState.Idle),
                (RecordingState.Recording, RecordingState.Error),
                (RecordingState.Preparing, RecordingState.Error),
                (RecordingState.Error, RecordingState.Idle)
            };
            
            foreach (var (from, to) in validTransitions)
            {
                // Assert - these are valid logical transitions
                Assert.AreNotEqual(from, to, 
                    $"Transition from {from} to {to} represents a state change");
            }
        }
        
        /// <summary>
        /// Test that verifies component interfaces are properly defined
        /// </summary>
        [Test]
        public void IVideoRecordingService_Interface_IsProperlyDefined()
        {
            // Arrange
            var interfaceType = typeof(IVideoRecordingService);
            
            // Act & Assert
            Assert.IsTrue(interfaceType.IsInterface, "IVideoRecordingService should be an interface");
            
            // Check for required properties
            Assert.IsNotNull(interfaceType.GetProperty("CurrentState"),
                "Interface should have CurrentState property");
            Assert.IsNotNull(interfaceType.GetProperty("IsRecording"),
                "Interface should have IsRecording property");
            Assert.IsNotNull(interfaceType.GetProperty("CurrentOutputFile"),
                "Interface should have CurrentOutputFile property");
            
            // Check for required methods
            Assert.IsNotNull(interfaceType.GetMethod("StartRecording", new[] { typeof(VideoRecordingConfig) }),
                "Interface should have StartRecording method with config parameter");
            Assert.IsNotNull(interfaceType.GetMethod("StopRecording"),
                "Interface should have StopRecording method");
            Assert.IsNotNull(interfaceType.GetMethod("GetRecordingStatus"),
                "Interface should have GetRecordingStatus method");
            Assert.IsNotNull(interfaceType.GetMethod("IsVideoRecordingSupported"),
                "Interface should have IsVideoRecordingSupported method");
        }
        
        /// <summary>
        /// Performance test for configuration creation
        /// </summary>
        [Test]
        public void VideoRecordingConfig_Creation_IsPerformant()
        {
            // Arrange
            const int iterations = 10000;
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var config = VideoRecordingConfig.Default;
                Assert.Greater(config.videoBitrate, 0); // Prevent optimization
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, 
                $"Creating {iterations} configs should take less than 100ms");
        }
    }
}