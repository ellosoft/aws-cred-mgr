namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

// ReSharper disable InconsistentNaming
public static class KeychainConstants
{
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
