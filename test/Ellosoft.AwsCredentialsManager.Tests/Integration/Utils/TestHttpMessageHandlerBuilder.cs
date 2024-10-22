// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Http;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestHttpMessageHandlerBuilder(IServiceProvider serviceProvider, IServer server, string correlationId) : HttpMessageHandlerBuilder
{
    private readonly HttpMessageHandler _testHandler = (server as TestServer)!.CreateHandler();

    public override IServiceProvider Services => serviceProvider;

    public override IList<DelegatingHandler> AdditionalHandlers { get; } = [new CorrelationIdMessageHandler(correlationId)];

    public override string? Name { get; set; }

    public override HttpMessageHandler PrimaryHandler
    {
        get => _testHandler;
        set => _ = value;
    }

    public override HttpMessageHandler Build() => CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
}

public class CorrelationIdMessageHandler(string correlationId) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("Correlation-Id", correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
