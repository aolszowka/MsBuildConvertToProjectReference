// -----------------------------------------------------------------------
// <copyright file="MSBCTPROptions.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildConvertToProjectReference
{
    using System.Collections.Generic;

    public class MSBCTPROptions
    {
        /// <summary>
        /// Gets or sets the directory to operate on
        /// </summary>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// Gets or sets the lookup directories
        /// </summary>
        public IEnumerable<string> LookupDirectories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run this tool in validation mode only.
        /// </summary>
        public bool Validate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to show help.
        /// </summary>
        public bool ShowHelp { get; set; }
    }
}
