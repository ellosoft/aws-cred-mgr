namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

[SupportedOSPlatform("macos")]
public class NSNumber : NSObject
{
    private static readonly IntPtr NSClassType = GetClass("NSNumber");
    private static readonly IntPtr CreateBoolSelector = GetSelector("numberWithBool:");
    private static readonly Lazy<IntPtr> CreateIntSelector = GetSelectorLazy("numberWithInt:");

    public NSNumber(bool value)
    {
        Handle = ObjectiveCRuntimeInterop.Instance.SendMessage(NSClassType, CreateBoolSelector, value);
    }

    public NSNumber(int value)
    {
        Handle = ObjectiveCRuntimeInterop.Instance.SendMessage(NSClassType, CreateIntSelector.Value, value);
    }
}
