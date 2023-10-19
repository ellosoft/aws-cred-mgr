// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

public interface IOktaMfaFactorSelector
{
    OktaFactor GetMfaFactor(string? preferredMfaType, IEnumerable<OktaFactor> factors);
}

public class OktaMfaFactorSelector : IOktaMfaFactorSelector
{
    public OktaFactor GetMfaFactor(string? preferredMfaType, IEnumerable<OktaFactor> factors)
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

    public static string GetOktaMfaFactorCode(string simplifiedMfaName)
    {
        return simplifiedMfaName switch
        {
            "push" => "push",
            "totp" => "token:software:totp",
            "code" => "token:software:totp",
            _ => throw new NotSupportedException($"MFA type '{simplifiedMfaName}' is not yet supported")
        };
    }

    private static UserFriendlyFactor GetUserFriendlyMfaFactor(OktaFactor factor)
    {
        return factor.FactorType switch
        {
            "push" => new UserFriendlyFactor("Okta Verify (Push)", factor),
            "token:software:totp" => new UserFriendlyFactor("Okta Verify (TOTP Code)", factor),
            _ => new UserFriendlyFactor($"[grey]{factor.FactorType} (Unsupported)[/]", factor)
        };
    }

    private sealed record UserFriendlyFactor(string Name, OktaFactor Factor);
}
