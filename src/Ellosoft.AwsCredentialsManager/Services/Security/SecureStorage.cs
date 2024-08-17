// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Security;

public interface ISecureStorage
{
    void StoreSecret(string key, string data);

    void DeleteSecret(string key);

    bool TryRetrieveSecret(string key, [NotNullWhen(true)] out string? data);
}

public class SecureStorage : ISecureStorage
{
    public virtual void StoreSecret(string key, string data) {}

    public virtual void DeleteSecret(string key) { }

    public virtual bool TryRetrieveSecret(string key, [NotNullWhen(true)] out string? data)
    {
        data = null;

        return false;
    }
}
