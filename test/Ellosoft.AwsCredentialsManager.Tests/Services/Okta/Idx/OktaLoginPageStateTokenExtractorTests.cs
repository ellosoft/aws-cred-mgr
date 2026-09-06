// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

public class OktaLoginPageStateTokenExtractorTests
{
    [Fact]
    public void Extract_WhenStateTokenIsInScriptVariable_ShouldReturnToken()
    {
        const string HTML = """
                            <html><head><script>
                            var oktaData = {};
                            var stateToken = '02vQULJDA20fnlkloDn2swWJkaxVTPQ10lyJH6I5c';
                            </script></head></html>
                            """;

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBe("02vQULJDA20fnlkloDn2swWJkaxVTPQ10lyJH6I5c");
    }

    [Fact]
    public void Extract_WhenStateTokenIsJavaScriptEscaped_ShouldUnescape()
    {
        // Okta escapes '-' as \x2D and '_' as \x5F in the sign-in page script
        const string HTML = "<script>var stateToken = '02abc\\x2Ddef\\x5Fxyz';</script>";

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBe("02abc-def_xyz");
    }

    [Fact]
    public void Extract_WhenStateTokenIsInJsonConfig_ShouldReturnToken()
    {
        const string HTML = """<script>var config = {"signIn":{"stateToken":"02json-token","baseUrl":"https://xyz.okta.com"}};</script>""";

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBe("02json-token");
    }

    [Fact]
    public void Extract_WhenStateTokenIsJwtStyleIdentityEngineToken_ShouldReturnToken()
    {
        // Identity Engine sign-in page rendered by /oauth2/v1/authorize embeds a JWE state token
        const string HTML = """<script type="text/javascript">var stateToken = 'eyJ6aXAiOiJERUYiLCJhbGlhcyI6ImVuY3J5cHRpb25rZXkifQ.abc\x2Ddef\x5Fghi.xyz';</script>""";

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBe("eyJ6aXAiOiJERUYiLCJhbGlhcyI6ImVuY3J5cHRpb25rZXkifQ.abc-def_ghi.xyz");
    }

    [Fact]
    public void Extract_WhenStateTokenIsEmpty_ShouldReturnNull()
    {
        // Classic Engine sign-in page declares the variable but leaves it empty
        const string HTML = "<script>var stateToken = '';\nvar fromUri = '\\x2F';</script>";

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBeNull();
    }

    [Fact]
    public void Extract_WhenNoStateToken_ShouldReturnNull()
    {
        const string HTML = "<html><body>Classic engine sign-in page</body></html>";

        OktaLoginPageStateTokenExtractor.Extract(HTML).ShouldBeNull();
    }
}
