// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Spectre.Console;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration;

public class TestFixture : IAsyncLifetime
{
    public ITestOutputHelper? TestOutputHelper { get; set; }

    public WebApplication WebApp { get; private set; } = null!;

    public IServiceProvider Services => WebApp.Services;

    public async Task InitializeAsync()
    {
        WebApp = CreateTestApp();
        WebApp.MapControllers();
        await WebApp.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Log.CloseAndFlushAsync();
        await WebApp.DisposeAsync();
    }

    private WebApplication CreateTestApp()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.Services.AddAppServices();
        builder.Services.AddSingleton<IAnsiConsole>(_ => Substitute.For<IAnsiConsole>());

        ConfigureTestServices(builder.Services);

        // configure test app setup
        builder.WebHost.UseTestServer();

        // configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .MinimumLevel.Override("Ellosoft", LogEventLevel.Verbose)
            .WriteTo.Sink(new TestLogEventSink(() => TestOutputHelper))
            .CreateLogger();

        builder.Logging.AddSerilog(Log.Logger);

        return builder.Build();
    }

    private static void ConfigureTestServices(IServiceCollection services)
    {
        services
            .AddControllers(opt => opt.Filters.Add<TestRequestsFilter>())
            .AddApplicationPart(typeof(IntegrationTest).Assembly);
    }
}
