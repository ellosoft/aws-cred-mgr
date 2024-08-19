// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Services.IO;

namespace Ellosoft.AwsCredentialsManager.Commands.Utils;

[Name("logs")]
[Description("Open log file")]
[Examples("logs")]
public class OpenLogs(IFileManager fileManager) : Command
{
    public override int Execute(CommandContext context)
    {
        if (!File.Exists(LogRegistration.LogFileName))
            AnsiConsole.MarkupLine("[yellow]Unable to find log file[/]");

        fileManager.OpenFileUsingDefaultApp(LogRegistration.LogFileName);

        return 0;
    }
}
