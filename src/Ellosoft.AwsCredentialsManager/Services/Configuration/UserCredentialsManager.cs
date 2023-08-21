// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Encryption;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration;

public class UserCredentialsManager
{
    public void SaveUserCredentials(string key, UserCredentials userCredentials)
    {
        var encryptedData = DataProtection.Encrypt(JsonSerializer.SerializeToUtf8Bytes(userCredentials));

        File.WriteAllBytes(GetUserCredentialsFilePath(key), encryptedData);
    }

    public void DeleteUserCredentials(string key)
    {
        var userProfileFilePath = GetUserCredentialsFilePath(key);

        if (File.Exists(userProfileFilePath))
            File.Delete(userProfileFilePath);
    }

    public UserCredentials? GetUserCredentials(string key)
    {
        var userProfileFilePath = GetUserCredentialsFilePath(key);

        if (!File.Exists(userProfileFilePath))
            return null;

        var data = File.ReadAllBytes(userProfileFilePath);
        var decryptedData = DataProtection.Decrypt(data);

        return JsonSerializer.Deserialize<UserCredentials>(decryptedData);
    }

    private static string GetUserCredentialsFilePath(string key) => AppDataDirectory.GetPath($"{key}_profile.bin");
}
