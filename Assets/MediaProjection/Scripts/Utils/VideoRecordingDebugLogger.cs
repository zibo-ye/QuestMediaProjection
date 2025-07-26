#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using MediaProjection.Services;

namespace MediaProjection.Utils
{
    /// <summary>
    /// Enhanced debug logging system for video recording functionality.
    /// Provides detailed logging, performance tracking, and log export capabilities.
    /// </summary>
    public static class VideoRecordingDebugLogger
    {
        private static readonly Queue<LogEntry> logHistory = new();
        private static readonly object logLock = new();
        private static bool isEnabled = true;
        private static bool enablePerformanceLogging = true;
        private static bool enableDetailedAndroidLogs = true;
        private static int maxLogEntries = 1000;
        
        // Performance tracking
        private static readonly Dictionary<string, PerformanceTracker> performanceTrackers = new();
        
        public enum LogLevel
        {
            Verbose = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4
        }
        
        private struct LogEntry
        {
            public DateTime timestamp;
            public LogLevel level;
            public string category;
            public string message;
            public string? stackTrace;
            
            public LogEntry(LogLevel level, string category, string message, string? stackTrace = null)
            {
                this.timestamp = DateTime.Now;
                this.level = level;
                this.category = category;
                this.message = message;
                this.stackTrace = stackTrace;
            }
        }
        
        private class PerformanceTracker
        {
            public readonly Stopwatch stopwatch = new();
            public int callCount = 0;
            public long totalMilliseconds = 0;
            public long maxMilliseconds = 0;
            public long minMilliseconds = long.MaxValue;
            
            public void Start()
            {
                stopwatch.Restart();
            }
            
            public void Stop()
            {
                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                
                callCount++;
                totalMilliseconds += elapsed;
                maxMilliseconds = Math.Max(maxMilliseconds, elapsed);
                minMilliseconds = Math.Min(minMilliseconds, elapsed);
            }
            
            public double AverageMilliseconds => callCount > 0 ? (double)totalMilliseconds / callCount : 0;
        }
        
        /// <summary>
        /// Initialize the debug logger with configuration
        /// </summary>
        public static void Initialize(bool enabled = true, bool performanceLogging = true, bool detailedAndroidLogs = true, int maxEntries = 1000)
        {
            isEnabled = enabled;
            enablePerformanceLogging = performanceLogging;
            enableDetailedAndroidLogs = detailedAndroidLogs;
            maxLogEntries = maxEntries;
            
            if (isEnabled)
            {
                LogInfo("Logger", "VideoRecordingDebugLogger initialized");
                LogInfo("System", $"Platform: {Application.platform}");
                LogInfo("System", $"Unity Version: {Application.unityVersion}");
                LogInfo("System", $"System Memory: {SystemInfo.systemMemorySize}MB");
                LogInfo("System", $"Graphics Device: {SystemInfo.graphicsDeviceName}");
                
                if (Application.platform == RuntimePlatform.Android)
                {
                    LogAndroidSystemInfo();
                }
            }
        }
        
        /// <summary>
        /// Log Android-specific system information
        /// </summary>
        private static void LogAndroidSystemInfo()
        {
            try
            {
                using var buildClass = new AndroidJavaClass("android.os.Build");
                using var versionClass = new AndroidJavaClass("android.os.Build$VERSION");
                
                LogInfo("Android", $"Device: {buildClass.GetStatic<string>("MANUFACTURER")} {buildClass.GetStatic<string>("MODEL")}");
                LogInfo("Android", $"SDK: {versionClass.GetStatic<int>("SDK_INT")} ({versionClass.GetStatic<string>("RELEASE")})");
                LogInfo("Android", $"Board: {buildClass.GetStatic<string>("BOARD")}");
                LogInfo("Android", $"Bootloader: {buildClass.GetStatic<string>("BOOTLOADER")}");
                
                // Check MediaProjection support
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                
                var hasMediaProjection = context.Call<AndroidJavaObject>("getSystemService", "media_projection") != null;
                LogInfo("Android", $"MediaProjection Service Available: {hasMediaProjection}");
                
            }
            catch (Exception e)
            {
                LogError("Android", $"Failed to get Android system info: {e.Message}");
            }
        }
        
        /// <summary>
        /// Log verbose message
        /// </summary>
        public static void LogVerbose(string category, string message)
        {
            Log(LogLevel.Verbose, category, message);
        }
        
        /// <summary>
        /// Log debug message
        /// </summary>
        public static void LogDebug(string category, string message)
        {
            Log(LogLevel.Debug, category, message);
        }
        
