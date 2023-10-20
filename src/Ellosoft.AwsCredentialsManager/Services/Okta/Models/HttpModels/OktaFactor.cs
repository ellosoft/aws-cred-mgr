// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class OktaFactor
{
    public required string Id { get; set; }
    public required string FactorType { get; set; }
    public required string Provider { get; set; }
    public string? VendorName { get; set; }
    public Dictionary<string, object> Profile { get; set; } = new();
}
