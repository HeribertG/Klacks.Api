// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a free-text geocoding search: the query must carry 2 to 200 characters after trimming, the optional
/// language must be a BCP 47 tag of at most 35 characters (forwarded to the geocoder as accept-language).
/// </summary>

using System.Text.RegularExpressions;
using FluentValidation;
using Klacks.Api.Application.Queries.Geocoding;

namespace Klacks.Api.Application.Validation.Geocoding;

public class SearchGeocodingQueryValidator : AbstractValidator<SearchGeocodingQuery>
{
    public const int MinimumQueryLength = 2;
    public const int MaximumQueryLength = 200;
    public const int MaximumLanguageLength = 35;

    private static readonly Regex LanguageTagPattern = new(
        "^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SearchGeocodingQueryValidator()
    {
        RuleFor(query => query.Query)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A search query is required.")
            .Must(HaveAllowedLength)
            .WithMessage($"The search query must contain {MinimumQueryLength} to {MaximumQueryLength} characters.");

        RuleFor(query => query.Language)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(MaximumLanguageLength)
            .WithMessage($"The language must not exceed {MaximumLanguageLength} characters.")
            .Must(language => LanguageTagPattern.IsMatch(language!.Trim()))
            .WithMessage("The language must be a BCP 47 language tag.")
            .When(query => !string.IsNullOrWhiteSpace(query.Language));
    }

    private static bool HaveAllowedLength(string query)
    {
        var length = query.Trim().Length;
        return length >= MinimumQueryLength && length <= MaximumQueryLength;
    }
}
