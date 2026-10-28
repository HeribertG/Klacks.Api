// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the ProblemDetails answers (with an errorCode extension) that the Playground filters return, so every
/// refusal has one shape: blocked functions answer HTTP 403, an over-long prompt HTTP 400 and a used-up budget HTTP 429,
/// never 401 and never a redirect. The title is the standard reason phrase of the status.
/// </summary>

using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Klacks.Api.Presentation.Filters;

public static class PlaygroundProblemResultFactory
{
    /// <param name="status">HTTP status code of the answer</param>
    /// <param name="detail">Human-readable explanation</param>
    /// <param name="errorCode">Machine-readable code the client reacts to</param>
    public static ObjectResult Create(int status, string detail, string errorCode)
    {
        var problem = new ProblemDetails
        {
            Title = ReasonPhrases.GetReasonPhrase(status),
            Status = status,
            Detail = detail,
        };
        problem.Extensions[PlaygroundHardeningConstants.ErrorCodeExtension] = errorCode;

        return new ObjectResult(problem) { StatusCode = status };
    }

    public static ObjectResult FeatureBlocked() => Create(
        StatusCodes.Status403Forbidden,
        PlaygroundHardeningConstants.FeatureBlockedMessage,
        PlaygroundHardeningConstants.FeatureBlockedErrorCode);

    public static ObjectResult PromptTooLong() => Create(
        StatusCodes.Status400BadRequest,
        PlaygroundHardeningConstants.PromptTooLongMessage,
        PlaygroundHardeningConstants.PromptTooLongErrorCode);

    public static ObjectResult TokenLimitReached() => Create(
        StatusCodes.Status429TooManyRequests,
        PlaygroundTokenLimitConstants.LimitReachedMessage,
        PlaygroundTokenLimitConstants.ErrorCode);
}
