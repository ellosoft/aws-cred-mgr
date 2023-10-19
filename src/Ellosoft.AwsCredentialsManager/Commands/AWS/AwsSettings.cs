// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Globalization;
using Amazon;

namespace Ellosoft.AwsCredentialsManager.Commands.AWS;

public class AwsSettings : CommonSettings
{
    [CommandOption("-r|--region <REGION>")]
    [Description("Sets or overrides the AWS region (e.g. us-east-2)")]
    [TypeConverter(typeof(AwsRegionConverter))]
    public RegionEndpoint? Region { get; set; }

    [CommandOption("--okta-profile <OKTA_PROFILE>")]
    [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
    [DefaultValue("default")]
    public virtual string OktaUserProfile { get; set; } = "default";

    public class AwsRegionConverter : TypeConverter
    {
        private const string INVALID_REGION = "Invalid AWS region";

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string regionStringValue)
            {
                throw new NotSupportedException(INVALID_REGION);
            }

            var region = RegionEndpoint.GetBySystemName(regionStringValue);

            return region ?? throw new NotSupportedException(INVALID_REGION);
        }
    }
}
