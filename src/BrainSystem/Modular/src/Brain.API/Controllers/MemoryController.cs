using Brain.API.Models;
using Brain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Brain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemoryController : ControllerBase
{
    private readonly OrchestrationService _orchestrator;

    public MemoryController(OrchestrationService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// GET /api/memory/query?text=... - Retrieves relevant long-term memories.
    /// </summary>
    [HttpGet("query")]
    public ActionResult<MemoryQueryResponse> Query([FromQuery] string text)
    {
        if (string.IsNullOrEmpty(text))
            return BadRequest(new { error = "Query text is required" });

        try
        {
            var result = _orchestrator.QueryMemory(text);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
