// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the "names" parameter of an aggregated proactive summary: the first MaxListedNames names
/// joined by a separator, plus a "+n" tail counting whoever did not fit. The cap is not cosmetic -
/// AgentTriggerService drops ContentParamsJson entirely once the serialized parameters exceed
/// ProactiveTriggerDispatchLimits.ContentParamsJsonMaxLength, so an uncapped list would cost the inbox
/// row every one of its parameters rather than just the surplus names.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public static class ProactiveNameListRenderer
{
    public const int MaxListedNames = 10;

    private const string NameSeparator = ", ";
    private const string OverflowFormat = "{0} +{1}";

    /// <summary>
    /// Renders the capped, comma-separated name list with its overflow tail.
    /// </summary>
    /// <param name="clientNames">Display names of every affected employee, in the order to list them.</param>
    public static string Render(IReadOnlyList<string> clientNames)
    {
        var listed = string.Join(NameSeparator, clientNames.Take(MaxListedNames));

        return clientNames.Count <= MaxListedNames
            ? listed
            : string.Format(
                CultureInfo.InvariantCulture, OverflowFormat, listed, clientNames.Count - MaxListedNames);
    }
}
