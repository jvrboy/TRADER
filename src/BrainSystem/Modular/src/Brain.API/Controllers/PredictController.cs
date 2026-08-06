using Brain.API.Models;
using Brain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Brain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictController : ControllerBase
{
    private readonly OrchestrationService _orchestrator;

    public PredictController(OrchestrationService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// GET /api/predict/{index} - Returns live prediction for drift switch index (10, 20, 30).
    /// </summary>
    [HttpGet("{index}")]
    public ActionResult<PredictResponse> Predict(int index, [FromQuery] string? features = null)
    {
        try
        {
            float[]? featureArray = null;
            if (!string.IsNullOrEmpty(features))
            {
                featureArray = features.Split(',')
                    .Select(f => float.TryParse(f.Trim(), out var v) ? v : 0f)
                    .ToArray();
            }

            var result = _orchestrator.Predict(index, featureArray);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
