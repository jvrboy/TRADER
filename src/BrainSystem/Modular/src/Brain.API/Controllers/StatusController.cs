using Brain.API.Models;
using Brain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Brain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly OrchestrationService _orchestrator;

    public StatusController(OrchestrationService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// GET /api/status - Returns health and model loading status.
    /// </summary>
    [HttpGet]
    public ActionResult<StatusResponse> Status()
    {
        return Ok(_orchestrator.GetStatus());
    }
}
