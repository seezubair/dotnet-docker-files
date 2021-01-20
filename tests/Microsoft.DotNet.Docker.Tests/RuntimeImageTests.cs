﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Docker.Tests
{
    [Trait("Category", "runtime")]
    public class RuntimeImageTests : CommonRuntimeImageTests
    {
        public RuntimeImageTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        protected override DotNetImageType ImageType => DotNetImageType.Runtime;

        [Theory]
        [MemberData(nameof(GetImageData))]
        public async Task VerifyAppScenario(ProductImageData imageData)
        {
            // Skip test for 6.0 due to https://github.com/dotnet/sdk/issues/14624
            if (imageData.Version.Major == 6)
            {
                return;
            }

            // Skip test until end-to-end scenario works for self-contained publishing on Alpine arm32
            if ((imageData.Version.Major == 5 || imageData.Version.Major == 6) &&
                imageData.Arch == Arch.Arm && imageData.OS.Contains("alpine"))
            {
                return;
            }

            ImageScenarioVerifier verifier = new ImageScenarioVerifier(imageData, DockerHelper, OutputHelper);
            await verifier.Execute();
        }

        [Theory]
        [MemberData(nameof(GetImageData))]
        public void VerifyEnvironmentVariables(ProductImageData imageData)
        {
            List<EnvironmentVariableInfo> variables = new List<EnvironmentVariableInfo>();

            if (imageData.Version.Major >= 5 || (imageData.Version.Major == 2 && DockerHelper.IsLinuxContainerModeEnabled))
            {
                variables.Add(GetRuntimeVersionVariableInfo(imageData, DockerHelper));
            }

            base.VerifyCommonEnvironmentVariables(imageData, variables);
        }

        public static EnvironmentVariableInfo GetRuntimeVersionVariableInfo(ProductImageData imageData, DockerHelper dockerHelper)
        {
            string version = imageData.GetProductVersion(DotNetImageType.Runtime, dockerHelper);
            return new EnvironmentVariableInfo("DOTNET_VERSION", version);
        }
    }
}
