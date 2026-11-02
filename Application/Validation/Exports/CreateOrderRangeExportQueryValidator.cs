// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the Language field of CreateOrderRangeExportQuery; see ExportLanguageRules.
/// </summary>
using FluentValidation;
using Klacks.Api.Application.Queries.Exports;

namespace Klacks.Api.Application.Validation.Exports;

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
