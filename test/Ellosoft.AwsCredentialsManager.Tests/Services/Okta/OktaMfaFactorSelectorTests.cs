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
    [InlineData("fastpass", "signed_nonce")]
    [InlineData("FastPass", "signed_nonce")]
    [InlineData("okta_verify", "signed_nonce")]
    [InlineData("signed_nonce", "signed_nonce")]
    public void GetOktaMfaFactorCode_SupportedValues_ShouldNormalize(string input, string expected)
    {
        var result = OktaMfaFactorSelector.GetOktaMfaFactorCode(input);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("signed_nonce", true)]
    [InlineData("push", false)]
    [InlineData("token:software:totp", false)]
    [InlineData(null, false)]
    public void IsFastPass_ShouldDetectSignedNonceFactorCode(string? factorCode, bool expected)
    {
        OktaMfaFactorSelector.IsFastPass(factorCode).ShouldBe(expected);
    }

    [Theory]
    [InlineData("sms")]
    [InlineData("duo")]
    [InlineData("fingerprint")]
    [InlineData("app")]
    [InlineData("")]
    public void GetOktaMfaFactorCode_UnsupportedValues_ShouldThrow(string input)
    {
        var act = () => OktaMfaFactorSelector.GetOktaMfaFactorCode(input);

        Should.Throw<NotSupportedException>(act)
            .Message.ShouldContain(input);
    }
}
