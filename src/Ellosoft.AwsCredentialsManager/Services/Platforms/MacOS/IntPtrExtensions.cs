using System.Runtime.InteropServices;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS;

public static class IntPtrExtensions
{
    public static void SafeReleaseIntPrtMem(this IntPtr handle)
    {
        try
        {
            if (handle == IntPtr.Zero)
                return;

            Marshal.FreeHGlobal(handle);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Unable to release memory");
        }
    }
}
