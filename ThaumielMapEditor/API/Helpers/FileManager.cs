// -----------------------------------------------------------------------
// <copyright file="FileManager.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using LabApi.Loader.Features.Paths;

namespace ThaumielMapEditor.API.Helpers
{
    public class FileManager
    {
        /// <summary>
        /// Invoked when <see cref="ReadFileInBackground"/> is called.
        /// </summary>
        /// <remarks>
        /// Already dispatched by <see cref="MainThreadDispatcher"/>.
        /// Arg1 = File path
        /// Arg2 = File content (Can be empty if failed to read) 
        /// </remarks>
        public static event Action<string, string>? OnFileReadOnBackgroundThread;

        /// <summary>
        /// Gets the Thaumiel directory
        /// </summary>
        /// <returns>Directory path to the Thaumiel directory</returns>
        public static string Dir() => Path.Combine(PathManager.Configs.ToString(), "Thaumiel");

        /// <summary>
        /// Gets the Thaumiel directory plus the inputed directories
        /// </summary>
        /// <param name="path">The path of directories</param>
        /// <returns>Directory path to the Thaumiel directory plus the inputed directories</returns>
        public static string Dir(string[] path) => Path.Combine([Dir(), .. path]);

        /// <summary>
        /// Tries to create a directory with the inputted name
        /// </summary>
        /// <param name="name">The name of the directory. If rooted, it is used directly.</param>
        public static void TryCreateDirectory(string name)
        {
            string path = Path.IsPathRooted(name) ? name : Dir([name]);
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Tries to create a directory at the path
        /// </summary>
        /// <param name="path">The directory path to make</param>
        public static void TryCreateDirectory(string[] path) => Directory.CreateDirectory(Dir(path));

        /// <summary>
        /// Gets all the file paths in the Thaumiel directory combined with the specified directory path.
        /// </summary>
        /// <param name="name">The directory path relative to the Thaumiel directory, or an absolute path.</param>
        /// <param name="filter">The search pattern to filter files by. Defaults to <c>*</c> which returns all files.</param>
        /// <returns>An array of file paths matching the <paramref name="filter"/> in the resolved directory.</returns>
        public static string[] GetFilesInDirectory(string name, string filter = "*")
        {
            string dir = Path.IsPathRooted(name) ? name : Dir([name]);
            if (!Directory.Exists(dir))
                return [];

            return Directory.GetFiles(dir, filter);
        }

        /// <summary>
        /// Reads the specified file at the file path in the background.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onComplete">Fired when the reading is complete with the read text</param>
        public static void ReadFileInBackground(string path, Action<string> onComplete)
        {
            if (!File.Exists(path))
            {
                onComplete?.Invoke(string.Empty);
                return;
            }

            Task.Run(() =>
            {
                string content = string.Empty;

                try
                {
                    content = File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Exception while reading file on background thread: {ex}");
                }

                MainThreadDispatcher.Dispatch(() => 
                {
                    onComplete?.Invoke(content);
                    OnFileReadOnBackgroundThread?.Invoke(path, content);
                });
            });
        }
    }
}