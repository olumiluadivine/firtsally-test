using domain.Common;
using FluentValidation.Results;

namespace application.Common;

/// <summary>
/// Extension methods for FluentValidation integration with generic response structure
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts FluentValidation ValidationResult to ApiResponse
    /// </summary>
    public static ApiResponse<T> ToApiResponse<T>(this ValidationResult validationResult, string message = "Validation failed")
    {
        if (validationResult.IsValid)
        {
            return ApiResponse<T>.SuccessResponse(default(T)!, "Validation passed");
        }

        var errors = validationResult.Errors.Select(e => e.ErrorMessage);
        return ApiResponse<T>.ValidationErrorResponse(errors);
    }

    /// <summary>
    /// Converts FluentValidation ValidationResult to detailed ApiResponse with property information
    /// </summary>
    public static ApiResponse<T> ToDetailedApiResponse<T>(this ValidationResult validationResult, string message = "Validation failed")
    {
        if (validationResult.IsValid)
        {
            return ApiResponse<T>.SuccessResponse(default(T)!, "Validation passed");
        }

        var errors = validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
        var response = ApiResponse<T>.ValidationErrorResponse(errors);
        
        // Add detailed validation metadata
        response.Metadata = new Dictionary<string, object>
        {
            ["ValidationDetails"] = validationResult.Errors.Select(e => new
            {
                Property = e.PropertyName,
                Error = e.ErrorMessage,
                AttemptedValue = e.AttemptedValue?.ToString(),
                ErrorCode = e.ErrorCode,
                Severity = e.Severity.ToString()
            }).ToList(),
            ["ValidationSummary"] = new
            {
                TotalErrors = validationResult.Errors.Count,
                PropertiesWithErrors = validationResult.Errors.Select(e => e.PropertyName).Distinct().ToList()
            }
        };

        return response;
    }

    /// <summary>
    /// Gets grouped validation errors by property for structured responses
    /// </summary>
    public static Dictionary<string, List<string>> GetGroupedErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToList()
            );
    }

    /// <summary>
    /// Creates an ApiResponse with grouped validation errors
    /// </summary>
    public static ApiResponse<T> ToGroupedApiResponse<T>(this ValidationResult validationResult, string message = "Validation failed")
    {
        if (validationResult.IsValid)
        {
            return ApiResponse<T>.SuccessResponse(default(T)!, "Validation passed");
        }

        var groupedErrors = validationResult.GetGroupedErrors();
        var flatErrors = groupedErrors.SelectMany(kvp => 
            kvp.Value.Select(error => $"{kvp.Key}: {error}"));

        var response = ApiResponse<T>.ValidationErrorResponse(flatErrors);
        
        // Add grouped errors as metadata
        response.Metadata = new Dictionary<string, object>
        {
            ["GroupedErrors"] = groupedErrors,
            ["ErrorsByProperty"] = groupedErrors.ToDictionary(
                kvp => kvp.Key, 
                kvp => new { Count = kvp.Value.Count, Errors = kvp.Value }
            )
        };

        return response;
    }
}