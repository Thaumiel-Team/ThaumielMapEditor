// -----------------------------------------------------------------------
// <copyright file="LogUploadRequest.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using ThaumielMapEditor.API.Enums;
using UnityEngine.Networking;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    public class LogUploadRequest : NetworkRequest
    {
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

        public override string Name => nameof(LogUploadRequest);

        public override RequestType Method => RequestType.Post;

        public override string Endpoint => "upload";

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

        public override void SendRequest(Action<UnityWebRequest>? onComplete = null)
        {
            Payload = new()
            {
                ["port"] = Server.Port,
                ["labapi_version"] = LabApiProperties.CurrentVersion.ToString(),
                ["plugin_version"] = Main.Instance.Version.ToString(),
                ["log_data"] = string.Join("\n", LogManager.GetLogSnapshot().Select(l => $"[{l.LogTime}] [{l.LogLevel}] {l.Message}")),
                ["local_admin_log_data"] = ProcessLocalAdminLogs()
            };

            base.SendRequest(onComplete);
        }

        public override void OnRequestCompleted(UnityWebRequest request)
        {
            LogUploadResponse? response = JsonSerializer.Deserialize<LogUploadResponse>(request.downloadHandler.text);
            if (response == null || !response.Success)
            {
                LogManager.LogShare("[Error]: Failed to upload logs, server returned unsuccessful response");
                return;
            }
            
            LogManager.LogShare($"[Info]: Please share this with the developers. [Id: {response.Id}] Success: {response.Success}, Created: {response.CreatedAt}");
            base.OnRequestCompleted(request);
        }
    }
}