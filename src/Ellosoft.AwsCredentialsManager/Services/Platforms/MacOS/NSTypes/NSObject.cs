namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

[SupportedOSPlatform("macos")]
public abstract class NSObject : IDisposable
{
    public IntPtr Handle { get; protected init; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected static IntPtr GetClass(string name) => ObjectiveCRuntimeInterop.Instance.GetClass(name);
    protected static IntPtr GetSelector(string name) => ObjectiveCRuntimeInterop.Instance.RegisterSelector(name);

    protected static Lazy<IntPtr> GetClassLazy(string name) => new(() => GetClass(name));

    protected static Lazy<IntPtr> GetSelectorLazy(string name) => new(() => GetSelector(name));
}
