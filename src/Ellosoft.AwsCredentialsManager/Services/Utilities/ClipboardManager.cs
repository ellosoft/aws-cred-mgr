// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Diagnostics;

namespace Ellosoft.AwsCredentialsManager.Services.Utilities;

public interface IClipboardManager
{
    /// <summary>
    ///     Set the clipboard text
    /// </summary>
    /// <param name="text">Text to copy to the clipboard</param>
    /// <returns>True if the operation was successful, false otherwise</returns>
    bool SetClipboardText(string text);
}

public class ClipboardManager : PlatformServiceSlim, IClipboardManager
{
    private readonly string _command = ExecuteMultiPlatformCommand(
        win: () => "clip",
        macos: () => "/usr/bin/pbcopy",
        linux: () => "xclip");

    public bool SetClipboardText(string text) => ExecuteCliCommand(_command, text) == 0;

    private static int ExecuteCliCommand(string command, string? stdin = null)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        if (stdin is not null)
        {
            using var writer = process.StandardInput;

            if (writer.BaseStream.CanWrite)
            {
                writer.Write(stdin);
                writer.Flush();
            }

            writer.Close();
        }

        process.WaitForExit();

        return process.ExitCode;
    }
}
