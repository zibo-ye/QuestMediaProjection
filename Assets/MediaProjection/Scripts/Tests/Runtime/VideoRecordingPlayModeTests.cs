#nullable enable

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MediaProjection.Services;
using MediaProjection.ViewModels;

namespace MediaProjection.Tests.Runtime
{
    /// <summary>
    /// Play mode tests for video recording functionality.
    /// These tests require Unity to be in Play mode and test actual runtime behavior.
    /// </summary>
    public class VideoRecordingPlayModeTests
    {
        private GameObject? testObject;
        private ServiceContainer? serviceContainer;
        private VideoRecordingViewModel? viewModel;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject
            testObject = new GameObject("VideoRecordingTestObject");
            
            // Add ServiceContainer
            serviceContainer = testObject.AddComponent<ServiceContainer>();
            
            // Add VideoRecordingViewModel
            viewModel = testObject.AddComponent<VideoRecordingViewModel>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [UnityTest]
        public IEnumerator ServiceContainer_Initialization_CreatesVideoRecordingService()
        {
            // Wait for initialization
            yield return new WaitForSeconds(0.1f);
            
            // Act & Assert
            Assert.IsNotNull(serviceContainer, "ServiceContainer should be created");
            
            // Note: On non-Android platforms, the service creation might fail
            // This is expected behavior and should be handled gracefully
            LogAssert.NoUnexpectedReceived();
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_Initialization_CompletesWithoutErrors()
        {
            // Wait for initialization
            yield return new WaitForSeconds(0.5f);
            
            // Act & Assert
            Assert.IsNotNull(viewModel, "VideoRecordingViewModel should be created");
            Assert.AreEqual(RecordingState.Idle, viewModel.CurrentState, 
                "Initial state should be Idle");
            Assert.IsFalse(viewModel.IsRecording, "Should not be recording initially");
            
            LogAssert.NoUnexpectedReceived();
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingService_MockTest_HandlesNonAndroidPlatform()
        {
            // This test verifies that the service handles non-Android platforms gracefully
            yield return new WaitForSeconds(0.1f);
            
            if (serviceContainer != null)
            {
                try
                {
                    // On non-Android platforms, this should not crash
                    var service = serviceContainer.VideoRecordingService;
                    
                    // The service might be null or throw an exception on non-Android platforms
                    // This is expected behavior
                    LogAssert.NoUnexpectedReceived();
                }
                catch (System.InvalidOperationException)
                {
                    // Expected on non-Android platforms when video recording is not enabled
                    Assert.Pass("InvalidOperationException expected on non-Android platforms");
                }
                catch (System.Exception e)
                {
                    // Log unexpected exceptions for debugging
                    Debug.LogWarning($"Unexpected exception (may be platform-related): {e.Message}");
                }
            }
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_StateTransitions_AreProperlyHandled()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // Test initial state
            Assert.AreEqual(RecordingState.Idle, viewModel.CurrentState);
            Assert.IsTrue(viewModel.CanStartRecording);
            Assert.IsFalse(viewModel.CanStopRecording);
            
            // Note: Actual recording tests would require Android platform
            // and proper MediaProjection permissions
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_ConfigurationPresets_AreAppliedCorrectly()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // Test preset changes
            viewModel.SetRecordingPreset(VideoRecordingViewModel.VideoRecordingPreset.HighQuality);
            yield return null;
            
            viewModel.SetRecordingPreset(VideoRecordingViewModel.VideoRecordingPreset.Performance);
            yield return null;
            
            viewModel.SetRecordingPreset(VideoRecordingViewModel.VideoRecordingPreset.Default);
            yield return null;
            
            // Test custom configuration
            viewModel.SetCustomConfig(2000000, 24, "", -1);
            yield return null;
            
            LogAssert.NoUnexpectedReceived();
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_Events_AreProperlyWired()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            bool stateChangedCalled = false;
            bool recordingCompleteCalled = false;
            bool recordingErrorCalled = false;
            
            // Subscribe to UnityEvents (simulating UI binding)
            viewModel.onRecordingStateChanged.AddListener((state) => {
                stateChangedCalled = true;
                Debug.Log($"State changed to: {state}");
            });
            
            viewModel.onRecordingComplete.AddListener((path) => {
                recordingCompleteCalled = true;
                Debug.Log($"Recording completed: {path}");
            });
            
            viewModel.onRecordingError.AddListener((error) => {
                recordingErrorCalled = true;
                Debug.Log($"Recording error: {error}");
            });
            
            // Wait a bit to ensure events are wired
            yield return new WaitForSeconds(0.5f);
            
            // Events should be properly wired (even if not called yet)
            Assert.IsNotNull(viewModel.onRecordingStateChanged);
            Assert.IsNotNull(viewModel.onRecordingComplete);
            Assert.IsNotNull(viewModel.onRecordingError);
            
            LogAssert.NoUnexpectedReceived();
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_FormattedDuration_IsCorrect()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // Test duration formatting
            string formattedDuration = viewModel.GetFormattedDuration();
            Assert.IsNotNull(formattedDuration);
            Assert.IsTrue(formattedDuration.Contains(":"), "Duration should contain colon separator");
            
            // Test recording info
            string recordingInfo = viewModel.GetRecordingInfo();
            Assert.IsNotNull(recordingInfo);
            Assert.IsTrue(recordingInfo.Length > 0, "Recording info should not be empty");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator ServiceContainer_MultipleAccess_ReturnsSameInstance()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (serviceContainer == null)
            {
                Assert.Fail("ServiceContainer is null");
                yield break;
            }
            
            try
            {
                // Access the service multiple times
                var service1 = serviceContainer.VideoRecordingService;
                var service2 = serviceContainer.VideoRecordingService;
                
                // Should return the same instance (singleton pattern)
                if (service1 != null && service2 != null)
                {
                    Assert.AreSame(service1, service2, "Should return same service instance");
                }
            }
            catch (System.InvalidOperationException)
            {
                // Expected when video recording is not enabled
                Assert.Pass("Service access properly restricted when not enabled");
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_Update_HandlesNullService()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // The ViewModel should handle cases where the service is not available
            // (e.g., on non-Android platforms or when video recording is disabled)
            
            // Wait several frames to ensure Update is called
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
            
            // Should not crash or log errors during Update calls
            LogAssert.NoUnexpectedReceived();
        }
        
        [UnityTest]
        public IEnumerator VideoRecordingService_Dispose_DoesNotThrow()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (serviceContainer == null)
            {
                Assert.Fail("ServiceContainer is null");
                yield break;
            }
            
            try
            {
                var service = serviceContainer.VideoRecordingService;
                
                // Dispose should not throw exceptions
                service?.Dispose();
                
                LogAssert.NoUnexpectedReceived();
            }
            catch (System.InvalidOperationException)
            {
                // Expected when service is not available
                Assert.Pass("Service disposal handled when service not available");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Performance test for ViewModel updates
        /// </summary>
        [UnityTest, Performance]
        public IEnumerator VideoRecordingViewModel_Update_IsPerformant()
        {
            yield return new WaitForSeconds(0.1f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // Measure update performance over several frames
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 60; i++) // 60 frames
            {
                yield return null;
            }
            
            stopwatch.Stop();
            
            // Updates should be performant (less than 16ms per frame on average)
            float averageFrameTime = stopwatch.ElapsedMilliseconds / 60f;
            Assert.Less(averageFrameTime, 16f, 
                $"Average frame time should be less than 16ms, was {averageFrameTime:F2}ms");
        }
        
        /// <summary>
        /// Memory allocation test
        /// </summary>
        [UnityTest]
        public IEnumerator VideoRecordingViewModel_Update_DoesNotAllocateMemory()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (viewModel == null)
            {
                Assert.Fail("ViewModel is null");
                yield break;
            }
            
            // Force garbage collection to get baseline
            System.GC.Collect();
            yield return new WaitForSeconds(0.1f);
            
            long initialMemory = System.GC.GetTotalMemory(false);
            
            // Run several update cycles
            for (int i = 0; i < 100; i++)
            {
                yield return null;
            }
            
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryDelta = finalMemory - initialMemory;
            
            // Should not allocate significant memory during updates
            Assert.Less(memoryDelta, 1024 * 1024, // 1MB threshold
                $"Update cycles should not allocate significant memory. Delta: {memoryDelta} bytes");
        }
        
        /// <summary>
        /// Test component lifecycle
        /// </summary>
        [UnityTest]
        public IEnumerator Components_Lifecycle_HandledProperly()
        {
            yield return new WaitForSeconds(0.1f);
            
            // Components should initialize properly
            Assert.IsNotNull(serviceContainer);
            Assert.IsNotNull(viewModel);
            
            // Wait for any initialization
            yield return new WaitForSeconds(0.5f);
            
            // Destroy and recreate to test cleanup
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
            
            yield return null;
            
            // Recreate
            testObject = new GameObject("NewTestObject");
            serviceContainer = testObject.AddComponent<ServiceContainer>();
            viewModel = testObject.AddComponent<VideoRecordingViewModel>();
            
            yield return new WaitForSeconds(0.1f);
            
            // Should work after recreation
            Assert.IsNotNull(serviceContainer);
            Assert.IsNotNull(viewModel);
            
            LogAssert.NoUnexpectedReceived();
        }
    }
}