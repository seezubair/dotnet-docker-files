// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Docker.Tests
{
    public static class Config
    {
        public static bool IsHttpVerificationDisabled { get; } =
            Environment.GetEnvironmentVariable("DISABLE_HTTP_VERIFICATION") != null;
        public static bool PullImages { get; } = Environment.GetEnvironmentVariable("PULL_IMAGES") != null;
        public static bool IsNightlyRepo { get; } = GetIsNightlyRepo();
        public static bool IsRunningInContainer { get; } =
            Environment.GetEnvironmentVariable("RUNNING_TESTS_IN_CONTAINER") != null;
        public static string RepoPrefix { get; } = Environment.GetEnvironmentVariable("REPO_PREFIX") ?? string.Empty;
        public static string Registry { get; } = Environment.GetEnvironmentVariable("REGISTRY") ?? GetManifestRegistry();
        public static string VersionFilter => Environment.GetEnvironmentVariable("IMAGE_VERSION_FILTER");
        public static string ArchFilter => Environment.GetEnvironmentVariable("IMAGE_ARCH_FILTER");
        public static string OsFilter => Environment.GetEnvironmentVariable("IMAGE_OS_FILTER");

        private static string GetManifestRegistry()
        {
            string manifestJson = File.ReadAllText("manifest.json");
            JObject manifest = JObject.Parse(manifestJson);
            return (string)manifest["registry"];
        }

        private static bool GetIsNightlyRepo()
        {
            string manifestJson = File.ReadAllText("manifest.json");
            JObject manifest = JObject.Parse(manifestJson);
            string repo = (string)manifest["repos"][0]["name"];
            return repo.Contains("-nightly");
        }

        public static string GetFilterRegexPattern(string filter)
        {
            return filter != null ? $"^{Regex.Escape(filter).Replace(@"\*", ".*").Replace(@"\?", ".")}$" : null;
        }
    }
}
