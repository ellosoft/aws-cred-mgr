// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestRequestsFilter : IAsyncActionFilter
{
    public static readonly Dictionary<string, List<TestRequestContext>> Requests = [];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var contextData = GetRequestContextList(context);

        var actionExecutedContext = await next();

        contextData.Add(
            new TestRequestContext
            {
                Request =  CloneRequest(context.HttpContext.Request),
                Response = actionExecutedContext.HttpContext.Response,
                RequestModel = context.ActionArguments.Values.FirstOrDefault()
            });
    }

    private static List<TestRequestContext> GetRequestContextList(FilterContext context)
    {
        var correlationId = context.HttpContext.Request.Headers["Correlation-Id"].ToString();

        if (!Requests.TryGetValue(correlationId, out var requestContext))
        {
            requestContext = [];
            Requests.Add(correlationId, requestContext);
        }

        return requestContext;
    }

    private static  HttpRequestMessage CloneRequest(HttpRequest request)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(request.Method),
            RequestUri = new Uri($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}")
        };

        foreach (var header in request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
            }
        }

        return requestMessage;
    }
}
