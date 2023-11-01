// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text;
using System.Xml;
using AngleSharp.Html.Parser;
using Ellosoft.AwsCredentialsManager.Services.Okta;

namespace Ellosoft.AwsCredentialsManager.Services.AWS;

public record AwsRole(string RoleName, string AccountName);

public class AwsSamlService
{
    /// <summary>
    ///     Extracts AWS roles and IDP from an encoded SAML assertion and
    ///     returns a dictionary with the key being the role ARN and the value being the IDP
    /// </summary>
    /// <param name="encodedSamlAssertion">Encoded SAML assertion</param>
    /// <returns>Dictionary with AWS role ARN as the key and the IDP as the value</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Dictionary<string, string> GetAwsRolesAndIdpFromSamlAssertion(string encodedSamlAssertion)
    {
        var samlAssertion = Encoding.UTF8.GetString(Convert.FromBase64String(encodedSamlAssertion));

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(samlAssertion);

        var roleNodes = xmlDocument.SelectNodes(
            "//*[local-name()='Attribute'][@Name='https://aws.amazon.com/SAML/Attributes/Role']/*[local-name()='AttributeValue']");

        if (roleNodes is null)
            throw new InvalidOperationException("Failed to parse SAML assertion");

        return roleNodes
            .Cast<XmlNode>()
            .Select(roleNode => roleNode.InnerText.Split(','))
            .ToDictionary(roleInfo => roleInfo[1], roleInfo => roleInfo[0]);
    }

    /// <summary>
    ///     Retrieves the AWS account names and roles for the current user
    ///     based on the provided SAML data.
    /// </summary>
    /// <param name="samlData">The SAML data containing the SAML assertion and sign-in URL</param>
    /// <returns>A list of <see cref="AwsRole">AWS roles</see></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<ICollection<AwsRole>> GetAwsRolesWithAccountName(SamlData samlData)
    {
        if (samlData.SamlAssertion is null || samlData.SignInUrl is null)
            throw new InvalidOperationException("Invalid SAML assertion or Sign-in URL");

        using var httpClient = new HttpClient();
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["RelayState"] = samlData.RelayState,
            ["SAMLResponse"] = samlData.SamlAssertion
        });

        using var response = await httpClient.PostAsync(samlData.SignInUrl, formContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        return ExtractAwsAccountNamesAndRolesFromSignInResponse(responseContent);
    }

    private static List<AwsRole> ExtractAwsAccountNamesAndRolesFromSignInResponse(string samlSignInResponse)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(samlSignInResponse);

        var awsAccountsElements = document.QuerySelectorAll("fieldset > div.saml-account");

        var awsRoles = new List<AwsRole>();

        foreach (var awsAccountsElement in awsAccountsElements)
        {
            var accountName = awsAccountsElement.QuerySelector("div.saml-account-name")?.TextContent;
            var roleArn = awsAccountsElement.QuerySelector("div.saml-role > input[name='roleIndex']")
                ?.GetAttribute("value");

            if (roleArn is not null && accountName is not null)
            {
                awsRoles.Add(new AwsRole(roleArn, accountName));
            }
        }

        return awsRoles;
    }
}
