using System.Runtime.InteropServices;
using System.Text;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.NSTypes;

public class NSData : NSObject
{
    private static readonly IntPtr LengthSelector = GetSelector("length");
    private static readonly IntPtr BytesSelector = GetSelector("bytes");

    public NSData(IntPtr handle) => Handle = handle;

    public override string ToString()
    {
        var length = (int)ObjectiveCRuntime.Instance.SendMessage(Handle, LengthSelector);
        var bytes = ObjectiveCRuntime.Instance.SendMessage(Handle, BytesSelector);

        var buffer = new byte[length];
        Marshal.Copy(bytes, buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }
}