        /// <summary>
        /// Log info message
        /// </summary>
        public static void LogInfo(string category, string message)
        {
            Log(LogLevel.Info, category, message);
        }
        
        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(string category, string message)
        {
            Log(LogLevel.Warning, category, message);
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(string category, string message, Exception? exception = null)
        {
            var fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
            var stackTrace = exception?.StackTrace ?? Environment.StackTrace;
            Log(LogLevel.Error, category, fullMessage, stackTrace);
        }
        
        /// <summary>
        /// Log recording state change
        /// </summary>
        public static void LogStateChange(RecordingState oldState, RecordingState newState, string context = "")
        {
            LogInfo("StateChange", $"{oldState} → {newState}" + (string.IsNullOrEmpty(context) ? "" : $" ({context})"));
        }
        
        /// <summary>
        /// Log recording configuration
        /// </summary>
        public static void LogRecordingConfig(VideoRecordingConfig config)
        {
            LogInfo("Config", $"Bitrate: {config.videoBitrate / 1000000f:F1}Mbps");
            LogInfo("Config", $"Frame Rate: {config.videoFrameRate}fps");
            LogInfo("Config", $"Format: {config.videoFormat}");
            LogInfo("Config", $"Audio: {config.audioEnabled}");
            LogInfo("Config", $"Output Dir: {(string.IsNullOrEmpty(config.outputDirectory) ? "Default" : config.outputDirectory)}");
            LogInfo("Config", $"Max Duration: {(config.maxRecordingDurationMs == -1 ? "Unlimited" : $"{config.maxRecordingDurationMs / 1000}s")}");
        }
        
        /// <summary>
        /// Start performance tracking for an operation
        /// </summary>
        public static void StartPerformanceTracking(string operationName)
        {
            if (!enablePerformanceLogging) return;
            
            lock (performanceTrackers)
            {
                if (!performanceTrackers.ContainsKey(operationName))
                {
                    performanceTrackers[operationName] = new PerformanceTracker();
                }
                
                performanceTrackers[operationName].Start();
            }
        }
        
        /// <summary>
        /// Stop performance tracking for an operation
        /// </summary>
        public static void StopPerformanceTracking(string operationName)
        {
            if (!enablePerformanceLogging) return;
            
            lock (performanceTrackers)
            {
                if (performanceTrackers.TryGetValue(operationName, out var tracker))
                {
                    tracker.Stop();
                    
                    if (tracker.callCount % 10 == 0) // Log every 10th call
                    {
                        LogDebug("Performance", 
                            $"{operationName}: {tracker.stopwatch.ElapsedMilliseconds}ms " +
                            $"(avg: {tracker.AverageMilliseconds:F1}ms, " +
                            $"min: {tracker.minMilliseconds}ms, " +
                            $"max: {tracker.maxMilliseconds}ms, " +
                            $"calls: {tracker.callCount})");
                    }
                }
            }
        }
        
        /// <summary>
        /// Log performance summary
        /// </summary>
        public static void LogPerformanceSummary()
        {
            if (!enablePerformanceLogging) return;
            
            LogInfo("Performance", "=== Performance Summary ===");
            
            lock (performanceTrackers)
            {
                foreach (var kvp in performanceTrackers)
                {
                    var tracker = kvp.Value;
                    if (tracker.callCount > 0)
                    {
                        LogInfo("Performance", 
                            $"{kvp.Key}: " +
                            $"Calls: {tracker.callCount}, " +
                            $"Total: {tracker.totalMilliseconds}ms, " +
                            $"Avg: {tracker.AverageMilliseconds:F2}ms, " +
                            $"Min: {tracker.minMilliseconds}ms, " +
                            $"Max: {tracker.maxMilliseconds}ms");
                    }
                }
            }
        }
        
        /// <summary>
        /// Log memory usage
        /// </summary>
        public static void LogMemoryUsage(string context = "")
        {
            var gcMemory = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            var unityMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024); // MB
            
            LogInfo("Memory", $"{context}GC Memory: {gcMemory}MB, Unity Memory: {unityMemory}MB");
        }
        
        /// <summary>
        /// Log Android logcat message (if available)
        /// </summary>
        public static void LogAndroid(string tag, string message)
        {
            if (!enableDetailedAndroidLogs || Application.platform != RuntimePlatform.Android) return;
            
            try
            {
                using var logClass = new AndroidJavaClass("android.util.Log");
                logClass.CallStatic<int>("d", $"Unity-{tag}", message);
            }
            catch (Exception e)
            {
                LogError("AndroidLog", $"Failed to log to Android: {e.Message}");
            }
        }
        
