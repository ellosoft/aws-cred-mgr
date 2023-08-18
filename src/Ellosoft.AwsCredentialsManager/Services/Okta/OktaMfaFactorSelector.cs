// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public class OktaMfaFactorSelector
{
    public Factor GetMfaFactor(string? preferredMfaType, IEnumerable<Factor> factors)
    {
        var factorOptions = factors.Select(GetUserFriendlyMfaFactor);

        var factorSelectionMessage = preferredMfaType is not null
            ? $"[yellow]Preferred MFA factor '{preferredMfaType}' not found in your profile, please select one of the following options:[/]"
            : "[yellow]Please select one of the following MFA options:[/]";

        var selectedFactor = AnsiConsole.Prompt(
            new SelectionPrompt<UserFriendlyFactor>()
                .Title(factorSelectionMessage)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(f => f.Name)
                .AddChoices(factorOptions));

        return selectedFactor.Factor;
    }

    private static UserFriendlyFactor GetUserFriendlyMfaFactor(Factor factor)
    {
        return factor.Type switch
        {
            "push" => new UserFriendlyFactor("Okta Verify (Push)", factor),
            "token:software:totp" => new UserFriendlyFactor("Okta Verify (TOTP Code)", factor),
            _ => new UserFriendlyFactor($"[grey]{factor.Type} (Unsupported)[/]", factor)
        };
    }

    private sealed record UserFriendlyFactor(string Name, Factor Factor);
}
