// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Globalization;
using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

public class LogLevelConverter : TypeConverter
{
    private const string INVALID_LOG_ERROR = "Invalid log level";

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string stringValue)
        {
            throw new NotSupportedException(INVALID_LOG_ERROR);
        }

        return stringValue switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "info" => LogEventLevel.Information,
            "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            _ => throw new InvalidOperationException(INVALID_LOG_ERROR)
        };
    }
}
