// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Bogus;
using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration;

[Collection(nameof(IntegrationTest))]
public class IntegrationTest
{
    protected IntegrationTest(ITestOutputHelper outputHelper, TestFixture fixture)
    {
        TestFixture = fixture;
        TestFixture.TestOutputHelper = outputHelper;

        var services = new ServiceCollection();
        ConfigureTestServices(services);

        services.AddAppServices();

        App = new TestCommandApp(services);
    }

    protected string TestCorrelationId { get; } = Guid.NewGuid().ToString();

    protected Faker Faker { get; } = new();

    protected TestFixture TestFixture { get; }

    protected TestCommandApp App { get; }

    private void ConfigureTestServices(ServiceCollection services)
    {
        services.AddLogging(config => config.AddSerilog());
        AddIntegrationTestHttpClient(TestFixture.WebApp, services);
    }

    private void AddIntegrationTestHttpClient(IHost app, ServiceCollection services)
    {
        services.AddTransient<HttpMessageHandlerBuilder>(_ =>
            new TestHttpMessageHandlerBuilder(app.Services, app.GetTestServer(), TestCorrelationId));

        services.AddHttpClient();
    }
}

[CollectionDefinition(nameof(IntegrationTest))]
public class IntegrationTestsCollection : ICollectionFixture<TestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
