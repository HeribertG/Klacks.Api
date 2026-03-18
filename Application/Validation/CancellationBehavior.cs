// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pipeline behavior that catches OperationCanceledException from cancelled HTTP requests.
/// Returns default response instead of throwing, to avoid flooding VS debug output
/// with harmless OperationCanceledException/PostgreSQL 57014 on client-side navigation.
/// </summary>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Validation;

public class CancellationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return default!;
        }
    }
}
