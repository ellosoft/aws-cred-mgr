using System.Runtime.InteropServices;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

public interface IMacOsKeychainInterop
{
    SecurityItemResult SecItemAdd(IntPtr query, IntPtr result);

    SecurityItemResult SecItemCopyMatching(IntPtr query, out IntPtr result);

    SecurityItemResult SecItemDelete(IntPtr query);
}

#pragma warning disable S4200

[SupportedOSPlatform("macos")]
[ExcludeFromCodeCoverage]
public class MacOsKeychainInterop : IMacOsKeychainInterop
{
    private const string SECURITY_LIBRARY = "/System/Library/Frameworks/Security.framework/Security";

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemAdd")]
    private static extern SecurityItemResult SecItemAddNative(IntPtr query, IntPtr result);

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemCopyMatching")]
    private static extern SecurityItemResult SecItemCopyMatchingNative(IntPtr query, out IntPtr result);

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemDelete")]
    private static extern SecurityItemResult SecItemDeleteNative(IntPtr query);

    public SecurityItemResult SecItemAdd(IntPtr query, IntPtr result) => SecItemAddNative(query, result);

    public SecurityItemResult SecItemCopyMatching(IntPtr query, out IntPtr result) => SecItemCopyMatchingNative(query, out result);

    public SecurityItemResult SecItemDelete(IntPtr query) => SecItemDeleteNative(query);
}
