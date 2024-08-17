// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

namespace Ellosoft.AwsCredentialsManager.Services.Security;

[SupportedOSPlatform("macos")]
public class SecureStorageMacOS(IKeychainService keychainService) : SecureStorage
{
    public override void StoreSecret(string key, string data)
    {
        var result = keychainService.AddGenericPassword(key, AppMetadata.AppName, data);

       if (result == SecurityItemResult.Success)
           return;

       if (result == SecurityItemResult.DuplicateItem)
       {
           keychainService.DeleteItem(key, AppMetadata.AppName);
           keychainService.AddGenericPassword(key, AppMetadata.AppName, data);
       }
    }

    public override void DeleteSecret(string key) => keychainService.DeleteItem(key, AppMetadata.AppName);

    public override bool TryRetrieveSecret(string key, [NotNullWhen(true)] out string? data)
    {
        data = keychainService.GetGenericPassword(key, AppMetadata.AppName);

        return !string.IsNullOrEmpty(data);
    }
}
