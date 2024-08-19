// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Text;
using Ellosoft.AwsCredentialsManager.Services.IO;
using Ellosoft.AwsCredentialsManager.Services.Platforms.Windows.Security;

namespace Ellosoft.AwsCredentialsManager.Services.Security;

[SupportedOSPlatform("windows")]
public class SecureStorageWindows(
    IProtectedDataService protectedDataService,
    IFileManager fileManager) : SecureStorage
{
    private static readonly byte[] Scope = Encoding.UTF8.GetBytes(AppMetadata.AppName);

    public override void StoreSecret(string key, string data)
    {
        var protectedData = protectedDataService.Protect(Encoding.UTF8.GetBytes(data), Scope);
        var credentialPath = GetCredentialPath(key);

        fileManager.SaveFile(credentialPath, protectedData);
    }

    public override void DeleteSecret(string key) => fileManager.DeleteFile(GetCredentialPath(key));

    public override bool TryRetrieveSecret(string key, [NotNullWhen(true)] out string? data)
    {
        data = null;
        var credentialPath = GetCredentialPath(key);

        if (!fileManager.FileExists(credentialPath))
            return false;

        var protectedData = fileManager.ReadFile(credentialPath);
        var unprotectedData = protectedDataService.Unprotect(protectedData, Scope);
        data = Encoding.UTF8.GetString(unprotectedData);

        return !string.IsNullOrEmpty(data);
    }

    private static string GetCredentialPath(string key) => AppDataDirectory.GetPath($"credentials/{key}.bin");
}
