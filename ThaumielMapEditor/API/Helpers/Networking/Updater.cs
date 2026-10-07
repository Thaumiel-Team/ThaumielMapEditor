// -----------------------------------------------------------------------
// <copyright file="UpdateHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MEC;
using UnityEngine.Networking;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    public class Updater
    {
        public class GitHubReleaseInfo
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("prerelease")]
            public bool PreRelease { get; set; }

            [JsonPropertyName("assets")]
            public GitHubAssetInfo[] Assets { get; set; } = [];

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;
        }

        public class GitHubAssetInfo
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = string.Empty;
        }

        private const string PluginName = "ThaumielMapEditor.dll";
        private const string DependenciesZipName = "Dependencies.zip";
        private const string UserAgent = "ThaumielMapEditor-Updater/1.0.0";
        private const string ReleasesApiUrl = "https://api.github.com/repos/Mr-Baguetter/ThaumielMapEditor/releases";

        public static IEnumerator<float> CheckForUpdatesCoroutine(bool prerelease)
        {
            LogManager.Updater($"Current version: {Main.Instance.Version}. Checking for updates...");
            GitHubReleaseInfo? latestRelease = null;
            yield return Timing.WaitUntilDone(GetLatestReleaseCoroutine(prerelease, result => latestRelease = result));

            if (latestRelease == null)
                yield break;

            string latestVersionTag = latestRelease.TagName?.TrimStart('v') ?? string.Empty;
            if (Version.TryParse(latestVersionTag, out Version githubVersion))
            {
                LogManager.Updater($"Latest version: {githubVersion}.");
                if (githubVersion > Main.Instance.Version)
                {
                    if (latestRelease.PreRelease)
                    {
                        LogManager.Updater("A prerelease update is available! Run 'tmeupdate true true' to install it.");
                        if (!string.IsNullOrWhiteSpace(latestRelease.Body))
                            LogManager.Updater($"Changes: \n {latestRelease.Body}");
                    }
                    else
                    {
                        LogManager.Updater("An update is available! Run 'tmeupdate' to install it.");
                        if (!string.IsNullOrWhiteSpace(latestRelease.Body))
                            LogManager.Updater($"Changes: \n {latestRelease.Body}");
                    }
                }
                else if (githubVersion < Main.Instance.Version)
                {
                    LogManager.Updater("You are on a Pre Release or Developer version! :D");
                }
                else
                    LogManager.Updater("You are on the latest version.");
            }
        }

        public static IEnumerator<float> UpdatePluginCoroutine(bool force, bool prerelease)
        {
            GitHubReleaseInfo? latestRelease = null;
            yield return Timing.WaitUntilDone(GetLatestReleaseCoroutine(prerelease, result => latestRelease = result));

            if (latestRelease == null)
                yield break;

            GitHubAssetInfo? asset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(PluginName, StringComparison.OrdinalIgnoreCase));
            if (asset == null || string.IsNullOrEmpty(asset.BrowserDownloadUrl))
            {
                LogManager.Error($"Could not find the plugin DLL ('{PluginName}') in the latest GitHub release.");
                yield break;
            }

            string latestVersionTag = latestRelease.TagName?.TrimStart('v') ?? string.Empty;
            if (Version.TryParse(latestVersionTag, out Version latestGitHubVersion) && latestGitHubVersion <= Main.Instance.Version && !force)
            {
                LogManager.Updater("You are already on the latest version. Use 'tmeupdate force' to proceed anyway.");
                yield break;
            }

            LogManager.Updater($"Downloading new version from {asset.BrowserDownloadUrl}...");
            if (!string.IsNullOrWhiteSpace(latestRelease.Body))
                LogManager.Updater($"Changes for this version: \n {latestRelease.Body}");

            UnityWebRequest req = UnityWebRequest.Get(asset.BrowserDownloadUrl);
            req.SetRequestHeader("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(Main.Instance.Config!?.GithubToken))
                req.SetRequestHeader("Authorization", $"token {Main.Instance.Config!?.GithubToken}");

            req.downloadHandler = new DownloadHandlerBuffer();
            yield return Timing.WaitUntilDone(req.SendWebRequest());

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogManager.Error($"Failed to download plugin: {req.error} ({req.responseCode})");
                req.Dispose();
                yield break;
            }

            try
            {
                byte[] fileBytes = req.downloadHandler.data ?? [];
                LogManager.Updater($"{PluginName} downloaded successfully ({fileBytes.Length} bytes). Applying update...");

                File.WriteAllBytes(Main.Instance.FilePath, fileBytes);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to write plugin file: {ex}");
                yield break;
            }
            finally
            {
                req.Dispose();
            }

            GitHubAssetInfo? depsAsset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(DependenciesZipName, StringComparison.OrdinalIgnoreCase));
            if (depsAsset == null || string.IsNullOrEmpty(depsAsset.BrowserDownloadUrl))
            {
                LogManager.Updater("No Dependencies.zip found in release, skipping dependency update.");
            }
            else
            {
                LogManager.Updater($"Downloading {DependenciesZipName}...");

                UnityWebRequest depsReq = UnityWebRequest.Get(depsAsset.BrowserDownloadUrl);
                depsReq.SetRequestHeader("User-Agent", UserAgent);
                if (!string.IsNullOrEmpty(Main.Instance.Config!?.GithubToken))
                    depsReq.SetRequestHeader("Authorization", $"token {Main.Instance.Config!?.GithubToken}");

                depsReq.downloadHandler = new DownloadHandlerBuffer();
                yield return Timing.WaitUntilDone(depsReq.SendWebRequest());

                if (depsReq.result != UnityWebRequest.Result.Success)
                {
                    LogManager.Error($"Failed to download {DependenciesZipName}: {depsReq.error} ({depsReq.responseCode})");
                    depsReq.Dispose();
                }
                else
                {
                    try
                    {
                        byte[] zipBytes = depsReq.downloadHandler.data ?? [];
                        string dependenciesPath = Path.Combine(PathManager.Dependencies.FullName, Server.Port.ToString());

                        using MemoryStream zipStream = new(zipBytes);
                        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string entryPath = entry.FullName.Replace('\\', '/');
                            if (Path.IsPathRooted(entry.FullName) || entryPath.Contains(".."))
                            {
                                LogManager.Warn($"Skipping unsafe zip entry: '{entry.FullName}'");
                                continue;
                            }

                            string destinationPath = Path.Combine(dependenciesPath, entryPath);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(destinationPath);
                                continue;
                            }

                            string? destinationDir = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(destinationDir))
                                Directory.CreateDirectory(destinationDir);

                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }

                        LogManager.Updater($"{DependenciesZipName} extracted successfully to '{dependenciesPath}'.");
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to extract {DependenciesZipName}: {ex}");
                    }
                    finally
                    {
                        depsReq.Dispose();
                    }
                }
            }

            Server.RunCommand("rnr");
        }

        private static IEnumerator<float> GetLatestReleaseCoroutine(bool prerelease, Action<GitHubReleaseInfo> onComplete)
        {
            UnityWebRequest req = UnityWebRequest.Get(ReleasesApiUrl);
            req.SetRequestHeader("User-Agent", UserAgent);
            req.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            if (!string.IsNullOrEmpty(Main.Instance.Config!?.GithubToken))
                req.SetRequestHeader("Authorization", $"token {Main.Instance.Config!?.GithubToken}");

            req.downloadHandler = new DownloadHandlerBuffer();
            yield return Timing.WaitUntilDone(req.SendWebRequest());

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogManager.Error($"Failed to fetch release info from GitHub. Error: {req.error} ({req.responseCode})");
                req.Dispose();
                onComplete?.Invoke(null!);
                yield break;
            }

            try
            {
                string jsonResponse = req.downloadHandler.text;
                List<GitHubReleaseInfo>? releases = JsonSerializer.Deserialize<List<GitHubReleaseInfo>>(jsonResponse);

                if (releases == null || releases.Count == 0)
                {
                    onComplete?.Invoke(null!);
                    yield break;
                }

                IEnumerable<GitHubReleaseInfo> filtered = prerelease ? releases : releases.Where(r => !r.PreRelease);

                GitHubReleaseInfo chosen = filtered.OrderByDescending(r =>
                {
                    string tag = r.TagName?.TrimStart('v') ?? string.Empty;
                    return Version.TryParse(tag, out Version v) ? v : new Version(0, 0);
                }).FirstOrDefault();

                onComplete?.Invoke(chosen);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error parsing GitHub response: {ex.Message}");
                onComplete?.Invoke(null!);
            }
            finally
            {
                req.Dispose();
            }
        }
    }
}