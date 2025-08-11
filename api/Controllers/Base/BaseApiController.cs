using domain.Common;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace api.Controllers.Base;

/// <summary>
/// Base controller with standardized response methods
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Returns a standardized success response
    /// </summary>
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Operation completed successfully")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    /// <summary>
    /// Returns a standardized created response
    /// </summary>
    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string message = "Resource created successfully")
    {
        var response = ApiResponse<T>.CreatedResponse(data, message);
        response.CorrelationId = GetCorrelationId();
        return StatusCode((int)HttpStatusCode.Created, response);
    }

    /// <summary>
    /// Returns a standardized not found response
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string message = "Resource not found")
    {
        var response = ApiResponse<T>.NotFoundResponse(message);
        response.CorrelationId = GetCorrelationId();
        return NotFound(response);
    }

    /// <summary>
    /// Returns a standardized bad request response
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequestResponse<T>(string message = "Bad request", IEnumerable<string>? errors = null)
    {
        var response = ApiResponse<T>.BadRequestResponse(message, errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }

    /// <summary>
    /// Returns a standardized unauthorized response
    /// </summary>
    protected ActionResult<ApiResponse<T>> UnauthorizedResponse<T>(string message = "Unauthorized access")
    {
        var response = ApiResponse<T>.UnauthorizedResponse(message);
        response.CorrelationId = GetCorrelationId();
        return Unauthorized(response);
    }

    /// <summary>
    /// Returns a standardized internal server error response
    /// </summary>
    protected ActionResult<ApiResponse<T>> InternalServerErrorResponse<T>(string message = "An internal server error occurred")
    {
        var response = ApiResponse<T>.InternalServerErrorResponse(message);
        response.CorrelationId = GetCorrelationId();
        return StatusCode((int)HttpStatusCode.InternalServerError, response);
    }

    /// <summary>
    /// Returns a standardized validation error response
    /// </summary>
    protected ActionResult<ApiResponse<T>> ValidationErrorResponse<T>(IEnumerable<string> errors)
    {
        var response = ApiResponse<T>.ValidationErrorResponse(errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }

    /// <summary>
    /// Returns a standardized paginated response
    /// </summary>
    protected ActionResult<PaginatedApiResponse<T>> PaginatedResponse<T>(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalCount,
        string message = "Data retrieved successfully")
    {
        var response = PaginatedApiResponse<T>.SuccessResponse(data, pageNumber, pageSize, totalCount, message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    /// <summary>
    /// Non-generic success response for operations that don't return data
    /// </summary>
    protected ActionResult<ApiResponse> SuccessResponse(string message = "Operation completed successfully")
    {
        var response = ApiResponse.SuccessResponse(message);
        response.CorrelationId = GetCorrelationId();
        return Ok(response);
    }

    /// <summary>
    /// Non-generic created response
    /// </summary>
    protected ActionResult<ApiResponse> CreatedResponse(string message = "Resource created successfully")
    {
        var response = ApiResponse.CreatedResponse(message);
        response.CorrelationId = GetCorrelationId();
        return StatusCode((int)HttpStatusCode.Created, response);
    }

    /// <summary>
    /// Non-generic not found response
    /// </summary>
    protected ActionResult<ApiResponse> NotFoundResponse(string message = "Resource not found")
    {
        var response = ApiResponse.NotFoundResponse(message);
        response.CorrelationId = GetCorrelationId();
        return NotFound(response);
    }

    /// <summary>
    /// Non-generic bad request response
    /// </summary>
    protected ActionResult<ApiResponse> BadRequestResponse(string message = "Bad request", IEnumerable<string>? errors = null)
    {
        var response = ApiResponse.BadRequestResponse(message, errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }

    /// <summary>
    /// Non-generic unauthorized response
    /// </summary>
    protected ActionResult<ApiResponse> UnauthorizedResponse(string message = "Unauthorized access")
    {
        var response = ApiResponse.UnauthorizedResponse(message);
        response.CorrelationId = GetCorrelationId();
        return Unauthorized(response);
    }

    /// <summary>
    /// Non-generic internal server error response
    /// </summary>
    protected ActionResult<ApiResponse> InternalServerErrorResponse(string message = "An internal server error occurred")
    {
        var response = ApiResponse.InternalServerErrorResponse(message);
        response.CorrelationId = GetCorrelationId();
        return StatusCode((int)HttpStatusCode.InternalServerError, response);
    }

    /// <summary>
    /// Non-generic validation error response
    /// </summary>
    protected ActionResult<ApiResponse> ValidationErrorResponse(IEnumerable<string> errors)
    {
        var response = ApiResponse.ValidationErrorResponse(errors);
        response.CorrelationId = GetCorrelationId();
        return BadRequest(response);
    }

    /// <summary>
    /// Gets correlation ID from request headers or generates a new one
    /// </summary>
    private string GetCorrelationId()
    {
        return HttpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId)
            ? correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();
    }
}