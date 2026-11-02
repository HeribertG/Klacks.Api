// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the Language field of the order export filters. Only the shape of a culture tag
/// (e.g. de, en-US, zh-Hant-TW) is checked; unknown but well-formed codes are tolerated and
/// fall back to the invariant culture in the formatters.
/// </summary>
using FluentValidation;
using Klacks.Api.Application.Queries.Exports;

namespace Klacks.Api.Application.Validation.Exports;

internal static class ExportLanguageRules
{
    public const int MaxLanguageLength = 35;
    public const string LanguageTagPattern = "^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$";
}

public class CreateOrderExportQueryValidator : AbstractValidator<CreateOrderExportQuery>
{
    public CreateOrderExportQueryValidator()
    {
        RuleFor(query => query.Filter.Language)
            .NotEmpty()
            .MaximumLength(ExportLanguageRules.MaxLanguageLength)
            .Matches(ExportLanguageRules.LanguageTagPattern)
            .When(query => query.Filter != null);
    }
}

public class CreateOrderRangeExportQueryValidator : AbstractValidator<CreateOrderRangeExportQuery>
{
    public CreateOrderRangeExportQueryValidator()
    {
        RuleFor(query => query.Filter.Language)
            .NotEmpty()
            .MaximumLength(ExportLanguageRules.MaxLanguageLength)
            .Matches(ExportLanguageRules.LanguageTagPattern)
            .When(query => query.Filter != null);
    }
}
