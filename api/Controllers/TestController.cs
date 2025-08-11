using api.Controllers.Base;
using domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
public class TestController : BaseApiController
{
    /// <summary>
    /// Simple test endpoint to verify API is working
    /// </summary>
    /// <returns>A simple test message</returns>
    [HttpGet]
    public ActionResult<ApiResponse<object>> GetTest()
    {
        var testData = new { 
            message = "Banking API is running!", 
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return SuccessResponse<object>(testData, "API is running successfully");
    }

    /// <summary>
    /// Test endpoint with parameter
    /// </summary>
    /// <param name="name">Name to include in response</param>
    /// <returns>Personalized greeting</returns>
    [HttpGet("hello/{name}")]
    public ActionResult<ApiResponse<object>> SayHello(string name)
    {
        var greetingData = new { 
            message = $"Hello {name}!", 
            timestamp = DateTime.UtcNow 
        };

        return SuccessResponse<object>(greetingData, $"Greeting sent to {name}");
    }

    /// <summary>
    /// Test endpoint for error handling
    /// </summary>
    [HttpGet("error")]
    public ActionResult<ApiResponse<object>> TestError()
    {
        return InternalServerErrorResponse<object>("This is a test error response");
    }

    /// <summary>
    /// Test endpoint for validation errors
    /// </summary>
    [HttpGet("validation-error")]
    public ActionResult<ApiResponse<object>> TestValidationError()
    {
        var errors = new[] { "Test validation error 1", "Test validation error 2" };
        return ValidationErrorResponse<object>(errors);
    }
}