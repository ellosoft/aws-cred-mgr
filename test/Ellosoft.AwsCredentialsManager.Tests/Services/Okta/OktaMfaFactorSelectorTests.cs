// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta;

public class OktaMfaFactorSelectorTests
{
    [Theory]
    [InlineData("push", "push")]
    [InlineData("PUSH", "push")]
    [InlineData("totp", "token:software:totp")]
    [InlineData("code", "token:software:totp")]
    [InlineData("token:software:totp", "token:software:totp")]
    public void GetOktaMfaFactorCode_SupportedValues_ShouldNormalize(string input, string expected)
    {
        var result = OktaMfaFactorSelector.GetOktaMfaFactorCode(input);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("sms")]
    [InlineData("duo")]
    [InlineData("signed_nonce")]
    [InlineData("fingerprint")]
    [InlineData("app")]
    [InlineData("")]
    public void GetOktaMfaFactorCode_UnsupportedValues_ShouldThrow(string input)
    {
        var act = () => OktaMfaFactorSelector.GetOktaMfaFactorCode(input!);

        Should.Throw<NotSupportedException>(act)
            .Message.ShouldContain(input!);
    }
}
