// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;

namespace Ellosoft.AwsCredentialsManager.Services.Utilities;

public class ClipboardManager : PlatformService
{
    private readonly string _clipboardCommand = string.Empty;
    private readonly bool _isSupportedPlatform;

    public ClipboardManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _clipboardCommand = "clip";
            _isSupportedPlatform = true;
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _clipboardCommand = "pbcopy";
            _isSupportedPlatform = true;
        }
    }

    protected override bool IsSupportedPlatform() => _isSupportedPlatform;

    public Task<bool> SetClipboardTextAsync(string text)
    {
        return ExecuteMultiPlatformCommand(async () =>
        {
            var cmdResult = await Cli.Wrap(_clipboardCommand)
                .WithStandardInputPipe(PipeSource.FromString(text))
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            return cmdResult.IsSuccess;
        });
    }
}
