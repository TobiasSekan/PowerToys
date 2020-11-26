﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using Microsoft.Plugin.Registry.Classes;
using Microsoft.Win32;
using Wox.Plugin;

namespace Microsoft.Plugin.Registry.Helper
{
    /// <summary>
    /// Helper class to easier work with results
    /// </summary>
    internal static class ResultHelper
    {
        #pragma warning disable CA1031 // Do not catch general exception types

        /// <summary>
        /// Return a list with <see cref="Result"/>s, based on the given list
        /// </summary>
        /// <param name="list">The original result list to convert</param>
        /// <param name="iconPath">The path to the icon of each entry</param>
        /// <param name="maxLength">(optional) The maximum length of result text</param>
        /// <returns>A list with <see cref="Result"/></returns>
        internal static List<Result> GetResultList(in ICollection<RegistryEntry> list, in string iconPath, in int maxLength = 45)
        {
            var resultList = new List<Result>();

            foreach (var entry in list)
            {
                var result = new Result
                {
                    IcoPath = iconPath,
                };

                if (entry.Exception is null && !(entry.Key is null))
                {
                    // when key contains keys or fields
                    result.QueryTextDisplay = entry.Key.Name;
                    result.SubTitle = RegistryHelper.GetSummary(entry.Key);
                    result.Title = GetTruncatedText(entry.Key.Name, maxLength);
                }
                else if (entry.Key is null && !(entry.Exception is null))
                {
                    // on error (e.g access denied)
                    result.QueryTextDisplay = entry.KeyPath;
                    result.SubTitle = entry.Exception.Message;
                    result.Title = GetTruncatedText(entry.KeyPath, maxLength);
                }
                else
                {
                    result.QueryTextDisplay = entry.KeyPath;
                    result.Title = GetTruncatedText(entry.KeyPath, maxLength);
                }

                result.ContextData = entry;
                result.ToolTipData = new ToolTipData("Registry key", $"Key:\t{result.Title}");

                resultList.Add(result);
            }

            return resultList;
        }

        /// <summary>
        /// Return a list with <see cref="Result"/>s, based on the given <see cref="RegistryKey"/>
        /// </summary>
        /// <param name="key">The <see cref="RegistryKey"/> that should contain entries for the list</param>
        /// <param name="iconPath">The path to the icon of each entry</param>
        /// <param name="searchValue">(optional) When not <see cref="string.Empty"/> filter the list for the given value name and value</param>
        /// <param name="maxLength">(optional) The maximum length of result text</param>
        /// <returns>A list with <see cref="Result"/></returns>
        internal static List<Result> GetValuesFromKey(in RegistryKey? key, in string iconPath, string searchValue = "", in int maxLength = 45)
        {
            if (key is null)
            {
                return new List<Result>(0);
            }

            ICollection<KeyValuePair<string, object>> valueList = new List<KeyValuePair<string, object>>(key.ValueCount);

            var resultList = new List<Result>();

            try
            {
                var valueNames = key.GetValueNames();

                try
                {
                    foreach (var valueName in valueNames)
                    {
                        valueList.Add(KeyValuePair.Create(valueName, key.GetValue(valueName)));
                    }
                }
                catch (Exception valueException)
                {
                    resultList.Add(new Result
                    {
                        ContextData = new RegistryEntry(key.Name, valueException),
                        IcoPath = iconPath,
                        SubTitle = valueException.Message,
                        Title = GetTruncatedText(key.Name, maxLength),
                        ToolTipData = new ToolTipData(valueException.Message, valueException.ToString()),
                        QueryTextDisplay = key.Name,
                    });
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    var filteredValueName = valueList.Where(found => found.Key.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase));
                    var filteredValueList = valueList.Where(found => found.Value.ToString()?.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase) ?? false);

                    valueList = filteredValueName.Concat(filteredValueList).ToList();
                }

                foreach (var valueEntry in valueList)
                {
                    resultList.Add(new Result
                    {
                        ContextData = new RegistryEntry(key),
                        IcoPath = iconPath,
                        SubTitle = $"Type: {ValueHelper.GetType(key, valueEntry.Key)} * Value: {ValueHelper.GetValue(key, valueEntry.Key, 50)}",
                        Title = GetTruncatedText(valueEntry.Key, maxLength),
                        ToolTipData = new ToolTipData("Registry value", $"Key:\t{key.Name}\nName:\t{valueEntry.Key}\nType:\t{ValueHelper.GetType(key, valueEntry.Key)}\nValue:\t{ValueHelper.GetValue(key, valueEntry.Key)}"),
                        QueryTextDisplay = key.Name,
                    });
                }
            }
            catch (Exception exception)
            {
                resultList.Add(new Result
                {
                    ContextData = new RegistryEntry(key.Name, exception),
                    IcoPath = iconPath,
                    SubTitle = exception.Message,
                    Title = GetTruncatedText(key.Name, maxLength),
                    ToolTipData = new ToolTipData(exception.Message, exception.ToString()),
                    QueryTextDisplay = key.Name,
                });
            }

            return resultList;
        }

#pragma warning restore CA1031 // Do not catch general exception types

        /// <summary>
        /// Return a truncated name (right based with three left dots)
        /// </summary>
        /// <param name="text">The text to truncate</param>
        /// <param name="maxLength">(optional) The maximum length of the text</param>
        /// <returns>A truncated text with a maximum length</returns>
        internal static string GetTruncatedText(string text, in int maxLength = 45)
        {
            if (text.Length > maxLength)
            {
                text = QueryHelper.GetKeyWithShortBaseKey(text);
            }

            return text.Length > maxLength ? "..." + text[^maxLength..] : text;
        }
    }
}
