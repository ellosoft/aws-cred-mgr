// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services;

public abstract class PlatformService
{
    protected abstract bool IsSupportedPlatform();

    protected virtual bool ThrowIfNotSupportedPlatform { get; }

    protected async Task<T?> ExecuteMultiPlatformCommand<T>(Func<Task<T>> command)
    {
        if (IsSupportedPlatformInternal())
            return await command();

        return default;
    }

    protected async Task ExecuteMultiPlatformCommand(Func<Task> command)
    {
        if (IsSupportedPlatformInternal())
        {
            await command();
        }
    }

    private bool IsSupportedPlatformInternal()
    {
        if (IsSupportedPlatform())
            return true;

        if (ThrowIfNotSupportedPlatform)
            throw new PlatformNotSupportedException("This operation is not supported on this platform.");

        return false;
    }
}
