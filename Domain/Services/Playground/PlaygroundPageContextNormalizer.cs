// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cuts the page context a Playground visitor sends down to a sane size before it can reach a provider call. Route, ids,
/// dates and entity type are rendered unabridged into the system prompt, so an unbounded value would inflate every turn
/// and let a single request reserve a huge share of the daily budget. Text fields are cut at
/// PlaygroundMaxContextFieldChars and the client multi-selection at PlaygroundMaxSelectedClientIds entries.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Playground;

public static class PlaygroundPageContextNormalizer
{
    /// <summary>
    /// Shortens the over-long fields of the context in place; a null context is left alone.
    /// </summary>
    /// <param name="pageContext">Page context taken from the request body</param>
    public static void Normalize(AssistantPageContext? pageContext)
    {
        if (pageContext is null)
        {
            return;
        }

        pageContext.CurrentRoute = Cut(pageContext.CurrentRoute);
        pageContext.SelectedGroupId = Cut(pageContext.SelectedGroupId);
        pageContext.SelectedPeriodFrom = Cut(pageContext.SelectedPeriodFrom);
        pageContext.SelectedPeriodUntil = Cut(pageContext.SelectedPeriodUntil);
        pageContext.SelectedClientId = Cut(pageContext.SelectedClientId);
        pageContext.SelectedEntityType = Cut(pageContext.SelectedEntityType);

        if (pageContext.SelectedClientIds is { } ids)
        {
            pageContext.SelectedClientIds = ids
                .Take(PlaygroundHardeningConstants.PlaygroundMaxSelectedClientIds)
                .Select(id => Cut(id) ?? string.Empty)
                .ToList();
        }
    }

    private static string? Cut(string? value) =>
        value is not null && value.Length > PlaygroundHardeningConstants.PlaygroundMaxContextFieldChars
            ? value[..PlaygroundHardeningConstants.PlaygroundMaxContextFieldChars]
            : value;
}
