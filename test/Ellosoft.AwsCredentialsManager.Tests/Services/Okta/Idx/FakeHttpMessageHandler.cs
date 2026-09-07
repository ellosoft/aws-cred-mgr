// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

/// <summary>
///     Scripted HTTP handler: routes requests by "METHOD url" and records every request (with its body)
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>> _prefixRoutes = new(StringComparer.OrdinalIgnoreCase);

    public List<RecordedRequest> Requests { get; } = [];

    public FakeHttpMessageHandler On(HttpMethod method, string url, params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] responders) =>
        Register(_routes, RouteKey(method, url), responders);

    /// <summary>
    ///     Scripts responses for any request whose URL starts with <paramref name="urlPrefix" /> (e.g. URLs with random query values)
    /// </summary>
    public FakeHttpMessageHandler OnPrefix(HttpMethod method, string urlPrefix, params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] responders) =>
        Register(_prefixRoutes, RouteKey(method, urlPrefix), responders);

    private FakeHttpMessageHandler Register(
        Dictionary<string, Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>> routes,
        string key,
        Func<HttpRequestMessage, Task<HttpResponseMessage>>[] responders)
    {
        if (!routes.TryGetValue(key, out var queue))
            routes[key] = queue = new Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>();

        foreach (var responder in responders)
            queue.Enqueue(responder);

        return this;
    }

    public FakeHttpMessageHandler OnJson(HttpMethod method, string url, params string[] jsonResponses) =>
        On(method, url, jsonResponses.Select(json => (Func<HttpRequestMessage, Task<HttpResponseMessage>>)(_ => Task.FromResult(Json(json)))).ToArray());

    public FakeHttpMessageHandler OnStatus(HttpMethod method, string url, params HttpStatusCode[] statusCodes) =>
        On(method, url, statusCodes.Select(code => (Func<HttpRequestMessage, Task<HttpResponseMessage>>)(_ => Task.FromResult(new HttpResponseMessage(code)))).ToArray());

    public FakeHttpMessageHandler OnUnreachable(HttpMethod method, string url) =>
        On(method, url, _ => throw new HttpRequestException("Connection refused"));

    public IEnumerable<RecordedRequest> RequestsTo(HttpMethod method, string url) =>
        Requests.Where(r => r.Method == method && string.Equals(r.Url, url, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<RecordedRequest> RequestsToPrefix(HttpMethod method, string urlPrefix) =>
        Requests.Where(r => r.Method == method && r.Url.StartsWith(urlPrefix, StringComparison.OrdinalIgnoreCase));

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/ion+json") };

    public static HttpResponseMessage Html(string html, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(html, Encoding.UTF8, "text/html") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.ToString(), body, request));

        var key = RouteKey(request.Method, request.RequestUri.ToString());

        if (!_routes.TryGetValue(key, out var queue) || queue.Count == 0)
            queue = _prefixRoutes.FirstOrDefault(r => key.StartsWith(r.Key, StringComparison.OrdinalIgnoreCase)).Value;

        if (queue is null || queue.Count == 0)
            throw new InvalidOperationException($"No scripted response for {key}");

        // the last responder is reused when the script is exhausted
        var responder = queue.Count == 1 ? queue.Peek() : queue.Dequeue();

        return await responder(request);
    }

    private static string RouteKey(HttpMethod method, string url) => $"{method} {url}";

    public sealed record RecordedRequest(HttpMethod Method, string Url, string? Body, HttpRequestMessage Request);
}
