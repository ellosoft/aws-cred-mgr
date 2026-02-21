// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Text;
using Ellosoft.AwsCredentialsManager.Services.AWS;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.AWS;

public class AwsSamlServiceTests
{
    private readonly AwsSamlService _samlService = new();

    private static string CreateEncodedSamlAssertion(params string[] roleIdpPairs)
    {
        var attributes = string.Join(Environment.NewLine,
            roleIdpPairs.Select(pair =>
                $"        <saml:AttributeValue>{pair}</saml:AttributeValue>"));

        var xml = $"""
                   <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol">
                     <saml:Assertion xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">
                       <saml:AttributeStatement>
                         <saml:Attribute Name="https://aws.amazon.com/SAML/Attributes/Role">
                   {attributes}
                         </saml:Attribute>
                       </saml:AttributeStatement>
                     </saml:Assertion>
                   </samlp:Response>
                   """;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
    }

    [Fact]
    public void GetAwsRolesAndIdpFromSamlAssertion_SingleRole_ShouldReturnOneEntry()
    {
        var encoded = CreateEncodedSamlAssertion(
            "arn:aws:iam::123:saml-provider/okta,arn:aws:iam::123:role/TestRole");

        var result = _samlService.GetAwsRolesAndIdpFromSamlAssertion(encoded);

        result.Should().HaveCount(1);
        result.Should().ContainKey("arn:aws:iam::123:role/TestRole");
        result["arn:aws:iam::123:role/TestRole"].Should().Be("arn:aws:iam::123:saml-provider/okta");
    }

    [Fact]
    public void GetAwsRolesAndIdpFromSamlAssertion_MultipleRoles_ShouldReturnAllEntries()
    {
        var encoded = CreateEncodedSamlAssertion(
            "arn:aws:iam::123:saml-provider/okta,arn:aws:iam::123:role/RoleA",
            "arn:aws:iam::456:saml-provider/okta,arn:aws:iam::456:role/RoleB");

        var result = _samlService.GetAwsRolesAndIdpFromSamlAssertion(encoded);

        result.Should().HaveCount(2);
        result.Should().ContainKey("arn:aws:iam::123:role/RoleA");
        result.Should().ContainKey("arn:aws:iam::456:role/RoleB");
        result["arn:aws:iam::123:role/RoleA"].Should().Be("arn:aws:iam::123:saml-provider/okta");
        result["arn:aws:iam::456:role/RoleB"].Should().Be("arn:aws:iam::456:saml-provider/okta");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetAwsRolesAndIdpFromSamlAssertion_NullOrEmpty_ShouldThrow(string? assertion)
    {
        var act = () => _samlService.GetAwsRolesAndIdpFromSamlAssertion(assertion!);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetAwsRolesAndIdpFromSamlAssertion_NoRoleAttributes_ShouldReturnEmptyDictionary()
    {
        var xml = """
                  <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol">
                    <saml:Assertion xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">
                      <saml:AttributeStatement>
                        <saml:Attribute Name="https://aws.amazon.com/SAML/Attributes/SessionDuration">
                          <saml:AttributeValue>3600</saml:AttributeValue>
                        </saml:Attribute>
                      </saml:AttributeStatement>
                    </saml:Assertion>
                  </samlp:Response>
                  """;

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));

        var result = _samlService.GetAwsRolesAndIdpFromSamlAssertion(encoded);

        result.Should().BeEmpty();
    }
}
