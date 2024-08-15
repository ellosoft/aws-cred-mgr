using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

public interface IKeychain
{
    bool AddGenericPassword(string account, string service, string password);

    string? GetGenericPassword(string account, string service);

    bool DeleteItem(string account, string service);
}

public class Keychain : IKeychain
{
    public bool AddGenericPassword(string account, string service, string password)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainQueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainQueryKeys.kSecAttrAccount, account },
            { KeychainQueryKeys.kSecAttrService, service },
            { KeychainQueryKeys.kSecValueData, password },
        };

        using var nsQuery = NSMutableDictionary.Create(query);
        var result = NativeMacOsKeychain.Instance.SecItemAdd(nsQuery.Handle, IntPtr.Zero);

        return result == 0;
    }

    public string? GetGenericPassword(string account, string service)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainQueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainQueryKeys.kSecAttrAccount, account },
            { KeychainQueryKeys.kSecAttrService, service },
            { KeychainQueryKeys.kSecReturnData, true }
        };

        using var nsQuery = NSMutableDictionary.Create(query);
        var result = NativeMacOsKeychain.Instance.SecItemCopyMatching(nsQuery.Handle, out var resultPtr);

        if (result == 0 && resultPtr != IntPtr.Zero)
        {
            using var nsData = new NSData(resultPtr);

            return nsData.ToString();
        }

        return null;
    }

    public bool DeleteItem(string account, string service)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainQueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainQueryKeys.kSecAttrAccount, account },
            { KeychainQueryKeys.kSecAttrService, service },
        };

        using var nsQuery = NSMutableDictionary.Create(query);
        var status = NativeMacOsKeychain.Instance.SecItemDelete(nsQuery.Handle);

        return status == 0;
    }
}
