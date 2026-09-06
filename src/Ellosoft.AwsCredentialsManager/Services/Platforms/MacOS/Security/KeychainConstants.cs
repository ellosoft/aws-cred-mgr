namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

// ReSharper disable InconsistentNaming
public static class KeychainConstants
{
    // Apple Security framework constant (kSecClassGenericPassword => "genp"), not a credential
    [SuppressMessage("Security", "S2068:Hard-coded credentials are security-sensitive", Justification = "Apple Security framework item class identifier")]
    public const string kSecClassGenericPassword = "genp";

    public static class QueryKeys
    {
        public const string kSecClass = "class";
        public const string kSecAttrAccount = "acct";
        public const string kSecAttrService = "svce";
        public const string kSecValueData = "v_Data";

        public const string kSecReturnData = "r_Data";
    }
}
