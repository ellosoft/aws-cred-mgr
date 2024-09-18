// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using Microsoft.AspNetCore.Mvc;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.FakeApis;

[ApiController]
[Route("/api/v1/")]
public class OktaController : ControllerBase
{
    [HttpPost("authn")]
    public ActionResult<AuthenticationResponse> Authenticate([FromBody] AuthenticationRequest request)
    {
        return Ok(new AuthenticationResponse
        {
            Status = "SUCCESS",
            SessionToken = "session_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        });
    }

    [HttpGet("users/me/appLinks")]
    public IActionResult GetAppLinks()
    {
        return Ok(new
        {
            Links = new[]
            {
                new
                {
                    Label = "amazon_aws",
                    LinkUrl = "https://xyz.okta.com/home/amazon_aws/abc/272"
                }
            }
        });
    }
}
