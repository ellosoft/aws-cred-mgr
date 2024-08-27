using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

public interface IKeychainService
{
    SecurityItemResult AddGenericPassword(string account, string service, string password);

    string? GetGenericPassword(string account, string service);

    SecurityItemResult DeleteItem(string account, string service);
}

[SupportedOSPlatform("macos")]
public class KeychainService(IMacOsKeychainInterop keychainInterop) : IKeychainService
{
    public SecurityItemResult AddGenericPassword(string account, string service, string password)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainConstants.QueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainConstants.QueryKeys.kSecAttrAccount, account },
            { KeychainConstants.QueryKeys.kSecAttrService, service },
            { KeychainConstants.QueryKeys.kSecValueData, password },
        };

        using var nsQuery = NSMutableDictionary.Create(query);

        return keychainInterop.SecItemAdd(nsQuery.Handle, IntPtr.Zero);
    }

    public string? GetGenericPassword(string account, string service)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainConstants.QueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainConstants.QueryKeys.kSecAttrAccount, account },
            { KeychainConstants.QueryKeys.kSecAttrService, service },
            { KeychainConstants.QueryKeys.kSecReturnData, true }
        };

        using var nsQuery = NSMutableDictionary.Create(query);
        var result = keychainInterop.SecItemCopyMatching(nsQuery.Handle, out var resultPtr);

        if (result == 0 && resultPtr != IntPtr.Zero)
        {
            using var nsData = new NSData(resultPtr);

            return nsData.ToString();
        }

        return null;
    }

    public SecurityItemResult DeleteItem(string account, string service)
    {
        var query = new Dictionary<string, object>
        {
            { KeychainConstants.QueryKeys.kSecClass, KeychainConstants.kSecClassGenericPassword },
            { KeychainConstants.QueryKeys.kSecAttrAccount, account },
            { KeychainConstants.QueryKeys.kSecAttrService, service },
        };

        using var nsQuery = NSMutableDictionary.Create(query);

        return keychainInterop.SecItemDelete(nsQuery.Handle);
    }
}
