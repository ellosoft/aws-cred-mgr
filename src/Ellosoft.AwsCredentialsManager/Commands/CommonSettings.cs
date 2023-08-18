// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Commands;

public class CommonSettings : CommandSettings, ILogLevelSettings
{
    [Description("Set log level <verbose|debug|info|warn|error>")]
    [TypeConverter(typeof(LogLevelConverter))]
    [DefaultValue("warn")]
    [CommandOption("--log-level <LEVEL>")]
    public LogEventLevel LogLevel { get; set; }
}
