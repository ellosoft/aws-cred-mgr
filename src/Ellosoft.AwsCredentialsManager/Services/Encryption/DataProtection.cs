// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;

namespace Ellosoft.AwsCredentialsManager.Services.Encryption;

public static class DataProtection
{
    public static byte[] Encrypt(byte[] data)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Saving passwords is only supported on Windows at the moment");

        return WindowsDataProtection.Protect(data);
    }

    public static byte[] Decrypt(byte[] data)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Saving passwords is only supported on Windows at the moment");

        return WindowsDataProtection.Unprotect(data);
    }
}
