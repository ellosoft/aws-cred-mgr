// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ellosoft.AwsCredentialsManager.Services.IO;

public interface IFileManager
{
    void OpenFile(string filePath);
}

public class FileManager : IFileManager
{
    public void OpenFile(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer", $"\"{filePath}\"");

            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"\"{filePath}\"");

            return;
        }

        Process.Start("xdg-open", $"\"{filePath}\"");
    }
}
