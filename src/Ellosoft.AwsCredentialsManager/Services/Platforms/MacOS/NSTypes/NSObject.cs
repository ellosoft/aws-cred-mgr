namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

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
        if (!disposing) return;

        Handle.SafeReleaseIntPrtMem();
    }

    protected static IntPtr GetClass(string name) => ObjectiveCRuntime.Instance.GetClass(name);
    protected static IntPtr GetSelector(string name) => ObjectiveCRuntime.Instance.RegisterSelector(name);

    protected static Lazy<IntPtr> GetClassLazy(string name) => new(() => GetClass(name));

    protected static Lazy<IntPtr> GetSelectorLazy(string name) => new(() => GetSelector(name));
}
