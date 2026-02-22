// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using NSubstitute;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Configuration;

public class CredentialsManagerTests
{
    private readonly IConfigManager _configManager = Substitute.For<IConfigManager>();
    private readonly CredentialsManager _credentialsManager;
    private readonly AppConfig _appConfig = new();

    public CredentialsManagerTests()
    {
        _configManager.AppConfig.Returns(_appConfig);
        _credentialsManager = new CredentialsManager(_configManager);
    }

    [Fact]
    public void TryGetCredential_WhenCredentialExistsWithValidOktaProperties_ShouldReturnTrue()
    {
        _appConfig.Credentials["test_profile"] = new CredentialsConfiguration
        {
            RoleArn = "arn:aws:iam::123:role/TestRole",
            OktaAppUrl = "https://test.okta.com/home/amazon_aws/abc/272",
            OktaProfile = "default"
        };

        var result = _credentialsManager.TryGetCredential("test_profile", out var config);

        result.ShouldBeTrue();
        config.ShouldNotBeNull();
        config!.RoleArn.ShouldBe("arn:aws:iam::123:role/TestRole");
        config.OktaAppUrl.ShouldBe("https://test.okta.com/home/amazon_aws/abc/272");
        config.OktaProfile.ShouldBe("default");
    }

    [Fact]
    public void TryGetCredential_WhenCredentialDoesNotExist_ShouldReturnFalse()
    {
        var result = _credentialsManager.TryGetCredential("nonexistent", out var config);

        result.ShouldBeFalse();
        config.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "default")]
    [InlineData("https://test.okta.com/app", null)]
    [InlineData(null, null)]
    public void TryGetCredential_WhenOktaPropertiesAreNull_ShouldReturnFalse(
        string? oktaAppUrl, string? oktaProfile)
    {
        _appConfig.Credentials["incomplete_profile"] = new CredentialsConfiguration
        {
            RoleArn = "arn:aws:iam::123:role/TestRole",
            OktaAppUrl = oktaAppUrl,
            OktaProfile = oktaProfile
        };

        var result = _credentialsManager.TryGetCredential("incomplete_profile", out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void CreateCredential_ShouldAddToConfigAndCallSaveConfig()
    {
        _credentialsManager.CreateCredential(
            "new_profile",
            "my_aws_profile",
            "arn:aws:iam::456:role/NewRole",
            "https://test.okta.com/home/amazon_aws/xyz/123",
            "default");

        _appConfig.Credentials.ShouldContainKey("new_profile");
        var cred = _appConfig.Credentials["new_profile"];
        cred.RoleArn.ShouldBe("arn:aws:iam::456:role/NewRole");
        cred.AwsProfile.ShouldBe("my_aws_profile");
        cred.OktaAppUrl.ShouldBe("https://test.okta.com/home/amazon_aws/xyz/123");
        cred.OktaProfile.ShouldBe("default");
        _configManager.Received(1).SaveConfig();
    }
}
