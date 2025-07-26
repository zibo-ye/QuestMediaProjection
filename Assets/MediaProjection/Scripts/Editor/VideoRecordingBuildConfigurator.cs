#nullable enable

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

namespace MediaProjection.Editor
{
    /// <summary>
    /// Build configurator for video recording functionality.
    /// Automatically configures build settings for different Android targets.
    /// </summary>
    public class VideoRecordingBuildConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        [MenuItem("MediaProjection/Build Settings/Configure for Android Phone")]
        public static void ConfigureForAndroidPhone()
        {
            ConfigureBuildSettings(BuildTarget.AndroidPhone, false);
        }
        
        [MenuItem("MediaProjection/Build Settings/Configure for Quest 3")]
        public static void ConfigureForQuest3()
        {
            ConfigureBuildSettings(BuildTarget.Quest, true);
        }
        
        [MenuItem("MediaProjection/Build Settings/Build Android Phone APK")]
        public static void BuildAndroidPhoneAPK()
        {
            ConfigureForAndroidPhone();
            BuildAPK("AndroidPhone");
        }
        
        [MenuItem("MediaProjection/Build Settings/Build Quest 3 APK")]
        public static void BuildQuest3APK()
        {
            ConfigureForQuest3();
            BuildAPK("Quest3");
        }
        
