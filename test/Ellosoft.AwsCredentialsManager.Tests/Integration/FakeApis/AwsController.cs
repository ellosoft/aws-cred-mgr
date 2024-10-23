// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Microsoft.AspNetCore.Mvc;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.FakeApis;

[ApiController]
[Route("aws")]
public class AwsController : ControllerBase
{
    [HttpPost("saml")]
    public IActionResult ProcessSamlResponse()
    {
        return Ok(new
        {
            Roles = new[]
            {
                new { RoleName = "TestRole", AccountName = "Test Account" }
            }
        });
    }
}
