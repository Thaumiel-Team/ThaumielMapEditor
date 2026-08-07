// -----------------------------------------------------------------------
// <copyright file="LogsUploader.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LabApi.Features;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine.Networking;
using System.IO;
using LabApi.Loader.Features.Paths;
using System.Text.RegularExpressions;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    internal class LogsUploader
    {
        private class AutoPayload
        {
            [JsonPropertyName("plugin_version")]
            public string PluginVersion { get; set; } = string.Empty;

            [JsonPropertyName("log_data")]
            public string LogData { get; set; } = string.Empty;
        }

        private class LogPayload
        {
            [JsonPropertyName("port")]
            public int Port { get; set; }

            [JsonPropertyName("labapi_version")]
            public string LabApiVersion { get; set; } = string.Empty;

            [JsonPropertyName("plugin_version")]
            public string PluginVersion { get; set; } = string.Empty;

            [JsonPropertyName("log_data")]
            public string LogData { get; set; } = string.Empty;

            [JsonPropertyName("local_admin_log_data")]
            public string LocalAdminLog { get; set; } = string.Empty;
        }

        public class LogUploadResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("size_bytes")]
            public int SizeBytes { get; set; }

            [JsonPropertyName("created_at")]
            public string CreatedAt { get; set; } = string.Empty;
        }

        public static bool GetLocalAdminConfig()
        {
            string portPath = Path.Combine(PathManager.SecretLab.ToString(), "config", Server.Port.ToString(), "config_localadmin.txt");
            string globalPath = Path.Combine(PathManager.SecretLab.ToString(), "config", "config_localadmin_global.txt");

            if (File.Exists(portPath))
            {
                if (IsLoggingEnabled(portPath))
                    return true;
            }

            if (File.Exists(globalPath))
            {
                if (IsLoggingEnabled(globalPath))
                    return true;
            }

            return false;
        }

        private static bool IsLoggingEnabled(string filePath)
        {
            foreach (string line in File.ReadLines(filePath))
            {
                if (line.Trim().ToLower().StartsWith("enable_la_logs: true"))
                    return true;
            }

            return false;
        }

        public static string GetLocalAdminLogs()
        {
            if (!Main.Instance.Config.AllowLocalAdminLogUpload || !GetLocalAdminConfig())
                return "Disabled";

            string logDir = Path.Combine(PathManager.SecretLab.ToString(), "LocalAdminLogs", Server.Port.ToString());
            if (!Directory.Exists(logDir))
                return string.Empty;

            DirectoryInfo directory = new(logDir);
            FileInfo latestFile = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            return latestFile?.FullName ?? string.Empty;
        }
        
        public static string ProcessLocalAdminLogs()
        {
            string logPath = GetLocalAdminLogs();
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
                return "No logs found.";

            string[] Keywords =
            [
                "The referenced script on this Behaviour",
                "Trying to access a shader",
                "The referenced script (Unknown) on this Behaviour"
            ];

            string ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";

            try
            {
                List<string> lines = [];

                using (FileStream fs = new(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        string? line = reader.ReadLine();
                        if (line != null)
                            lines.Add(line);
                    }
                }

                IEnumerable<string> filteredLines = lines
                    .Where(line => !Keywords.Any(spam => line.Contains(spam)))
                    .Select(line => Regex.Replace(line, ipPattern, "[REDACTED IP]"));

                return string.Join(Environment.NewLine, filteredLines);
            }
            catch (Exception ex)
            {
                return $"Error processing logs: {ex.Message}";
            }
        }
        
        public static CoroutineHandle SendRequest(Action<LogUploadResponse?> onComplete)
        {
            MECHelper.TryRunCoroutine(SendLogsCoroutine(onComplete), out var handle);
            return handle;
        }

        internal static void SendAutoRequest(string log)
        {
            if (!Main.Instance.Config.AutomaticErrorUpload)
                return;

            Timing.RunCoroutine(AutoErrorLogCoroutine(log));
        }

        private static IEnumerator<float> AutoErrorLogCoroutine(string log)
        {
            AutoPayload payload = new()
            {
                PluginVersion = Main.Instance.Version.ToString(3),
                LogData = log,
            };

            using UnityWebRequest request = new("https://tmelogs.thaumiel-servers.workers.dev/auto/upload", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return Timing.WaitUntilDone(request.SendWebRequest());
        }

        private static IEnumerator<float> SendLogsCoroutine(Action<LogUploadResponse?> onComplete)
        {
            LogPayload payload = new()
            {
                Port = Server.Port,
                LabApiVersion = LabApiProperties.CompiledVersion,
                PluginVersion = Main.Instance.Version.ToString(),
                LogData = string.Join("\n", LogManager.GetLogSnapshot().Select(l => $"[{l.LogTime}] [{l.LogLevel}] {l.Message}")),
                LocalAdminLog = ProcessLocalAdminLogs()
            };

            using UnityWebRequest request = new("https://tmelogs.thaumiel-servers.workers.dev/upload", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return Timing.WaitUntilDone(request.SendWebRequest());

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogManager.LogShare($"[Error]: Failed to send logs: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            LogUploadResponse? response = JsonSerializer.Deserialize<LogUploadResponse>(request.downloadHandler.text);
            if (response == null || !response.Success)
            {
                LogManager.LogShare("[Error]: Failed to upload logs, server returned unsuccessful response");
                onComplete?.Invoke(null);
                yield break;
            }

            onComplete?.Invoke(response);
        }
    }
}