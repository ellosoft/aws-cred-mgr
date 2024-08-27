// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services;

public abstract class PlatformServiceSlim
{
    protected static T ExecuteMultiPlatformCommand<T>(
        Func<T>? win = null,
        Func<T>? macos = null,
        Func<T>? linux = null,
        Func<T>? nonSupported = null)
    {
        if (OperatingSystem.IsWindows() && win is not null)
            return win();

        if (OperatingSystem.IsMacOS() && macos is not null)
            return macos();

        if (OperatingSystem.IsLinux() && linux is not null)
            return linux();

        return HandleNonSupportedPlatformException(nonSupported);
    }

    private static T HandleNonSupportedPlatformException<T>(Func<T>? nonSupported)
    {
        if (nonSupported is not null)
            return nonSupported();

        throw new PlatformNotSupportedException("This operation is not supported on this platform.");
    }
}
