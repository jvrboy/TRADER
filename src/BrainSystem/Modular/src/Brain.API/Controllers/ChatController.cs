using Brain.API.Models;
using Brain.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Brain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OrchestrationService _orchestrator;

    public ChatController(OrchestrationService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// POST /api/chat - Sends a user message; the system responds using LLM + tools + memory.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
            return BadRequest(new { error = "Message is required" });

        try
        {
            var result = await _orchestrator.Chat(request.Message, request.SessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
