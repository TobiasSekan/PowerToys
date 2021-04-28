﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.PowerToys.Run.Plugin.WindowsSettings.Classes;
using Microsoft.PowerToys.Run.Plugin.WindowsSettings.Properties;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Microsoft.PowerToys.Run.Plugin.WindowsRegistry.Helper
{
    /// <summary>
    /// Helper class to easier work with results
    /// </summary>
    internal static class ResultHelper
    {
        /// <summary>
        /// Return a list with <see cref="Result"/>s, based on the given list.
        /// </summary>
        /// <param name="list">The original result list to convert.</param>
        /// <param name="iconPath">The path to the icon of each entry.</param>
        /// <returns>A list with <see cref="Result"/>.</returns>
        internal static List<Result> GetResultList(
            in IEnumerable<IWindowsSetting> list,
            string query,
            in string iconPath)
        {
            var resultList = new List<Result>(list.Count());

            foreach (var entry in list)
            {
                var type = entry.Command.StartsWith("ms-settings", StringComparison.InvariantCultureIgnoreCase)
                    ? Resources.Settings_app
                    : Resources.Control_panel;

                var result = new Result
                {
                    Action = (_) => DoOpenSettingsAction(entry),
                    IcoPath = iconPath,
                    SubTitle = $"{Resources.Area}: {entry.Area} - {Resources.Type}: {type}",
                    Title = entry.Name,
                    ContextData = entry,
                };

                AddOptionalToolTip(entry, result);

                resultList.Add(result);
            }

            SetScores(resultList, query);

            return resultList;
        }

        /// <summary>
        /// Add a tool-tip to the given <see cref="Result"/>, based o the given <see cref="IWindowsSetting"/>.
        /// </summary>
        /// <param name="entry">The <see cref="IWindowsSetting"/> that contain informations for the tool-tip.</param>
        /// <param name="result">The <see cref="Result"/> that need a tool-tip.</param>
        private static void AddOptionalToolTip(IWindowsSetting entry, Result result)
        {
            var toolTipText = new StringBuilder();

            if (entry.AltNames != null && entry.AltNames.Any())
            {
                var altList = entry.AltNames.Aggregate((current, next) => $"{current}, {next}");

                toolTipText.Append(Resources.Alternative_name);
                toolTipText.Append(": ");
                toolTipText.AppendLine(altList);
            }

            toolTipText.Append(Resources.Command);
            toolTipText.Append(": ");
            toolTipText.Append(entry.Command);

            if (!string.IsNullOrEmpty(entry.Note))
            {
                toolTipText.AppendLine(string.Empty);
                toolTipText.AppendLine(string.Empty);
                toolTipText.Append(Resources.Note);
                toolTipText.Append(": ");
                toolTipText.Append(entry.Note);
            }

            var type = entry.Command.StartsWith("ms-settings", StringComparison.InvariantCultureIgnoreCase)
                ? Resources.Settings_app
                : Resources.Control_panel;

            result.ToolTipData = new ToolTipData(type, toolTipText.ToString());
        }

        /// <summary>
        /// Open the settings page of the given <see cref="IWindowsSetting"/>.
        /// </summary>
        /// <param name="entry">The <see cref="IWindowsSetting"/> that contain the information to open the setting on command level.</param>
        /// <returns><see langword="true"/> if the settings could be opened, otherwise <see langword="false"/>.</returns>
        private static bool DoOpenSettingsAction(IWindowsSetting entry)
        {
            ProcessStartInfo processStartInfo;

            if (entry.Command.Contains(' '))
            {
                var commandSplit = entry.Command.Split(' ');
                var file = commandSplit.FirstOrDefault();
                var arguments = entry.Command[file.Length..].TrimStart();

                processStartInfo = new ProcessStartInfo(file, arguments)
                {
                    UseShellExecute = false,
                };
            }
            else
            {
                processStartInfo = new ProcessStartInfo(entry.Command)
                {
                    UseShellExecute = true,
                };
            }

            try
            {
                Process.Start(processStartInfo);
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception("can't open settings", exception, typeof(ResultHelper));
                return false;
            }
        }

        /// <summary>
        /// Set the score (known as order number or ranking number)
        /// for all <see cref="Results"/> in the given list, based on the given query.
        /// </summary>
        /// <param name="resultList">A list with <see cref="Result"/>s that need scores.</param>
        /// <param name="query">The query to calculated the score for the <see cref="Result"/>s.</param>
        private static void SetScores(IEnumerable<Result> resultList, string query)
        {
            var lowScore = 1_000;
            var highScore = 10_000;

            foreach (var result in resultList)
            {
                var context = (IWindowsSetting)result.ContextData;

                if (context.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    result.Score = highScore--;
                    continue;
                }

                if (context.Area.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    result.Score = highScore--;
                    continue;
                }

                if (context.AltNames is null)
                {
                    result.Score = lowScore--;
                    continue;
                }

                if (context.AltNames.Any(x => x.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)))
                {
                    result.Score = highScore--;
                    continue;
                }

                result.Score = lowScore--;
            }
        }
    }
}
