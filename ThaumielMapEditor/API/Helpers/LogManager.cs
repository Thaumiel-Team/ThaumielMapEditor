// -----------------------------------------------------------------------
// <copyright file="LogManager.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ThaumielMapEditor.API.Helpers
{
    internal class LogManager
    {
        private const int MaxLogs = 10000;
        private static readonly object LogLock = new();

        public class Log
        {
            public LogLevel LogLevel { get; set; }
            public string Message { get; set; } = string.Empty;
            public DateTime LogTime { get; set; }
        }

        public static List<Log> Logs = [];
        public static event Action<Log>? LogCreated;

        private static void AddLog(Log log)
        {
            lock (LogLock)
            {
                Logs.Add(log);
                if (Logs.Count > MaxLogs)
                    Logs.RemoveRange(0, Logs.Count - MaxLogs);
            }

            LogCreated?.Invoke(log);
        }

        /// <summary>
        /// Returns a thread-safe snapshot of all retained log entries.
        /// </summary>
        public static Log[] GetLogSnapshot()
        {
            lock (LogLock)
            {
                return Logs.ToArray();
            }
        }

        public static void Info(string message)
        {
            string formattedMessage = FormatLogMessage(message);
            Logger.Info(formattedMessage);
            Log log = new()
            {
                LogLevel = LogLevel.Info,
                Message = formattedMessage,
                LogTime = DateTime.Now
            };

            AddLog(log);
        }

        public static void Debug(string message)
        {
            if (Main.Instance.Config is not { Debug: true })
                return;

            string formattedMessage = FormatLogMessage(message);
            Logger.Debug(formattedMessage, true);
            Log log = new()
            {
                LogLevel = LogLevel.Debug,
                Message = formattedMessage,
                LogTime = DateTime.Now
            };

            AddLog(log);
        }

        public static void Warn(string message)
        {
            string formattedMessage = FormatLogMessage(message);
            Logger.Warn(formattedMessage);
            Log log = new()
            {
                LogLevel = LogLevel.Warn,
                Message = formattedMessage,
                LogTime = DateTime.Now
            };

            AddLog(log);
        }

        public static void Error(string message)
        {
            string formattedMessage = FormatLogMessage(message);
            string msg = formattedMessage += "\n An error has occured! Please run the command 'tmelogs' and share the issued code in our discord";
            Logger.Error(msg);
            Log log = new()
            {
                LogLevel = LogLevel.Error,
                Message = formattedMessage,
                LogTime = DateTime.Now
            };

            AddLog(log);
        }

        public static void Updater(string message)
        {
            Logger.Raw($"[Updater] [{Main.Instance.GetType().Assembly.GetName().Name}] {message}", ConsoleColor.Blue);
            Log log = new()
            {
                LogLevel = LogLevel.Info,
                Message = message,
                LogTime = DateTime.Now
            };
            
            AddLog(log);
        }
        
        public static void LogShare(string message)
        {
            Logger.Raw($"[LogShare] [{Main.Instance.GetType().Assembly.GetName().Name}] {message}", ConsoleColor.DarkMagenta);
            Log log = new()
            {
                LogLevel = LogLevel.Info,
                Message = message,
                LogTime = DateTime.Now
            };
            
            AddLog(log);
        }

        internal static string FormatLogMessage(string message)
        {
            StackTrace stackTrace = new(true);
            StackFrame? frame = stackTrace.GetFrame(2);
            if (frame != null)
            {
                MethodBase method = frame.GetMethod();
                if (method?.DeclaringType != null)
                {
                    string className;
                    if (method.IsStatic)
                    {
                        className = method.DeclaringType.FullName + $".{method.Name}()" ?? method.DeclaringType.Name + $".{method.Name}()";
                    }
                    else
                        className = method.DeclaringType.FullName + $"::{method.Name}()" ?? method.DeclaringType.Name + $"::{method.Name}()";

                    message = Regex.Replace(message, @"_Patch\d+", "");
                    return $"[{className}] {message}";
                }
            }

            message = Regex.Replace(message, @"_Patch\d+", "");
            return $"[Unknown] {message}";
        }
    }
}