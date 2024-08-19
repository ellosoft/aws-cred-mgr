// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Security;

public interface IUserCredentialsManager
{
    bool SupportCredentialsStore { get; }

    /// <summary>
    ///     Encrypt and save user credentials to the app data directory
    /// </summary>
    /// <param name="key">Credentials key</param>
    /// <param name="userCredentials">User credentials</param>
    /// <remarks>If the file already exists it will be overwritten</remarks>
    void SaveUserCredentials(string key, UserCredentials userCredentials);

    /// <summary>
    ///     Read encrypted user credentials from app data directory
    /// </summary>
    /// <param name="key">Credentials key</param>
    /// <remarks>This method will return null if no credentials file is found</remarks>
    UserCredentials? GetUserCredentials(string key);
}

public class UserCredentialsManager(ISecureStorage secureStorage) : IUserCredentialsManager
{
    public bool SupportCredentialsStore => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    public void SaveUserCredentials(string key, UserCredentials userCredentials)
    {
        var credentialsJson = JsonSerializer.Serialize(userCredentials, SourceGenerationContext.Default.UserCredentials);
        secureStorage.StoreSecret(key, credentialsJson);
    }

    public UserCredentials? GetUserCredentials(string key)
    {
        return secureStorage.TryRetrieveSecret(key, out var data) ?
            JsonSerializer.Deserialize(data, SourceGenerationContext.Default.UserCredentials) : null;
    }
}
