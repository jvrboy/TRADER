using Brain.API.Models;
using Brain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Brain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainController : ControllerBase
{
    private readonly OrchestrationService _orchestrator;

    public TrainController(OrchestrationService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// POST /api/train - Triggers training on drift indices 10, 20, 30.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TrainResponse>> Train([FromBody] TrainRequest request)
    {
        try
        {
            var result = await _orchestrator.Train(request.DriftIndices, request.Epochs, request.LearningRate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
