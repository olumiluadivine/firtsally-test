using domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace application.Common;

/// <summary>
/// Helper class for manual validation in command handlers
/// </summary>
public class ValidationHelper
{
    /// <summary>
    /// Validates a request manually and returns ApiResponse if validation fails
    /// </summary>
    /// <typeparam name="T">Request type</typeparam>
    /// <typeparam name="TResult">Expected result type</typeparam>
    /// <param name="validator">FluentValidation validator</param>
    /// <param name="request">Request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ApiResponse with validation errors if validation fails, null if valid</returns>
    public static async Task<ApiResponse<TResult>?> ValidateAsync<T, TResult>(
        IValidator<T> validator, 
        T request, 
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        
        if (validationResult.IsValid)
            return null;

        return validationResult.ToDetailedApiResponse<TResult>();
    }

    /// <summary>
    /// Validates a request and throws ValidationException if validation fails
    /// </summary>
    /// <typeparam name="T">Request type</typeparam>
    /// <param name="validator">FluentValidation validator</param>
    /// <param name="request">Request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task ValidateAndThrowAsync<T>(
        IValidator<T> validator, 
        T request, 
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }

    /// <summary>
    /// Validates multiple objects and returns combined validation result
    /// </summary>
    public static async Task<ValidationResult> ValidateMultipleAsync<T1, T2>(
        IValidator<T1> validator1, T1 request1,
        IValidator<T2> validator2, T2 request2,
        CancellationToken cancellationToken = default)
    {
        var result1 = await validator1.ValidateAsync(request1, cancellationToken);
        var result2 = await validator2.ValidateAsync(request2, cancellationToken);

        var combinedResult = new ValidationResult();
        
        foreach (var error in result1.Errors.Concat(result2.Errors))
        {
            combinedResult.Errors.Add(error);
        }

        return combinedResult;
    }

    /// <summary>
    /// Creates a custom validation error
    /// </summary>
    public static ValidationFailure CreateError(string propertyName, string errorMessage, object? attemptedValue = null)
    {
        return new ValidationFailure(propertyName, errorMessage)
        {
            AttemptedValue = attemptedValue
        };
    }

    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    public static ValidationResult CombineResults(params ValidationResult[] results)
    {
        var combinedResult = new ValidationResult();
        
        foreach (var result in results)
        {
            foreach (var error in result.Errors)
            {
                combinedResult.Errors.Add(error);
            }
        }

        return combinedResult;
    }
}