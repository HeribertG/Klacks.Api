// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerType = HandlerTypeCache.GetOrAdd(
            requestType,
            t => typeof(IRequestHandler<,>).MakeGenericType(t, responseType));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var behaviors = GetPipelineBehaviors<TResponse>(requestType);

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handleMethod = handlerType.GetMethod("Handle")!;
            return (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var currentDelegate = handlerDelegate;
            var currentBehavior = behavior;
            handlerDelegate = () => InvokeBehavior(currentBehavior, request, currentDelegate, cancellationToken);
        }

        return await handlerDelegate();
    }

    private List<object> GetPipelineBehaviors<TResponse>(Type requestType)
    {
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
        var behaviors = _serviceProvider.GetService(enumerableType) as IEnumerable<object>;
        return behaviors?.Where(b => b != null).ToList() ?? [];
    }

    private static Task<TResponse> InvokeBehavior<TResponse>(
        object behavior,
        object request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var behaviorType = behavior.GetType();
        var handleMethod = behaviorType.GetMethod("Handle")!;
        return (Task<TResponse>)handleMethod.Invoke(behavior, [request, next, cancellationToken])!;
    }
}
