﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.DotNet.Docker.Tests
{
    public class SampleImageData : ImageData
    {
        public string GetImage(SampleImageType imageType, DockerHelper dockerHelper)
        {
            string tagPrefix;
            switch (imageType)
            {
                case SampleImageType.Console:
                    tagPrefix = "dotnetapp";
                    break;
                case SampleImageType.Aspnet:
                    tagPrefix = "aspnetapp";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported image type '{imageType}'");
            }

            string tag = GetTagName(tagPrefix, OS);
            string imageName = GetImageName(tag, "samples");

            PullImageIfNecessary(imageName, dockerHelper);

            return imageName;
        }
    }
}
