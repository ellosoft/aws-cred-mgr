// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Http;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestHttpMessageHandlerBuilder(IServiceProvider serviceProvider, IServer server) : HttpMessageHandlerBuilder
{
    private readonly HttpMessageHandler _testHandler = (server as TestServer)!.CreateHandler();

    public override IServiceProvider Services => serviceProvider;

    public override IList<DelegatingHandler> AdditionalHandlers { get; } = [];

    public override string? Name { get; set; }

    public override HttpMessageHandler PrimaryHandler
    {
        get => _testHandler;
        set => _ = value;
    }

    public override HttpMessageHandler Build() => CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
}
