// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Globalization;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;

namespace Ellosoft.AwsCredentialsManager.Services.AWS;

public class RdsTokenGenerator
{
    /// <summary>
    ///     Generates an RDS DB password using AWS credentials
    /// </summary>
    /// <param name="awsCredentials">AWS credentials.</param>
    /// <param name="region">AWS region.</param>
    /// <param name="hostname">Hostname of the RDS instance.</param>
    /// <param name="port">Port number of the RDS instance.</param>
    /// <param name="dbUser">Database user to authenticate.</param>
    /// <param name="ttlInMinutes">RDS password lifetime (recommended 15 minutes)</param>
    /// <returns>RDS DB password</returns>
    /// <remarks>
    ///     This method is based on the <see cref="RDSAuthTokenGenerator" />,
    ///     however it allows the DB password lifetime to be change from the hard code 15 minutes.
    /// </remarks>
    public string GenerateDbPassword(
        AWSCredentials awsCredentials,
        RegionEndpoint region,
        string hostname,
        int port,
        string dbUser,
        int ttlInMinutes)
    {
        var immutableCredentials = awsCredentials.GetCredentials();

        var request = new DefaultRequest(new GenerateRdsAuthTokenRequest(), "rds-db")
        {
            UseQueryString = true,
            HttpMethod = "GET",
            Endpoint = new UriBuilder("https", hostname, port).Uri
        };

        request.Parameters.Add("X-Amz-Expires", TimeSpan.FromMinutes(ttlInMinutes).TotalSeconds.ToString(CultureInfo.InvariantCulture));
        request.Parameters.Add("DBUser", dbUser);
        request.Parameters.Add("Action", "connect");

        if (immutableCredentials.UseToken)
            request.Parameters["X-Amz-Security-Token"] = immutableCredentials.Token;

        var str = "&" + AWS4PreSignedUrlSigner
            .SignRequest(request, null, new RequestMetrics(), immutableCredentials.AccessKey, immutableCredentials.SecretKey, "rds-db", region.SystemName)
            .ForQueryParameters;

        return AmazonServiceClient.ComposeUrl(request).AbsoluteUri["https://".Length..] + str;
    }

    private sealed class GenerateRdsAuthTokenRequest : AmazonWebServiceRequest
    {
        public GenerateRdsAuthTokenRequest() => ((IAmazonWebServiceRequest)this).SignatureVersion = SignatureVersion.SigV4;
    }
}
