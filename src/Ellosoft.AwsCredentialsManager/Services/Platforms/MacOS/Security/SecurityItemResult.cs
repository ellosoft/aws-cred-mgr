// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;

public enum SecurityItemResult
{
    Success = 0,
    DuplicateItem = -25299,
    ItemNotFound = -25300,
    AuthFailed = -25293,
    Param = -50,
    Allocate = -108
}
