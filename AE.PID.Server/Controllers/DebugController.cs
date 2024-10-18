using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[ApiVersion(3)]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
public class DebugController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(PDMSApiResolver.CreateHeader());
    }
}