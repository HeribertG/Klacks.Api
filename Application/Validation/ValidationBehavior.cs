// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Validation;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorCount = _validators.Count();
        _logger.LogInformation("[ValidationBehavior] Request={RequestType}, ValidatorCount={Count}", typeof(TRequest).Name, validatorCount);

        if (_validators.Any())
        {
            var validationTasks = _validators.Select(v =>
            {
                _logger.LogInformation("[ValidationBehavior] Running validator: {ValidatorType}", v.GetType().Name);
                return v.ValidateAsync(request, cancellationToken);
            });
            var validationResults = await Task.WhenAll(validationTasks);

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            _logger.LogInformation("[ValidationBehavior] FailureCount={FailureCount}", failures.Count);

            if (failures.Any())
            {
                foreach (var failure in failures)
                {
                    _logger.LogWarning("[ValidationBehavior] FAILURE: Property={Property}, Error={Error}", failure.PropertyName, failure.ErrorMessage);
                }
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
