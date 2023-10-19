// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Commands;

public class CommonSettings : CommandSettings, ILogLevelSettings
{
    [CommandOption("--log-level <LOG_LEVEL>")]
    [Description("Set log level <verbose|debug|info|warn|error>")]
    [TypeConverter(typeof(LogLevelConverter))]
    [DefaultValue("warn")]
    public LogEventLevel LogLevel { get; set; }
}