        /// <summary>
        /// Core logging method
        /// </summary>
        private static void Log(LogLevel level, string category, string message, string? stackTrace = null)
        {
            if (!isEnabled) return;
            
            var entry = new LogEntry(level, category, message, stackTrace);
            
            // Add to history with size limit
            lock (logLock)
            {
                logHistory.Enqueue(entry);
                while (logHistory.Count > maxLogEntries)
                {
                    logHistory.Dequeue();
                }
            }
            
            // Log to Unity console
            var formattedMessage = $"[{category}] {message}";
            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
            }
            
            // Log to Android logcat if available
            if (enableDetailedAndroidLogs)
            {
                LogAndroid(category, message);
            }
        }
        
        /// <summary>
        /// Export logs to file
        /// </summary>
        public static void ExportLogs(string? filePath = null)
        {
            try
            {
                filePath ??= Path.Combine(Application.persistentDataPath, $"video_recording_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                var logContent = new System.Text.StringBuilder();
                logContent.AppendLine($"Video Recording Debug Logs");
                logContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logContent.AppendLine($"Platform: {Application.platform}");
                logContent.AppendLine($"Unity Version: {Application.unityVersion}");
                logContent.AppendLine(new string('=', 50));
                logContent.AppendLine();
                
                lock (logLock)
                {
                    foreach (var entry in logHistory)
                    {
                        logContent.AppendLine($"[{entry.timestamp:HH:mm:ss.fff}] [{entry.level}] [{entry.category}] {entry.message}");
                        if (!string.IsNullOrEmpty(entry.stackTrace))
                        {
                            logContent.AppendLine($"Stack Trace: {entry.stackTrace}");
                        }
                        logContent.AppendLine();
                    }
                }
                
                // Add performance summary
                logContent.AppendLine(new string('=', 50));
                logContent.AppendLine("Performance Summary:");
                lock (performanceTrackers)
                {
                    foreach (var kvp in performanceTrackers)
                    {
                        var tracker = kvp.Value;
                        if (tracker.callCount > 0)
                        {
                            logContent.AppendLine($"{kvp.Key}: Calls={tracker.callCount}, Avg={tracker.AverageMilliseconds:F2}ms, Min={tracker.minMilliseconds}ms, Max={tracker.maxMilliseconds}ms");
                        }
                    }
                }
                
                File.WriteAllText(filePath, logContent.ToString());
                LogInfo("Export", $"Logs exported to: {filePath}");
            }
            catch (Exception e)
            {
                LogError("Export", $"Failed to export logs: {e.Message}", e);
            }
        }
        
        /// <summary>
        /// Clear log history
        /// </summary>
        public static void ClearLogs()
        {
            lock (logLock)
            {
                logHistory.Clear();
            }
            LogInfo("Logger", "Log history cleared");
        }
        
        /// <summary>
        /// Get current log count
        /// </summary>
        public static int GetLogCount()
        {
            lock (logLock)
            {
                return logHistory.Count;
            }
        }
        
        /// <summary>
        /// Get recent logs as formatted string
        /// </summary>
        public static string GetRecentLogs(int count = 50)
        {
            var result = new System.Text.StringBuilder();
            lock (logLock)
            {
                var logs = new LogEntry[logHistory.Count];
                logHistory.CopyTo(logs, 0);
                
                var startIndex = Math.Max(0, logs.Length - count);
                for (int i = startIndex; i < logs.Length; i++)
                {
                    var entry = logs[i];
                    result.AppendLine($"[{entry.timestamp:HH:mm:ss}] [{entry.level}] [{entry.category}] {entry.message}");
                }
            }
            return result.ToString();
        }
        
        /// <summary>
        /// Log recording session summary
        /// </summary>
        public static void LogRecordingSession(string outputFile, TimeSpan duration, VideoRecordingConfig config)
        {
            LogInfo("Session", "=== Recording Session Summary ===");
            LogInfo("Session", $"Output: {Path.GetFileName(outputFile)}");
            LogInfo("Session", $"Duration: {duration.TotalSeconds:F1}s");
            LogInfo("Session", $"Bitrate: {config.videoBitrate / 1000000f:F1} Mbps");
            LogInfo("Session", $"Frame Rate: {config.videoFrameRate} fps");
            LogInfo("Session", $"Estimated Size: {(config.videoBitrate * duration.TotalSeconds / 8 / 1024 / 1024):F1} MB");
            
            // Log file info if available
            try
            {
                if (File.Exists(outputFile))
                {
                    var fileInfo = new FileInfo(outputFile);
                    LogInfo("Session", $"Actual File Size: {fileInfo.Length / 1024f / 1024f:F1} MB");
                }
            }
            catch (Exception e)
            {
                LogWarning("Session", $"Could not get file info: {e.Message}");
            }
            
            LogMemoryUsage("Post-Recording ");
            LogPerformanceSummary();
        }
    }
}