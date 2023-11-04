// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Ellosoft.AwsCredentialsManager.Services.Encryption;

[SupportedOSPlatform("windows")]
public static class WindowsDataProtection
{
    public static byte[] Protect(byte[] data) => ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);

    public static byte[] Unprotect(byte[] data) => ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
}
