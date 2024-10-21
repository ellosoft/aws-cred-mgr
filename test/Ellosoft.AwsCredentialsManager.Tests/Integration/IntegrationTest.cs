// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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

        // configure test services
        services.AddTransient<HttpMessageHandlerBuilder>(_ =>
            new TestHttpMessageHandlerBuilder(TestFixture.App.Services, TestFixture.App.GetTestServer()));
        services.AddHttpClient();
        services.AddLogging(config => config.AddSerilog(TestFixture.Logger));

        // add app services
        services.AddAppServices();

        App = new TestCommandApp(services);
    }

    private TestFixture TestFixture { get; }

    protected TestCommandApp App { get; }

    protected WebApplication TestWebApp => TestFixture.App;
}

[CollectionDefinition(nameof(IntegrationTest))]
public class IntegrationTestsCollection : ICollectionFixture<TestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
