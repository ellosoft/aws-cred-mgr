// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Configuration;

public sealed class ConfigWriterTests : IDisposable
{
    private readonly ConfigWriter _configWriter = new();
    private readonly ConfigReader _configReader = new();
    private readonly List<string> _tempFiles = [];

    private string CreateTempFilePath()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        _tempFiles.Add(path + ".bak");

        return path;
    }

    [Fact]
    public void Write_ConfigWithCredentials_ShouldBeReadableByConfigReader()
    {
        var config = new AppConfig();
        config.Credentials["test_profile"] = new CredentialsConfiguration
        {
            RoleArn = "arn:aws:iam::123:role:/test_role",
            AwsProfile = "default",
            OktaAppUrl = "https://test.okta.com/home/amazon_aws/abc/272",
            OktaProfile = "default"
        };

        var filePath = CreateTempFilePath();

        _configWriter.Write(filePath, config);

        var readConfig = _configReader.Read(filePath);
        readConfig.Credentials.ShouldContainKey("test_profile");
        readConfig.Credentials["test_profile"].RoleArn.ShouldBe("arn:aws:iam::123:role:/test_role");
        readConfig.Credentials["test_profile"].AwsProfile.ShouldBe("default");
        readConfig.Credentials["test_profile"].OktaAppUrl.ShouldBe("https://test.okta.com/home/amazon_aws/abc/272");
        readConfig.Credentials["test_profile"].OktaProfile.ShouldBe("default");
    }

    [Fact]
    public void Write_ConfigWithVariables_ShouldIncludeSeparator()
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

        var inputPath = CreateTempFilePath();
        File.WriteAllText(inputPath, yaml);

        var config = _configReader.Read(inputPath);
        var outputPath = CreateTempFilePath();

        _configWriter.Write(outputPath, config);

        var content = File.ReadAllText(outputPath);
        content.ShouldContain("---");
        content.ShouldContain("my_var");
        content.ShouldContain("test_value");
    }

    [Fact]
    public void Write_EmptyConfig_ShouldNotThrow()
    {
        var config = new AppConfig();
        var filePath = CreateTempFilePath();

        var act = () => _configWriter.Write(filePath, config);

        Should.NotThrow(act);
        File.Exists(filePath).ShouldBeTrue();
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
