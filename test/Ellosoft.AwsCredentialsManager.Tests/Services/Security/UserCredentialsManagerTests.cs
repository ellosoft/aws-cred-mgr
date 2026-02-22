// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Text.Json;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Security;
using NSubstitute;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Security;

public class UserCredentialsManagerTests
{
    private readonly ISecureStorage _secureStorage = Substitute.For<ISecureStorage>();
    private readonly UserCredentialsManager _manager;

    public UserCredentialsManagerTests()
    {
        _manager = new UserCredentialsManager(_secureStorage);
    }

    [Fact]
    public void SaveUserCredentials_ShouldStoreJsonToSecureStorage()
    {
        var credentials = new UserCredentials("testuser", "testpass");

        _manager.SaveUserCredentials("test_key", credentials);

        _secureStorage.Received(1).StoreSecret("test_key", Arg.Is<string>(json =>
            json.Contains("testuser") && json.Contains("testpass")));
    }

    [Fact]
    public void GetUserCredentials_WhenDataExists_ShouldReturnDeserializedCredentials()
    {
        var credentials = new UserCredentials("testuser", "testpass");
        var json = JsonSerializer.Serialize(credentials);

        _secureStorage.TryRetrieveSecret("test_key", out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = json;

                return true;
            });

        var result = _manager.GetUserCredentials("test_key");

        result.ShouldNotBeNull();
        result!.Username.ShouldBe("testuser");
        result.Password.ShouldBe("testpass");
    }

    [Fact]
    public void GetUserCredentials_WhenNoData_ShouldReturnNull()
    {
        _secureStorage.TryRetrieveSecret("missing_key", out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = null;

                return false;
            });

        var result = _manager.GetUserCredentials("missing_key");

        result.ShouldBeNull();
    }
}
