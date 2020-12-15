﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Plugin.Registry.Classes;
using Microsoft.Plugin.Registry.Constants;
using Microsoft.Plugin.Registry.Properties;
using Microsoft.Win32;

namespace Microsoft.Plugin.Registry.Helper
{
#pragma warning disable CA1031 // Do not catch general exception types

    /// <summary>
    /// Helper class to easier work with the registry
    /// </summary>
    internal static class RegistryHelper
    {
        /// <summary>
        /// A list that contain all registry base keys in a long/full version and in a short version (e.g HKLM = HKEY_LOCAL_MACHINE)
        /// </summary>
        private static readonly IReadOnlyDictionary<string, RegistryKey?> _baseKeys = new Dictionary<string, RegistryKey?>(14)
        {
            { KeyName.FirstPart, null },
            { KeyName.FirstPartUnderscore, null },
            { KeyName.ClassRootShort, Win32.Registry.ClassesRoot },
            { Win32.Registry.ClassesRoot.Name, Win32.Registry.ClassesRoot },
            { KeyName.CurrentConfigShort, Win32.Registry.CurrentConfig },
            { Win32.Registry.CurrentConfig.Name, Win32.Registry.CurrentConfig },
            { KeyName.CurrentUserShort, Win32.Registry.CurrentUser },
            { Win32.Registry.CurrentUser.Name, Win32.Registry.CurrentUser },
            { KeyName.LocalMachineShort, Win32.Registry.LocalMachine },
            { Win32.Registry.LocalMachine.Name, Win32.Registry.LocalMachine },
            { KeyName.PerformanceDataShort, Win32.Registry.PerformanceData },
            { Win32.Registry.PerformanceData.Name, Win32.Registry.PerformanceData },
            { KeyName.UsersShort, Win32.Registry.Users },
            { Win32.Registry.Users.Name, Win32.Registry.Users },
        };

        /// <summary>
        /// Try to find a registry base key based on the given query
        /// </summary>
        /// <param name="query">The query to search</param>
        /// <returns>A combination of the base <see cref="RegistryKey"/> and the sub keys</returns>
        internal static (RegistryKey? baseKey, string subKey) GetRegistryBaseKey(in string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (null, string.Empty);
            }

            var baseKey = query.Split('\\').FirstOrDefault();
            var subKey = query.TrimStart('\\').Replace(baseKey, string.Empty, StringComparison.InvariantCultureIgnoreCase);
            var baseKeyResult = _baseKeys.FirstOrDefault(found => found.Key.StartsWith(baseKey, StringComparison.InvariantCultureIgnoreCase));

            return (baseKeyResult.Value, subKey);
        }

        /// <summary>
        /// Return a list of all registry base key
        /// </summary>
        /// <returns>A list with all registry base keys</returns>
        internal static ICollection<RegistryEntry> GetAllBaseKeys()
            => new Collection<RegistryEntry>
            {
                new RegistryEntry(Win32.Registry.ClassesRoot),
                new RegistryEntry(Win32.Registry.CurrentConfig),
                new RegistryEntry(Win32.Registry.CurrentUser),
                new RegistryEntry(Win32.Registry.LocalMachine),
                new RegistryEntry(Win32.Registry.PerformanceData),
                new RegistryEntry(Win32.Registry.Users),
            };

        /// <summary>
        /// Search for the given sub-key path in the given registry base key
        /// </summary>
        /// <param name="baseKey">The base <see cref="RegistryKey"/></param>
        /// <param name="subKeyPath">The path of the registry sub-key</param>
        /// <returns>A list with all found registry keys</returns>
        internal static ICollection<RegistryEntry> SearchForSubKey(in RegistryKey baseKey, in string subKeyPath)
        {
            var subKeysNames = subKeyPath.Split('\\');
            var index = 0;
            var subKey = baseKey;

            ICollection<RegistryEntry> result;

            do
            {
                result = FindSubKey(subKey, subKeysNames.ElementAtOrDefault(index));

                if (result.Count == 0)
                {
                    return GetAllSubKeys(baseKey);
                }

                if (result.Count == 1 && index < subKeysNames.Length)
                {
                    subKey = result.First().Key;
                }

                if (subKey == null)
                {
                    break;
                }

                index++;
            }
            while (index < subKeysNames.Length);

            return result;
        }

        /// <summary>
        /// Return a human readable summary of a given <see cref="RegistryKey"/>
        /// </summary>
        /// <param name="key">The <see cref="RegistryKey"/> for the summary</param>
        /// <returns>A human readable summary</returns>
        internal static string GetSummary(in RegistryKey key)
            => $"{Resources.SubKeys} {key.SubKeyCount} - {Resources.Values} {key.ValueCount}";

        /// <summary>
        /// Open a given registry key in the registry editor
        /// </summary>
        /// <param name="fullKey">The registry key to open</param>
        internal static void OpenRegistryKey(in string fullKey)
        {
            // it's impossible to directly open a key via command-line option, so we must override the last remember key
            Win32.Registry.SetValue(@"HKEY_Current_User\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", "LastKey", fullKey);

            Process.Start(new ProcessStartInfo
            {
                Arguments = "-m",           // -m => allow multi-instance (hidden start option)
                FileName = "regedit.exe",
                Verb = "runas",             // Start as administrator
                UseShellExecute = true,     // Start as administrator will not work without this
            });
        }

        /// <summary>
        /// Try to find the given registry sub-key in the given registry parent-key
        /// </summary>
        /// <param name="parentKey">The parent-key, also the root to start the search</param>
        /// <param name="searchSubKey">The sub-key to find</param>
        /// <returns>A list with all found registry sub-keys</returns>
        private static ICollection<RegistryEntry> FindSubKey(in RegistryKey parentKey, in string searchSubKey)
        {
            var list = new Collection<RegistryEntry>();

            try
            {
                foreach (var subKey in parentKey.GetSubKeyNames())
                {
                    if (!subKey.StartsWith(searchSubKey, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        list.Add(new RegistryEntry(parentKey.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadSubTree)));
                    }
                    catch (Exception exception)
                    {
                        list.Add(new RegistryEntry($"{parentKey.Name}\\{subKey}", exception));
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(new RegistryEntry(parentKey.Name, ex));
            }

            return list;
        }

        /// <summary>
        /// Return a list with a registry sub-keys of the given registry parent-key
        /// </summary>
        /// <param name="parentKey">The registry parent-key</param>
        /// <param name="maxCount">(optional) The maximum count of the results</param>
        /// <returns>A list with all found registry sub-keys</returns>
        private static ICollection<RegistryEntry> GetAllSubKeys(in RegistryKey parentKey, in int maxCount = 50)
        {
            var list = new Collection<RegistryEntry>();

            try
            {
                foreach (var subKey in parentKey.GetSubKeyNames())
                {
                    if (list.Count >= maxCount)
                    {
                        break;
                    }

                    list.Add(new RegistryEntry(parentKey));
                }
            }
            catch (Exception exception)
            {
                list.Add(new RegistryEntry(parentKey.Name, exception));
            }

            return list;
        }
    }

    #pragma warning restore CA1031 // Do not catch general exception types
}
