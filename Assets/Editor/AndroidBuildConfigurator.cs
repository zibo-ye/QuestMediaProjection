#nullable enable

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MediaProjection.Editor
{
    /// <summary>
    /// Generic Android build configurator for MediaProjection.
    /// Configures Android build settings targeting API 34+.
    /// </summary>
    public class AndroidBuildConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        [MenuItem("Build/Configure Android Settings")]
        public static void ConfigureAndroidSettings()
        {
            Debug.Log("Configuring Android build settings...");
            
            // Set platform to Android
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            // Android API 34+ settings
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            
            // Development settings
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            
            // Graphics settings
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
            
            // Basic optimization
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);
            
            Debug.Log("Android build configuration completed");
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Build/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            ConfigureAndroidSettings();
            
            var outputPath = System.IO.Path.Combine(Application.dataPath, "..", "Build", "2.apk");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath)!);
            
            var buildOptions = BuildOptions.Development | BuildOptions.AllowDebugging;
            
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Length > 0 ? 
                    System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path) :
                    new[] { "Assets/Scenes/MediaProjectionScene.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = buildOptions
            };
            
            Debug.Log($"Building APK to: {outputPath}");
            
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {outputPath}");
                EditorUtility.DisplayDialog("Build Complete", $"APK built successfully!\n\nOutput: {outputPath}", "OK");
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                EditorUtility.DisplayDialog("Build Failed", $"Build failed: {report.summary.result}", "OK");
            }
        }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                Debug.Log("Preprocessing Android build...");
                ValidateAndroidSettings();
            }
        }
        
        private static void ValidateAndroidSettings()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel34)
            {
                issues.Add("Minimum SDK version must be 34+ for this build");
            }
            
            if (PlayerSettings.Android.targetSdkVersion < AndroidSdkVersions.AndroidApiLevel34)
            {
                issues.Add("Target SDK version must be 34+ for this build");
            }
            
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                issues.Add("IL2CPP required for this build");
            }
            
            if (issues.Count > 0)
            {
                Debug.LogWarning("Build validation issues: " + string.Join(", ", issues));
            }
            else
            {
                Debug.Log("Android build validation passed");
            }
        }
    }
}