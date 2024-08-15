// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;
using Ellosoft.AwsCredentialsManager.Services.Platforms.Windows;

namespace Ellosoft.AwsCredentialsManager.Services.Encryption;

public class SecureStorage
{
    public void Store(string key, byte[] data)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Saving passwords is only supported on Windows at the moment");

        return WindowsDataProtection.Protect(data);
    }

    public byte[] Retrieve(string key)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Saving passwords is only supported on Windows at the moment");

        return WindowsDataProtection.Unprotect(data);
    }
}
