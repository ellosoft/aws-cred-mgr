// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

public class OktaApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public OktaApiError? OktaApiError { get; }

    public OktaApiException(HttpStatusCode statusCode, OktaApiError? oktaApiError)
    {
        StatusCode = statusCode;
        OktaApiError = oktaApiError;
    }
}
