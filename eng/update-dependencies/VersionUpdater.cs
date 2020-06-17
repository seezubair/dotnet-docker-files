// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.VersionTools.Dependencies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dotnet.Docker
{
    /// <summary>
    /// An IDependencyUpdater that will update the specified version variables within the manifest to align with the
    /// current product version.
    /// </summary>
    public class VersionUpdater : FileRegexUpdater
    {
        private readonly static string[] s_excludedMonikers = { "servicing", "rtm" };
        private readonly static string s_versionGroupName = "versionValue";

        private string _productName;
        private VersionType _versionType;

        public VersionUpdater(VersionType versionType, string productName, string dockerfileVersion, string repoRoot) : base()
        {
            _productName = productName;
            _versionType = versionType;
            string versionVariableName = GetVersionVariableName(versionType, productName, dockerfileVersion);

            Trace.TraceInformation($"Updating {versionVariableName}");

            Path = System.IO.Path.Combine(repoRoot, Program.VersionsFilename);
            VersionGroupName = s_versionGroupName;
            Regex = GetVersionVariableRegex(versionVariableName);
        }

        protected override string TryGetDesiredValue(
            IEnumerable<IDependencyInfo> dependencyBuildInfos, out IEnumerable<IDependencyInfo> usedBuildInfos)
        {
            IDependencyInfo productInfo = dependencyBuildInfos.First(info => info.SimpleName == _productName);

            usedBuildInfos = new IDependencyInfo[] { productInfo };

            string version;
            switch (_versionType)
            {
                case VersionType.Build:
                    version = GetBuildVersion(productInfo);
                    break;
                case VersionType.Product:
                    version = GetProductVersion(productInfo);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported VersionType: {_versionType}");
            }

            return version;
        }

        private string GetBuildVersion(IDependencyInfo productInfo) => productInfo.SimpleVersion;

        public static string GetBuildVersion(string productName, string dockerfileVersion, string variables)
        {
            string versionVariableName = GetVersionVariableName(VersionType.Build, productName, dockerfileVersion);
            Regex regex = GetVersionVariableRegex(versionVariableName);
            Match match = regex.Match(variables);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Unable to retrieve {versionVariableName}");
            }

            return match.Groups[s_versionGroupName].Value;
        }

        private string GetProductVersion(IDependencyInfo productInfo)
        {
            // Derive the Docker tag version from the product build version.
            // 5.0.0-preview.2.19530.9 => 5.0.0-preview.2
            string versionRegexPattern = "[\\d]+.[\\d]+.[\\d]+(-[\\w]+(.[\\d]+)?)?";
            Match versionMatch = Regex.Match(productInfo.SimpleVersion, versionRegexPattern);
            string version = versionMatch.Success ? versionMatch.Value : productInfo.SimpleVersion;

            foreach (string excludedMoniker in s_excludedMonikers)
            {
                int monikerIndex = version.IndexOf($"-{excludedMoniker}", StringComparison.OrdinalIgnoreCase);
                if (monikerIndex != -1)
                {
                    version = version.Substring(0, monikerIndex);
                }
            }

            return version;
        }

        private static Regex GetVersionVariableRegex(string versionVariableName) =>
            new Regex($"\"{Regex.Escape(versionVariableName)}\": \"(?<{s_versionGroupName}>[\\d]+.[\\d]+.[\\d]+(-[\\w]+(.[\\d]+)*)?)\"");

        private static string GetVersionVariableName(VersionType versionType, string productName, string dockerfileVersion) =>
            $"{productName}|{dockerfileVersion}|{versionType.ToString().ToLowerInvariant()}-version";
    }
}
