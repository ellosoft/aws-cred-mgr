namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

public class NSString : NSObject
{
    private static readonly IntPtr NSClassType = GetClass("NSString");
    private static readonly IntPtr CreateStringSelector = GetSelector("stringWithUTF8String:");

    public NSString(string value)
    {
        Handle = ObjectiveCRuntime.Instance.SendMessage(NSClassType, CreateStringSelector, value);
    }
}
