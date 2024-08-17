using System.Runtime.InteropServices;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS;

public interface IObjectiveCRuntimeInterop
{
    IntPtr SendMessage(IntPtr receiver, IntPtr selector);

    IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1);

    IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    IntPtr SendMessage(IntPtr receiver, IntPtr selector, string arg1);

    IntPtr SendMessage(IntPtr receiver, IntPtr selector, bool arg1);

    IntPtr GetClass(string name);

    IntPtr RegisterSelector(string name);
}

#pragma warning disable S4200

[SupportedOSPlatform("macos")]
[ExcludeFromCodeCoverage]
public class ObjectiveCRuntimeInterop : IObjectiveCRuntimeInterop
{
    private ObjectiveCRuntimeInterop() { }

    private const string LIBRARY_NAME = "/usr/lib/libobjc.dylib";

    public static IObjectiveCRuntimeInterop Instance { get; set; } = new ObjectiveCRuntimeInterop();

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessageNative(IntPtr receiver, IntPtr selector);

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessageNative(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessageNative(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessageNative(IntPtr receiver, IntPtr selector, string arg1);

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessageNative(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport(LIBRARY_NAME, EntryPoint = "objc_getClass")]
    private static extern IntPtr GetClassNative(string name);

    [DllImport(LIBRARY_NAME, EntryPoint = "sel_registerName")]
    private static extern IntPtr RegisterSelectorNative(string name);

    public IntPtr SendMessage(IntPtr receiver, IntPtr selector) => SendMessageNative(receiver, selector);

    public IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1) => SendMessageNative(receiver, selector, arg1);

    public IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2) => SendMessageNative(receiver, selector, arg1, arg2);

    public IntPtr SendMessage(IntPtr receiver, IntPtr selector, string arg1) => SendMessageNative(receiver, selector, arg1);

    public IntPtr SendMessage(IntPtr receiver, IntPtr selector, bool arg1) => SendMessageNative(receiver, selector, arg1);

    public IntPtr GetClass(string name) => GetClassNative(name);

    public IntPtr RegisterSelector(string name) => RegisterSelectorNative(name);
}