        [MenuItem("MediaProjection/Testing/Run Unit Tests")]
        public static void RunUnitTests()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }
        
        [MenuItem("MediaProjection/Testing/Export Debug Logs")]
        public static void ExportDebugLogs()
        {
            var outputPath = Path.Combine(Application.dataPath, "..", "Logs", $"build_logs_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            
            var logContent = $"Build Configuration Export\n" +
                           $"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                           $"Unity Version: {Application.unityVersion}\n" +
                           $"Target Platform: {EditorUserBuildSettings.activeBuildTarget}\n" +
                           $"Development Build: {EditorUserBuildSettings.development}\n" +
                           $"Script Debugging: {EditorUserBuildSettings.allowDebugging}\n" +
                           $"IL2CPP Code Generation: {PlayerSettings.GetIl2CppCodeGeneration(BuildTargetGroup.Android)}\n" +
                           $"API Level: Min={PlayerSettings.Android.minSdkVersion}, Target={PlayerSettings.Android.targetSdkVersion}\n";
            
            File.WriteAllText(outputPath, logContent);
            EditorUtility.DisplayDialog("Export Complete", $"Build configuration exported to:\n{outputPath}", "OK");
        }
        
        public enum BuildTarget
        {
            AndroidPhone,
            Quest
        }
        
        private static void ConfigureBuildSettings(BuildTarget target, bool isVR)
        {
            Debug.Log($"Configuring build settings for {target}");
            
            // Set platform to Android
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, UnityEditor.BuildTarget.Android);
            
            // Common Android settings
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29; // Required for MediaProjection
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34; // Android 14 for foreground service
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            
            // Video recording specific settings
            PlayerSettings.Android.useCustomKeystore = false; // Use debug keystore for testing
            PlayerSettings.SetIl2CppCodeGeneration(BuildTargetGroup.Android, Il2CppCodeGeneration.OptimizeSpeed);
            
            // Development settings
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            EditorUserBuildSettings.buildScriptsOnly = false;
            
            if (target == BuildTarget.AndroidPhone)
            {
                ConfigureForAndroidPhoneSpecific();
            }
            else if (target == BuildTarget.Quest)
            {
                ConfigureForQuestSpecific();
            }
            
            // Graphics settings for performance
            PlayerSettings.SetGraphicsAPIs(UnityEditor.BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
            
            // Optimization settings
            PlayerSettings.stripEngineCode = false; // Keep for debugging
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);
            
            Debug.Log($"Build configuration completed for {target}");
            AssetDatabase.SaveAssets();
        }
        
        private static void ConfigureForAndroidPhoneSpecific()
        {
            Debug.Log("Applying Android Phone specific settings");
            
            // Standard Android phone settings
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.ScaleToFill;
            
            // Performance settings for phones
            PlayerSettings.Android.blitType = AndroidBlitType.Auto;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mediaprojection.phonetest");
            
            // Disable VR settings
            UnityEngine.XR.XRSettings.enabled = false;
        }
        
        private static void ConfigureForQuestSpecific()
        {
            Debug.Log("Applying Quest 3 specific settings");
            
            // Quest/VR specific settings
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            
            // VR settings
            PlayerSettings.SetVirtualRealitySupported(BuildTargetGroup.Android, true);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mediaprojection.questtest");
            
            // Quest performance optimizations
            PlayerSettings.Android.blitType = AndroidBlitType.Always;
            QualitySettings.vSyncCount = 0; // VR handles its own vsync
            
            // Configure XR settings
            ConfigureXRSettings();
        }
        
        private static void ConfigureXRSettings()
        {
            try
            {
                // This would require XR Management package to be properly configured
                Debug.Log("XR settings configuration (requires XR packages)");
                
                // Enable OpenXR if available
                var xrGeneralSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
                if (xrGeneralSettings != null)
                {
                    Debug.Log("XR Management found, configuring for Quest");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"XR configuration skipped: {e.Message}");
            }
        }
        
        private static void BuildAPK(string targetName)
        {
            var outputPath = Path.Combine(Application.dataPath, "..", "Builds", targetName, $"VideoRecording_{targetName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            
            // Configure scenes to build
            var scenesToBuild = new[]
            {
                "Assets/Scenes/SampleScene.unity" // Default scene
            };
            
            // Look for test scenes
            var testScenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var sceneList = new System.Collections.Generic.List<string>();
            
            foreach (var sceneGuid in testScenes)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (scenePath.Contains("Test") || scenePath.Contains("VideoRecording"))
                {
                    sceneList.Add(scenePath);
                }
            }
            
            if (sceneList.Count == 0)
            {
                // Use any available scene
                sceneList.AddRange(scenesToBuild);
            }
            
            var buildOptions = BuildOptions.Development | BuildOptions.AllowDebugging;
            
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sceneList.ToArray(),
                locationPathName = outputPath,
                target = UnityEditor.BuildTarget.Android,
                options = buildOptions
            };
            
            Debug.Log($"Starting build for {targetName}...");
            Debug.Log($"Output: {outputPath}");
            Debug.Log($"Scenes: {string.Join(", ", sceneList)}");
            
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {outputPath}");
                Debug.Log($"Build size: {report.summary.totalSize} bytes");
                Debug.Log($"Build time: {report.summary.totalTime}");
                
                EditorUtility.DisplayDialog("Build Complete", 
                    $"Build succeeded!\n\nOutput: {outputPath}\n\nSize: {report.summary.totalSize / (1024*1024):F1} MB\nTime: {report.summary.totalTime}", 
                    "OK");
                
                // Offer to reveal in explorer
                if (EditorUtility.DisplayDialog("Build Complete", "Open build folder?", "Yes", "No"))
                {
                    EditorUtility.RevealInFinder(outputPath);
                }
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                
                // Show build errors
                var errors = new System.Text.StringBuilder();
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            errors.AppendLine($"• {message.content}");
                        }
                    }
                }
                
                EditorUtility.DisplayDialog("Build Failed", 
                    $"Build failed with result: {report.summary.result}\n\nErrors:\n{errors}", 
                    "OK");
            }
        }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Preprocessing build for video recording...");
            
            // Validate build settings
            if (report.summary.platform == UnityEditor.BuildTarget.Android)
            {
                ValidateAndroidBuildSettings();
            }
        }
        
        private static void ValidateAndroidBuildSettings()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            // Check SDK versions
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29)
            {
                issues.Add("Minimum SDK version should be 29+ for MediaProjection API");
            }
            
            if (PlayerSettings.Android.targetSdkVersion < AndroidSdkVersions.AndroidApiLevel34)
            {
                issues.Add("Target SDK version should be 34+ for Android 14 foreground service support");
            }
            
            // Check scripting backend
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                issues.Add("IL2CPP scripting backend recommended for better performance");
            }
            
            // Check architecture
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                issues.Add("ARM64 architecture required for modern Android devices");
            }
            
            // Check permissions in manifest
            var manifestPath = Path.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml");
            if (File.Exists(manifestPath))
            {
                var manifestContent = File.ReadAllText(manifestPath);
                if (!manifestContent.Contains("FOREGROUND_SERVICE_MEDIA_PROJECTION"))
                {
                    issues.Add("AndroidManifest.xml missing FOREGROUND_SERVICE_MEDIA_PROJECTION permission");
                }
            }
            
            if (issues.Count > 0)
            {
                var message = "Build validation found issues:\n\n" + string.Join("\n", issues);
                
                if (EditorUtility.DisplayDialog("Build Validation", message + "\n\nContinue anyway?", "Continue", "Cancel"))
                {
                    Debug.LogWarning("Build validation issues ignored by user");
                }
                else
                {
                    throw new BuildFailedException("Build cancelled due to validation issues");
                }
            }
            else
            {
                Debug.Log("Build validation passed");
            }
        }
        
        [MenuItem("MediaProjection/Documentation/Open Architecture Documentation")]
        public static void OpenArchitectureDocumentation()
        {
            var readmePath = Path.Combine(Application.dataPath, "..", "README.md");
            if (File.Exists(readmePath))
            {
                Application.OpenURL("file://" + readmePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", "README.md not found in project root", "OK");
            }
        }
        
        [MenuItem("MediaProjection/Documentation/Open CLAUDE.md")]
        public static void OpenClaudeDocumentation()
        {
            var claudePath = Path.Combine(Application.dataPath, "..", "CLAUDE.md");
            if (File.Exists(claudePath))
            {
                Application.OpenURL("file://" + claudePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", "CLAUDE.md not found in project root", "OK");
            }
        }
    }
}