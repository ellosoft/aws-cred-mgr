namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

[SupportedOSPlatform("macos")]
public class NSString : NSObject
{
    private static readonly IntPtr NSClassType = GetClass("NSString");
    private static readonly IntPtr CreateStringSelector = GetSelector("stringWithUTF8String:");

    public NSString(string value)
    {
        Handle = ObjectiveCRuntimeInterop.Instance.SendMessage(NSClassType, CreateStringSelector, value);
    }
}
