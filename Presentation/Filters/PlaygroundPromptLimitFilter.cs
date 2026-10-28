// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter that caps the free text a Playground visitor can send to the LLM endpoints: the chat message, the
/// transcription text to clean up and the string parameters of a direct skill call. Text longer than
/// PlaygroundMaxPromptChars is answered with HTTP 400 and errorCode PLAYGROUND_PROMPT_TOO_LONG before the action
/// (and so any provider call) runs; the page context of the request is cut to PlaygroundMaxContextFieldChars per field. With the Playground mode off it does nothing at all.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Services.Playground;
using Klacks.Api.Presentation.DTOs.Assistant;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class PlaygroundPromptLimitFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;

    public PlaygroundPromptLimitFilter(IPlaygroundMode mode)
    {
        _mode = mode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_mode.IsActive)
        {
            if (context.ActionArguments.Values.Any(ExceedsLimit))
            {
                context.Result = PlaygroundProblemResultFactory.PromptTooLong();
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                NormalizePageContext(argument);
            }
        }

        await next();
    }

    private static void NormalizePageContext(object? argument)
    {
        switch (argument)
        {
            case LLMRequest chat:
                PlaygroundPageContextNormalizer.Normalize(chat.PageContext);
                break;
            case LLMFunctionExecuteRequest function:
                PlaygroundPageContextNormalizer.Normalize(function.PageContext);
                break;
            case IEnumerable<LLMFunctionExecuteRequest> batch:
                foreach (var function in batch)
                {
                    PlaygroundPageContextNormalizer.Normalize(function.PageContext);
                }

                break;
        }
    }

    private static bool ExceedsLimit(object? argument) => argument switch
    {
        LLMRequest chat => IsTooLong(chat.Message),
        TranscriptionEnhanceRequest enhance => IsTooLong(enhance.RawText),
        LLMFunctionExecuteRequest function => HasTooLongParameter(function),
        IEnumerable<LLMFunctionExecuteRequest> batch => batch.Any(HasTooLongParameter),
        _ => false,
    };

    private static bool HasTooLongParameter(LLMFunctionExecuteRequest request) =>
        request.Parameters?.Values.Any(value => IsTooLong(AsText(value))) == true;

    private static string? AsText(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null,
    };

    private static bool IsTooLong(string? text) =>
        text is not null && text.Length > PlaygroundHardeningConstants.PlaygroundMaxPromptChars;
}
