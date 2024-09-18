
// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestLogEventSink(Func<ITestOutputHelper?> testOutputHelperProvider) : ILogEventSink
{
    private readonly List<string> _preInitializedMessages = [];
    private readonly MessageTemplateTextFormatter _messageFormatter = new("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}");

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _messageFormatter.Format(logEvent, writer);
        var logMessage = writer.ToString();

        if (logEvent.Exception is not null)
            logMessage += Environment.NewLine + logEvent.Exception;

        var testOutputHelper = testOutputHelperProvider();

        if (testOutputHelper is null)
        {
            _preInitializedMessages.Add(logMessage);

            return;
        }

        if (_preInitializedMessages.Count > 0)
        {
            _preInitializedMessages.ForEach(testOutputHelper.WriteLine);
            _preInitializedMessages.Clear();
        }

        try
        {
            testOutputHelper.WriteLine(logMessage);
        }
        catch
        {
            // ignore log message if the test output helper is not available
        }
    }
}
