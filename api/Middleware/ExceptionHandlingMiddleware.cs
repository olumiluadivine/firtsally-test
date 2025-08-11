using domain.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse response;

        switch (exception)
        {
            case ValidationException validationEx:
                response = CreateDetailedValidationResponse(validationEx);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidOperationException invalidOpEx:
                response = ApiResponse.BadRequestResponse(invalidOpEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response = ApiResponse.UnauthorizedResponse(unauthorizedEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ArgumentException argEx:
                response = ApiResponse.BadRequestResponse(argEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case KeyNotFoundException keyNotFoundEx:
                response = ApiResponse.NotFoundResponse(keyNotFoundEx.Message);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            default:
                response = ApiResponse.InternalServerErrorResponse("An error occurred while processing your request");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            response.CorrelationId = correlationId.FirstOrDefault();
        }
        else
        {
            response.CorrelationId = Guid.NewGuid().ToString();
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ApiResponse CreateDetailedValidationResponse(ValidationException validationEx)
    {
        var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
        var response = ApiResponse.ValidationErrorResponse(errors);

        // Check if detailed validation info was added to exception data
        if (validationEx.Data.Contains("ValidationDetails"))
        {
            var validationDetails = validationEx.Data["ValidationDetails"];
            var requestType = validationEx.Data["RequestType"]?.ToString();
            var validationCount = validationEx.Data["ValidationCount"];

            response.Metadata = new Dictionary<string, object>
            {
                ["ValidationDetails"] = validationDetails!,
                ["RequestType"] = requestType ?? "Unknown",
                ["ValidationCount"] = validationCount ?? errors.Count,
                ["GroupedErrors"] = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => new
                        {
                            Message = e.ErrorMessage,
                            AttemptedValue = e.AttemptedValue?.ToString(),
                            ErrorCode = e.ErrorCode,
                            Severity = e.Severity.ToString()
                        }).ToList()
                    )
            };
        }

        return response;
    }
}