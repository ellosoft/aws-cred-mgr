// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Commands.Okta;

[Name("setup")]
[Description("Setup Okta authentication")]
[Examples("setup")]
public class SetupOkta : AsyncCommand<CommonSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommonSettings settings)
    {
        await Task.Delay(1);
        //var oktaLoginService = new OktaLoginService();
        //await oktaLoginService.Login(DOMAIN, "default", preferredMfaType: null);

        return 0;
    }
}
