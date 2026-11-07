// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter of the creating endpoints marked with [IdempotentRequest]. Without the Idempotency-Key header the action
/// runs as before. With it, the request is keyed by authenticated user, method, path, key and a SHA-256 of the bound
/// action arguments (the request body as the action sees it; the filter runs after model binding, when the raw body is
/// already read): the first request runs the action and its successful (2xx, with body) answer is kept for
/// ResponseLifetime; a repeat while the first still runs waits for it, a repeat afterwards gets the kept answer, and in
/// both cases the action does not run again. The same key with another body is a new request. Failures, exceptions and
/// empty answers are not kept, so a repeat after them runs the action. A key that is not exactly one GUID is refused with
/// 400 before the action runs. A request without a user id claim, one whose arguments cannot be serialized for the hash,
/// or one the store has no room for, runs as without the header. A waiter whose own request is aborted stops waiting with OperationCanceledException.
/// </summary>
/// <param name="store">Instance-wide register of the running and answered keys</param>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace Klacks.Api.Presentation.Filters;

public sealed class IdempotentRequestFilter : IAsyncActionFilter
{
    private const char KeySeparator = '|';

    private readonly IdempotentRequestStore _store;

    public IdempotentRequestFilter(IdempotentRequestStore store)
    {
        _store = store;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(HttpHeaderNames.IdempotencyKey, out var values))
        {
            await next();
            return;
        }

        if (values.Count != 1
            || !Guid.TryParseExact(values[0], IdempotentRequestConstants.KeyFormat, out var idempotencyKey))
        {
            context.Result = InvalidKey();
            return;
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await next();
            return;
        }

        var argumentsHash = TryHashArguments(context.ActionArguments);
        if (argumentsHash is null)
        {
            await next();
            return;
        }

        var storeKey = string.Join(
            KeySeparator,
            userId,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            idempotencyKey.ToString(IdempotentRequestConstants.KeyFormat),
            argumentsHash);

        while (true)
        {
            var (isOwner, entry) = _store.Begin(userId, storeKey);
            if (entry is null)
            {
                await next();
                return;
            }

            if (isOwner)
            {
                await RunAsOwnerAsync(next, storeKey, entry);
                return;
            }

            var response = await entry.Task.WaitAsync(httpContext.RequestAborted);
            if (response is not null)
            {
                context.Result = response.ToResult();
                return;
            }
        }
    }

    private async Task RunAsOwnerAsync(
        ActionExecutionDelegate next,
        string storeKey,
        TaskCompletionSource<IdempotentResponse?> entry)
    {
        IdempotentResponse? response = null;
        try
        {
            response = Capture(await next());
        }
        finally
        {
            if (response is null)
            {
                _store.Abandon(storeKey, entry);
            }
            else
            {
                _store.Complete(entry, response);
            }
        }
    }

    private static string? TryHashArguments(IDictionary<string, object?> arguments)
    {
        var body = arguments.Where(argument => argument.Value is not CancellationToken);
        try
        {
            return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(body)));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static IdempotentResponse? Capture(ActionExecutedContext executed)
    {
        if (executed.Exception is not null || executed.Result is not ObjectResult { Value: not null } result)
        {
            return null;
        }

        var statusCode = result.StatusCode ?? StatusCodes.Status200OK;
        var isSuccess = statusCode >= StatusCodes.Status200OK && statusCode < StatusCodes.Status300MultipleChoices;
        return isSuccess ? new IdempotentResponse(statusCode, result.Value, result.DeclaredType) : null;
    }

    private static ObjectResult InvalidKey() => new(new ProblemDetails
    {
        Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
        Status = StatusCodes.Status400BadRequest,
        Detail = IdempotentRequestConstants.InvalidKeyMessage,
    })
    {
        StatusCode = StatusCodes.Status400BadRequest,
    };
}
