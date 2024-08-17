// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Security.Cryptography;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.Windows.Security;

public interface IProtectedDataService
{
    byte[] Protect(byte[] data, byte[] scope);

    byte[] Unprotect(byte[] data, byte[] scope);
}

[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage]
public class ProtectedDataService : IProtectedDataService
{
    public byte[] Protect(byte[] data, byte[] scope) => ProtectedData.Protect(data, scope, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(byte[] data, byte[] scope) => ProtectedData.Unprotect(data, scope, DataProtectionScope.CurrentUser);
}
