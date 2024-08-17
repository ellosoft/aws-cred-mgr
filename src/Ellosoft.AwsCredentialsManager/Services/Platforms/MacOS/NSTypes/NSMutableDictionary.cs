namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

[SupportedOSPlatform("macos")]
public class NSMutableDictionary : NSObject
{
    private static readonly IntPtr NSClassType = GetClass("NSMutableDictionary");
    private static readonly IntPtr CreateDictionarySelector = GetSelector("dictionary");
    private static readonly IntPtr AddKeyValueSelector = GetSelector("setObject:forKey:");

    public NSMutableDictionary()
    {
        Handle = ObjectiveCRuntimeInterop.Instance.SendMessage(NSClassType, CreateDictionarySelector);
    }

    public void Add(string key, object value)
    {
        var nsKey = new NSString(key);
        var nsValue = ConvertManagedTypeToNSObject(value);

        ObjectiveCRuntimeInterop.Instance.SendMessage(Handle, AddKeyValueSelector, nsValue.Handle, nsKey.Handle);
    }

    private static NSObject ConvertManagedTypeToNSObject(object value) => value switch
    {
        string s => new NSString(s),
        bool b => new NSNumber(b),
        int i => new NSNumber(i),
        _ => throw new NotSupportedException($"Unsupported type: {value.GetType()}")
    };

    public static NSMutableDictionary Create(Dictionary<string, object> dictionary)
    {
        var nsMutableDictionary = new NSMutableDictionary();

        foreach (var (key, value) in dictionary)
        {
            nsMutableDictionary.Add(key, value);
        }

        return nsMutableDictionary;
    }
}
