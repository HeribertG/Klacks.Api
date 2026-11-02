// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// An accept was requested for a scenario that carries at least one variant key that is no longer Active, because an
/// accepted scenario has already consumed it. Refused as a 409 with its own code and the key names, so a client can tell it
/// apart from every other conflict; neither overrideStale nor overrideBlock lifts it.
/// </summary>
/// <param name="variantNames">Names of the consumed keys the scenario still carries</param>
public sealed class ScenarioVariantConsumedException : ConflictException
{
    public const string ErrorCode = ScenarioVariantErrorCodes.Consumed;

    public const string VariantNamesField = "variantNames";

    public ScenarioVariantConsumedException(IReadOnlyList<string> variantNames)
        : base(BuildMessage(variantNames), ErrorCode, BuildDetails(variantNames))
    {
        VariantNames = variantNames;
    }

    public IReadOnlyList<string> VariantNames { get; }

    private static string BuildMessage(IReadOnlyList<string> variantNames)
        => string.Format(CultureInfo.InvariantCulture, ScenarioAcceptDefaults.VariantConsumedMessageFormat, string.Join(", ", variantNames));

    private static IReadOnlyDictionary<string, object?> BuildDetails(IReadOnlyList<string> variantNames)
        => new Dictionary<string, object?>
        {
            [VariantNamesField] = variantNames,
        };
}
