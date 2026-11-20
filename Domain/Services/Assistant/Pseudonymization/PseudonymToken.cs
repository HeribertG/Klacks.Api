// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The canonical text form of a pseudonym: prefix, separator, at least four digits and an optional variant suffix,
/// e.g. MA-0017 or MA-0017-N.
/// </summary>
/// <param name="Kind">What the pseudonym stands for</param>
/// <param name="Ordinal">The stable number of the installation</param>
/// <param name="Variant">The name part (N, V, ...), or null for the full name or a value</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public readonly record struct PseudonymToken(PseudonymKind Kind, int Ordinal, string? Variant = null)
{
    public string Base =>
        PseudonymPrefixes.Of(Kind) + PseudonymizationConstants.TokenSeparator +
        Ordinal.ToString(new string('0', PseudonymizationConstants.TokenDigits), CultureInfo.InvariantCulture);

    public override string ToString() =>
        string.IsNullOrEmpty(Variant) ? Base : Base + PseudonymizationConstants.TokenSeparator + Variant;
}
