// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Globalization;
using Amazon;

namespace Ellosoft.AwsCredentialsManager.Commands.AWS;

public class AwsSettings : CommonSettings
{
    [CommandOption("--region")]
    [Description("Sets or overrides the AWS region (e.g. us-east-2)")]
    [TypeConverter(typeof(AwsRegionConverter))]
    public RegionEndpoint? Region { get; set; }

    /// <summary>
    ///     Get AWS region endpoint
    /// </summary>
    /// <remarks>If the region option (--region) is provided it return its value, otherwise prompts the user for a AWS region</remarks>
    /// <returns>AWS Region Endpoint</returns>
    public RegionEndpoint GetRegion() =>
        Region ??= AwsRegionConverter.GetRegionFromString(AnsiConsole.Ask<string>("Enter the AWS region (e.g. us-east-2):"));

    public class AwsRegionConverter : TypeConverter
    {
        private const string INVALID_REGION = "'{0}' is not a valid AWS region";

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string regionStringValue)
                throw new NotSupportedException(String.Format(INVALID_REGION, value));

            return GetRegionFromString(regionStringValue);
        }

        public static RegionEndpoint GetRegionFromString(string regionStringValue)
        {
            var region = RegionEndpoint.GetBySystemName(regionStringValue);

            if (region is null || region.DisplayName == "Unknown")
                throw new NotSupportedException(String.Format(INVALID_REGION, regionStringValue));

            return region;
        }
    }
}
