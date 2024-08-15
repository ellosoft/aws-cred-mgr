// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;
using System.Text.Json;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Encryption;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration;

public class UserCredentialsManager
{
    public bool SupportCredentialsStore { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    ///     Encrypt and save user credentials to the app data directory
    /// </summary>
    /// <param name="key">Credentials key</param>
    /// <param name="userCredentials">User credentials</param>
    /// <remarks>If the file already exists it will be overwritten</remarks>
    public void SaveUserCredentials(string key, UserCredentials userCredentials)
    {
        var encryptedData = SecureStorage.Store(JsonSerializer.SerializeToUtf8Bytes(userCredentials, SourceGenerationContext.Default.UserCredentials));

        File.WriteAllBytes(GetUserCredentialsFilePath(key), encryptedData);
    }

    /// <summary>
    ///     Delete user credentials
    /// </summary>
    /// <param name="key">Credentials key</param>
    public void DeleteUserCredentials(string key)
    {
        var userProfileFilePath = GetUserCredentialsFilePath(key);

        if (File.Exists(userProfileFilePath))
            File.Delete(userProfileFilePath);
    }

    /// <summary>
    ///     Read encrypted user credentials from app data directory
    /// </summary>
    /// <param name="key">Credentials key</param>
    /// <remarks>This method will return null if no credentials file is found</remarks>
    public UserCredentials? GetUserCredentials(string key)
    {
        var userProfileFilePath = GetUserCredentialsFilePath(key);

        if (!File.Exists(userProfileFilePath))
            return null;

        var data = File.ReadAllBytes(userProfileFilePath);
        var decryptedData = SecureStorage.Retrieve(data);

        return JsonSerializer.Deserialize(decryptedData, SourceGenerationContext.Default.UserCredentials);
    }

    private static string GetUserCredentialsFilePath(string key) => AppDataDirectory.GetPath($"{key}_profile.bin");
}
