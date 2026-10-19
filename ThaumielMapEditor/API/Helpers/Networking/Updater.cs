// -----------------------------------------------------------------------
// <copyright file="Updater.cs" company="Thaumiel Team">
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
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
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
        private const string ToolGunPluginName = "ThaumielMapEditor.ToolGun.dll";
        private const string ToolGunPluginDisplayName = "Thaumiel ToolGun";
        private const string ToolGunDependenciesZipName = "Toolgun-Dependencies.zip";
        private const string UserAgent = "ThaumielMapEditor-Updater/2.0.0";
        private const string ReleasesApiUrl = "https://api.github.com/repos/Thaumiel-Team/ThaumielMapEditor/releases";

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

            byte[]? pluginBytes = null;
            yield return Timing.WaitUntilDone(DownloadAssetCoroutine(asset, bytes => pluginBytes = bytes));

            if (pluginBytes == null)
                yield break;

            try
            {
                LogManager.Updater($"{PluginName} downloaded successfully ({pluginBytes.Length} bytes). Applying update...");
                File.WriteAllBytes(Main.Instance.FilePath, pluginBytes);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to write plugin file: {ex}");
                yield break;
            }

            GitHubAssetInfo? depsAsset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(DependenciesZipName, StringComparison.OrdinalIgnoreCase));
            if (depsAsset == null || string.IsNullOrEmpty(depsAsset.BrowserDownloadUrl))
            {
                LogManager.Updater($"No {DependenciesZipName} found in release, skipping dependency update.");
            }
            else
            {
                LogManager.Updater($"Downloading {DependenciesZipName}...");
                byte[]? depsBytes = null;
                yield return Timing.WaitUntilDone(DownloadAssetCoroutine(depsAsset, bytes => depsBytes = bytes));

                if (depsBytes != null)
                {
                    string dependenciesPath = Path.Combine(PathManager.Dependencies.FullName, Server.Port.ToString());
                    ExtractZip(depsBytes, dependenciesPath, DependenciesZipName);
                }
            }

            if (!TryGetToolGun(out Plugin? toolGunPlugin))
            {
                LogManager.Updater($"The '{ToolGunPluginDisplayName}' plugin is not loaded and enabled, skipping its update.");
            }
            else
            {
                yield return Timing.WaitUntilDone(UpdateToolGunCoroutine(latestRelease, toolGunPlugin!));
            }

            Server.RunCommand("rnr");
        }

        private static IEnumerator<float> UpdateToolGunCoroutine(GitHubReleaseInfo latestRelease, Plugin toolGunPlugin)
        {
            GitHubAssetInfo? toolGunAsset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(ToolGunPluginName, StringComparison.OrdinalIgnoreCase));
            if (toolGunAsset == null || string.IsNullOrEmpty(toolGunAsset.BrowserDownloadUrl))
            {
                LogManager.Error($"Could not find the ToolGun DLL ('{ToolGunPluginName}') in the latest GitHub release.");
            }
            else
            {
                LogManager.Updater($"Downloading {ToolGunPluginName}...");
                byte[]? toolGunBytes = null;
                yield return Timing.WaitUntilDone(DownloadAssetCoroutine(toolGunAsset, bytes => toolGunBytes = bytes));

                if (toolGunBytes != null)
                {
                    string toolGunPath = string.IsNullOrWhiteSpace(toolGunPlugin.FilePath) ? Path.Combine(PathManager.Plugins.FullName, "global", ToolGunPluginName) : toolGunPlugin.FilePath;

                    try
                    {
                        File.WriteAllBytes(toolGunPath, toolGunBytes);
                        LogManager.Updater($"{ToolGunPluginName} updated successfully at '{toolGunPath}'.");
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to write ToolGun plugin file: {ex}");
                    }
                }
            }

            GitHubAssetInfo? toolGunDepsAsset = latestRelease.Assets?.FirstOrDefault(a => a.Name.Equals(ToolGunDependenciesZipName, StringComparison.OrdinalIgnoreCase));
            if (toolGunDepsAsset == null || string.IsNullOrEmpty(toolGunDepsAsset.BrowserDownloadUrl))
            {
                LogManager.Updater($"No {ToolGunDependenciesZipName} found in release, skipping ToolGun dependency update.");
                yield break;
            }

            LogManager.Updater($"Downloading {ToolGunDependenciesZipName}...");
            byte[]? toolGunDepsBytes = null;
            yield return Timing.WaitUntilDone(DownloadAssetCoroutine(toolGunDepsAsset, bytes => toolGunDepsBytes = bytes));

            if (toolGunDepsBytes != null)
                ExtractZip(toolGunDepsBytes, PathManager.LabApi.FullName, ToolGunDependenciesZipName);
        }

        private static IEnumerator<float> DownloadAssetCoroutine(GitHubAssetInfo asset, Action<byte[]> onComplete)
        {
            UnityWebRequest req = UnityWebRequest.Get(asset.BrowserDownloadUrl);
            req.SetRequestHeader("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(Main.Instance.Config!?.GithubToken))
                req.SetRequestHeader("Authorization", $"token {Main.Instance.Config!?.GithubToken}");

            req.downloadHandler = new DownloadHandlerBuffer();
            yield return Timing.WaitUntilDone(req.SendWebRequest());

            try
            {
                if (req.result != UnityWebRequest.Result.Success)
                {
                    LogManager.Error($"Failed to download '{asset.Name}': {req.error} ({req.responseCode})");
                    yield break;
                }

                onComplete(req.downloadHandler.data ?? []);
            }
            finally
            {
                req.Dispose();
            }
        }

        private static bool TryGetToolGun(out Plugin? plugin)
        {
            plugin = PluginLoader.EnabledPlugins.FirstOrDefault(p => p.Name.Equals(ToolGunPluginDisplayName, StringComparison.OrdinalIgnoreCase)) ?? PluginLoader.EnabledPlugins.FirstOrDefault(p => Path.GetFileName(p.FilePath).Equals(ToolGunPluginName, StringComparison.OrdinalIgnoreCase));
            return plugin != null;
        }

        private static void ExtractZip(byte[] zipBytes, string destinationPath, string zipName)
        {
            try
            {
                Directory.CreateDirectory(destinationPath);

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

                    string destinationFile = Path.Combine(destinationPath, entryPath);
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationFile);
                        continue;
                    }

                    string? destinationDir = Path.GetDirectoryName(destinationFile);
                    if (!string.IsNullOrEmpty(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    entry.ExtractToFile(destinationFile, overwrite: true);
                }

                LogManager.Updater($"{zipName} extracted successfully to '{destinationPath}'.");
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to extract {zipName}: {ex}");
            }
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