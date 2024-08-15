using System.Runtime.InteropServices;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

public interface INativeMacOsKeychain
{
    int SecItemAdd(IntPtr query, IntPtr result);

    int SecItemCopyMatching(IntPtr query, out IntPtr result);

    int SecItemDelete(IntPtr query);
}

#pragma warning disable S4200

public class NativeMacOsKeychain : INativeMacOsKeychain
{
    private NativeMacOsKeychain()
    {
    }

    private const string SECURITY_LIBRARY = "/System/Library/Frameworks/Security.framework/Security";

    public static INativeMacOsKeychain Instance { get; set; } = new NativeMacOsKeychain();

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemAdd")]
    private static extern int SecItemAddNative(IntPtr query, IntPtr result);

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemCopyMatching")]
    private static extern int SecItemCopyMatchingNative(IntPtr query, out IntPtr result);

    [DllImport(SECURITY_LIBRARY, EntryPoint = "SecItemDelete")]
    private static extern int SecItemDeleteNative(IntPtr query);

    public int SecItemAdd(IntPtr query, IntPtr result) => SecItemAddNative(query, result);

    public int SecItemCopyMatching(IntPtr query, out IntPtr result) => SecItemCopyMatchingNative(query, out result);

    public int SecItemDelete(IntPtr query) => SecItemDeleteNative(query);
}
