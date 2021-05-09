﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.PowerToys.Run.Plugin.WindowsSettings.Classes
{
    /// <summary>
    /// A windows setting
    /// </summary>
    internal class WindowsSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsSetting"/> class.
        /// </summary>
        public WindowsSetting()
        {
            Name = string.Empty;
            Area = string.Empty;
            Command = string.Empty;
            Type = string.Empty;
        }

        /// <summary>
        /// Gets or sets the name of this setting.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the area of this setting.
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// Gets or sets the command of this setting.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the type of the windows setting.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the alternative names of this setting.
        /// </summary>
        public IEnumerable<string>? AltNames { get; set; }

        /// <summary>
        /// Gets or sets a additional note of this settings.
        /// <para>(e.g. why is not supported on your system)</para>
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the minimum need Windows version for this setting.
        /// </summary>
        public ushort? IntroducedInVersion { get; set; }

        /// <summary>
        /// Gets or sets the Windows version since this settings is not longer present.
        /// </summary>
        public ushort? DeprecatedInVersion { get; set; }
    }
}
