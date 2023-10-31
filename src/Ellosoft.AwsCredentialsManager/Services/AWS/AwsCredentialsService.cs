// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Ellosoft.AwsCredentialsManager.Services.AWS.Models;

namespace Ellosoft.AwsCredentialsManager.Services.AWS;

public class AwsCredentialsService
{
    internal sealed record ProfileMetadata(string AccessKey, DateTime Expiration);

    /// <summary>
    ///     Assume AWS role and retrieve its credentials by using the SAML authentication assertion
    /// </summary>
    /// <param name="samlAssertion">A SAML authentication assertion used to authenticate the call to AWS STS.</param>
    /// <param name="roleArn">AWS role to be assumed.</param>
    /// <param name="idp">ARN of the SAML provider in AWS.</param>
    /// <param name="expirationInMinutes">The duration, in minutes, for which the temporary security credentials are valid. (default 120 min)</param>
    /// <returns>AwsCredentialsData containing retrieved temporary AWS credentials.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the SAML authentication assertion fails to meet the requirements
    ///     or AWS STS is unable to assume the specified role due to invalid parameters.
    /// </exception>
    public async Task<AwsCredentialsData> GetAwsCredentials(
        string samlAssertion,
        string roleArn,
        string idp,
        int expirationInMinutes = 120)
    {
        using var stsClient = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), (RegionEndpoint?)null);

        var request = new AssumeRoleWithSAMLRequest
        {
            DurationSeconds = expirationInMinutes * 60,
            RoleArn = roleArn,
            PrincipalArn = idp,
            SAMLAssertion = samlAssertion
        };

        try
        {
            var response = await stsClient.AssumeRoleWithSAMLAsync(request);

            return new AwsCredentialsData(
                response.Credentials.AccessKeyId,
                response.Credentials.SecretAccessKey,
                response.Credentials.SessionToken,
                response.Credentials.Expiration);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to assume AWS role {roleArn} using SAML", ex);
        }
    }

    /// <summary>
    ///     Store AWS credentials for a specific profile in the AWS credentials file.
    /// </summary>
    /// <param name="awsProfileName">The name of the AWS profile where the credentials should be stored.</param>
    /// <param name="credentials">AWS credentials to be stored.</param>
    public void StoreCredentials(string awsProfileName, AwsCredentialsData credentials)
    {
        var options = new CredentialProfileOptions
        {
            AccessKey = credentials.AccessKeyId,
            SecretKey = credentials.SecretAccessKey,
            Token = credentials.SessionToken
        };

        var profile = new CredentialProfile(awsProfileName, options);

        var sharedFile = new SharedCredentialsFile();
        sharedFile.RegisterProfile(profile);

        SaveProfileMetadata(awsProfileName, new ProfileMetadata(credentials.AccessKeyId, credentials.ExpirationDateTime));
    }

    /// <summary>
    ///     Retrieve AWS credentials for a specific profile from the AWS credentials file.
    /// </summary>
    /// <param name="awsProfileName">The name of the AWS profile where the credentials should be retrieved.</param>
    /// <returns>
    ///     AwsCredentialsData containing retrieved AWS credentials or
    ///     null if the profile isn't found or if the credential is about expire (15 min threshold).
    /// </returns>
    public AwsCredentialsData? GetCredentialsFromStore(string awsProfileName)
    {
        var sharedFile = new CredentialProfileStoreChain();

        if (!sharedFile.TryGetProfile(awsProfileName, out var profile) || !profile.CanCreateAWSCredentials)
            return null;

        var awsCredentials = profile.GetAWSCredentials(sharedFile);
        var immutableCredentials = awsCredentials.GetCredentials();

        var profileMetadata = GetProfileMetadata(awsProfileName);

        if (profileMetadata is null || profileMetadata.AccessKey != immutableCredentials.AccessKey || profileMetadata.Expiration < DateTime.Now.AddMinutes(15))
            return null;

        return new AwsCredentialsData(
            immutableCredentials.AccessKey,
            immutableCredentials.SecretKey,
            immutableCredentials.Token,
            profileMetadata.Expiration);
    }

    private static void SaveProfileMetadata(string profileName, ProfileMetadata metadata)
    {
        File.WriteAllBytes(GetProfileMetadataFilePath(profileName),
            JsonSerializer.SerializeToUtf8Bytes(metadata, SourceGenerationContext.Default.ProfileMetadata));
    }

    private static ProfileMetadata? GetProfileMetadata(string profileName)
    {
        var bytes = File.ReadAllBytes(GetProfileMetadataFilePath(profileName));

        return JsonSerializer.Deserialize(bytes, SourceGenerationContext.Default.ProfileMetadata);
    }

    private static string GetProfileMetadataFilePath(string profileName) => AppDataDirectory.GetPath($"aws_profiles\\{profileName}");
}
