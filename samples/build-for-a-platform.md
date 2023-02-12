# Building images for a specific platform

Docker exposes [multiple ways to interact with platforms](https://docs.docker.com/build/building/multi-platform/). Sometimes this will result in the images you want and sometimes not depending on how structure your Dockerfiles and your use of the `docker` cli. The most common scenario for needing to pay attention to platform targeting is if you have an Arm64 development machine (like Apple M1/M*) and are pushing images to an x64 cloud. This equally applies to `docker run` and `docker build`.

In Docker terminology, `platform` refers to operating system + architecture. For example the combination of Linux and x64 is a platform, described by the `linux/amd64` platform string. The `linux` part is relevant, however, the most common use for platform targeting is controlling the architecture, choosing (primarily) between `amd64` and `arm64`.

There are three patterns discussed. They are equally "correct". If you are using an Apple M1 device, you probably want to consider the last pattern, with the `--platform` argument.

Notes:

- `amd64` is used for historical reasons and is synonymous with "x64", however, `x64` is not an accepted alias.
- .NET tags are described in [.NET Container Tags -- Patterns and Policies](../documentation//supported-tags.md).
- This document applies to building Linux containers, not Windows containers (which are x64 only). Some aspects apply to Windows containers, but they are not specifically addressed.

## Dockerfiles that build everywhere

The default scenario is using multi-arch tags, which will work in multiple environments.

For example, using `FROM` statements like the following:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
```

The [Dockerfile](aspnetapp/Dockerfile) example demonstrates this case, using the following `docker build` invocation.

```bash
docker build -t app .
```

It can be built on any supported operating system and architecture. For example, if built on an Apple M1 machine, Docker will produce a `linux/arm64` image, while on a Windows x64 machine, it will produce a `linux/amd64` image.

This model works very well given a homogenous compute environment. For example, it works well if dev, CI, and prod machines are all x64. However, it doesn't as well in heterogenous environments, like if dev machines are Arm64 and prod machines are x64. This is because Docker defaults to the native architecture (which is a good policy), but that means that resulting images might not match.

## Lock Dockerfiles to one platform

Another approach is to always build for one platform with Dockerfiles that reference tags for that platform. This model has the benefit that Dockerfiles are simple and always produce the same results. It has the downside that it can result in a Dockerfile per platform (in a heterogenous compute environment), requiring users to know which to build.

The following are examples of this model:

- [Dockerfile.debian-x64](aspnetapp/Dockerfile.debian-x64)
- [Dockerfile.alpine-arm64](aspnetapp/Dockerfile.alpine-arm64)

They can be built with the following pattern:

```bash
docker build -t app -f Dockerfile.debian-x64 .
```

This pattern results in, for example, x64 images always being used. Those images will work on any platform but will require the use of emulation if an x64 image is used on Arm64 and vice versa. .NET doesn't support QEMU emulation, as covered later. As a result, this pattern is only appropriate for homogenous environments (all x64 or all Arm64).

## Conditionalize Dockerfile with `--platform`

The `--platform` argument is the best way to specify the desired architecture. The `--platform` argument doesn't switch Docker to a special mode, but specifies the platform to request for multi-arch tags. Single-architecture tags are unaffected by this argument. That's a good behavior, enabling user to lock some tags to a platform, if desired, and to enable other tags to be affected by the platform switch.

In addition, Docker [Buildkit exposes multiple environment variables](https://github.com/dotnet/dotnet-docker/pull/4387#issuecomment-1416565213) that can be used to further conditionalize behavior. These environment variables can be controlled with the pattern demonstrated in [Dockerfile](https://github.com/mthalman/dredge/blob/main/src/Valleysoft.Dredge/Dockerfile). As mentioned, .NET doesn't support being run in emulation. The pattern in that Dockerfile results in the SDK always being run natively while the final image is affected by the `--platform` switch. This model also has the best performance since the bulk of computation is run natively.

Note: We are enabling the following model in a future release: https://github.com/dotnet/dotnet-docker/issues/4388#issuecomment-1421401384.

## .NET and QEMU

Docker desktop uses [QEMU](https://www.qemu.org/) for emulation, for example running x64 code on an Arm64 machine. .NET doesn't support being run in QEMU. That means that the SDK needs to always be run natively, to enable [multi-stage build](https://docs.docker.com/build/building/multi-stage/). Multi-stage build is used by all of our samples.

As a result, we need a reliable pattern that enables produces multiple variants of images on one machine, but that doesn't use emulation.

Context: https://gitlab.com/qemu-project/qemu/-/issues/249 