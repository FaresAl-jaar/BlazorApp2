using Microsoft.AspNetCore.Mvc;

namespace BlazorApp2.Controllers;

/// <summary>
/// Mock API Controller zum Testen ohne externen Server
/// </summary>
[ApiController]
[Route("api/mock")]
[IgnoreAntiforgeryToken]
public class MockApiController : ControllerBase
{
    private readonly ILogger<MockApiController> _logger;

    public MockApiController(ILogger<MockApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Mock endpoint that simulates the external Lobster API
    /// </summary>
    [HttpPost("documents/upload")]
    public IActionResult UploadDocument([FromBody] MockUploadRequest request)
    {
        _logger.LogInformation("Mock API: Received document upload request. ExternalId: {ExternalId}", request?.ExternalId);
        
        // Simulate some processing
        var success = !string.IsNullOrEmpty(request?.ExternalId);
        
        if (success)
        {
            return Ok(new
            {
                success = true,
                message = "Document received successfully (MOCK)",
                documentId = $"mock-{Guid.NewGuid():N}",
                receivedAt = DateTime.UtcNow
            });
        }
        
        return BadRequest(new
        {
            success = false,
            message = "ExternalId is required"
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

public class MockUploadRequest
{
    public string? ExternalId { get; set; }
    public string? Data { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
