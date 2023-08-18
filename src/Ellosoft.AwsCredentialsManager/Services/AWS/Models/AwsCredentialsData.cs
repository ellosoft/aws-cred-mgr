// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.AWS.Models;

public record AwsCredentialsData(string AccessKeyId, string SecretAccessKey, string SessionToken, DateTime ExpirationDateTime);
