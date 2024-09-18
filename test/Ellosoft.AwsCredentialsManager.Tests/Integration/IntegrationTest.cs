// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration;

[Collection(nameof(IntegrationTest))]
public class IntegrationTest
{
    protected IntegrationTest(ITestOutputHelper outputHelper, TestFixture fixture)
    {
        TestFixture = fixture;
        TestFixture.TestOutputHelper = outputHelper;
    }

    private TestFixture TestFixture { get; }

    protected IServiceCollection Services => TestFixture.Services;
}

[CollectionDefinition(nameof(IntegrationTest))]
public class IntegrationTestsCollection : ICollectionFixture<TestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
