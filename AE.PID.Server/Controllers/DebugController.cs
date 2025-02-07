using System.Text.Json;
using System.Text.Json.Serialization;
using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using AE.PID.Server.PDMS;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[ApiVersion(3)]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
public class DebugController(ILogger<DebugController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(PDMSApiResolver.CreateHeader());
    }

}