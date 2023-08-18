// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

public interface ILogLevelSettings
{
    public LogEventLevel LogLevel { get; set; }
}
