// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Configuration;

public sealed class ConfigReaderTests : IDisposable
{
    private readonly ConfigReader _configReader = new();
    private readonly List<string> _tempFiles = [];

    private string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);

        return path;
    }

    [Fact]
    public void Read_BasicConfig_ShouldParseAuthenticationAndCredentials()
    {
        var yaml = """
                   authentication:
                     okta:
                       default:
                         okta_domain: https://test.okta.com/
                         preferred_mfa_type: push
                         auth_type: classic

                   credentials:
                     test_profile:
                       role_arn: arn:aws:iam::123:role:/test_role
                       aws_profile: default
                       okta_app_url: https://test.okta.com/home/amazon_aws/abc/272
                       okta_profile: default
                   """;

        var filePath = CreateTempFile(yaml);

        var config = _configReader.Read(filePath);

        config.Authentication.ShouldNotBeNull();
        config.Authentication!.Okta.ShouldContainKey("default");
        config.Authentication.Okta["default"].OktaDomain.ShouldBe("https://test.okta.com/");
        config.Authentication.Okta["default"].PreferredMfaType.ShouldBe("push");
        config.Authentication.Okta["default"].AuthType.ShouldBe("classic");

        config.Credentials.ShouldContainKey("test_profile");
        var cred = config.Credentials["test_profile"];
        cred.RoleArn.ShouldBe("arn:aws:iam::123:role:/test_role");
        cred.AwsProfile.ShouldBe("default");
        cred.OktaAppUrl.ShouldBe("https://test.okta.com/home/amazon_aws/abc/272");
        cred.OktaProfile.ShouldBe("default");
    }

    [Fact]
    public void Read_ConfigWithVariables_ShouldSubstituteVariables()
    {
        var yaml = """
                   variables:
                     my_var: test_value
                   ---
                   templates:
                     rds:
                       test_db:
                         hostname: test.host
                         port: 5432
                         username: ${my_var}
                         region: us-east-2
                   """;

        var filePath = CreateTempFile(yaml);

        var config = _configReader.Read(filePath);

        config.Templates.ShouldNotBeNull();
        config.Templates!.Rds.ShouldContainKey("test_db");
        config.Templates.Rds["test_db"].Username.ShouldBe("test_value");
        config.Templates.Rds["test_db"].Hostname.ShouldBe("test.host");
        config.Templates.Rds["test_db"].Port.ShouldBe(5432);
        config.Templates.Rds["test_db"].Region.ShouldBe("us-east-2");
    }

    [Fact]
    public void Read_EmptyYamlFile_ShouldReturnEmptyAppConfig()
    {
        var filePath = CreateTempFile(string.Empty);

        var config = _configReader.Read(filePath);

        config.ShouldNotBeNull();
        config.Credentials.ShouldBeEmpty();
        config.Authentication.ShouldBeNull();
    }

    [Fact]
    public void Read_InvalidYaml_ShouldThrowInvalidOperationException()
    {
        var yaml = """
                   credentials:
                     test_profile:
                       role_arn: arn:aws:iam::123:role:/test_role
                       invalid_indent:
                     bad: [
                   """;

        var filePath = CreateTempFile(yaml);

        var act = () => _configReader.Read(filePath);

        Should.Throw<InvalidOperationException>(act);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); }
            catch { /* ignore cleanup errors */ }
        }
    }
}
