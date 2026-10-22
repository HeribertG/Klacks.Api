// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Model-facing texts shared by the by-name resolvers. A suggested name is never acted on: the
/// resolver returns this text as its error, so the skill changes nothing and the model has to ask
/// the user before it may call again with the exact name.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Skills;

internal static class NameResolutionMessages
{
    public const string SuggestedNameTemplate =
        "Did the user mean '{0}'? Ask the user to confirm; call again with the exact name only after they confirm. " +
        "Nothing was changed.";

    public static string SuggestedName(string name) =>
        string.Format(CultureInfo.InvariantCulture, SuggestedNameTemplate, name) + " " + NameResolutionAskBack.Marker;
}
