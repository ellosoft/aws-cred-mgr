// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Http;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestRequestContext
{
    public HttpRequest Request { get; set; } = null!;

    public HttpResponse Response { get; set; } = null!;

    public object? RequestModel { get; set; }
}
