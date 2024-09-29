// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Serilog;
using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration;

public class TestFixture : IAsyncLifetime
{
    public ITestOutputHelper? TestOutputHelper { get; set; }

    public WebApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        App = CreateTestApp();
        App.MapControllers();
        await App.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Log.CloseAndFlushAsync();
        await App.DisposeAsync();
    }

    private WebApplication CreateTestApp()
    {
        var builder = WebApplication.CreateSlimBuilder();

        ConfigureTestServices(builder.Services);

        // configure test app setup
        builder.WebHost.UseTestServer();

        // configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Ellosoft", LogEventLevel.Verbose)
            .WriteTo.Sink(new TestLogEventSink(() => TestOutputHelper))
            .CreateLogger();

        builder.Logging.AddSerilog(Log.Logger);

        return builder.Build();
    }

    private static void ConfigureTestServices(IServiceCollection services)
    {
        // configure test services
        services.AddTransient<HttpMessageHandlerBuilder, TestHttpMessageHandlerBuilder>();
        services.AddHttpClient();
        services.AddControllers().AddApplicationPart(typeof(IntegrationTest).Assembly);

        // add app services
        services.AddAppServices();
    }
}
