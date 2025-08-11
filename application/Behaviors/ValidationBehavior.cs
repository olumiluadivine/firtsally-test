using domain.Common;
using FluentValidation;
using MediatR;

namespace application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                // Enhanced error information with property details
                var validationErrors = failures.Select(f => new
                {
                    Property = f.PropertyName,
                    Error = f.ErrorMessage,
                    AttemptedValue = f.AttemptedValue?.ToString(),
                    ErrorCode = f.ErrorCode,
                    Severity = f.Severity.ToString()
                }).ToList();

                // Create a detailed validation exception with structured error info
                var exception = new ValidationException(failures);
                exception.Data.Add("ValidationDetails", validationErrors);
                exception.Data.Add("RequestType", typeof(TRequest).Name);
                exception.Data.Add("ValidationCount", failures.Count);
                
                throw exception;
            }
        }

        return await next();
    }
}