// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public static class OktaHttpClient
{
    public static void Configure(HttpClient httpClient)
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
    }
}
