// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks a bulk creation of stand-ins before anything is read: a count from 1 to StandInConstants.MaxBulkCount, a name
/// pattern with the index placeholder exactly once and at most StandInConstants.MaxNamePatternLength characters, a group,
/// a membership start and an end that is not before it, and no qualification twice. Whether the group is visible and
/// the qualifications exist is checked by the handler.
/// </summary>

using FluentValidation;
using Klacks.Api.Application.Commands.Clients;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Validation.Clients;

public class CreateStandInsCommandValidator : AbstractValidator<CreateStandInsCommand>
{
    public const string CountMessage = "The number of stand-ins must be between 1 and {0}.";
    public const string NamePatternMessage = "The name pattern must contain {0} exactly once and have at most {1} characters.";
    public const string GroupRequiredMessage = "A group is required.";
    public const string PeriodMessage = "The end of the membership must not be before its start.";
    public const string ValidFromRequiredMessage = "The start of the membership is required.";
    public const string DuplicateQualificationMessage = "A qualification is listed twice.";

    public CreateStandInsCommandValidator()
    {
        RuleFor(x => x.Resource.Count)
            .InclusiveBetween(1, StandInConstants.MaxBulkCount)
            .WithMessage(string.Format(CountMessage, StandInConstants.MaxBulkCount));

        RuleFor(x => x.Resource.NamePattern)
            .Must(pattern => StandInNamePattern.HasSingleIndexPlaceholder(pattern)
                && pattern.Length <= StandInConstants.MaxNamePatternLength
                && !string.IsNullOrWhiteSpace(pattern.Replace(StandInConstants.IndexPlaceholder, string.Empty, StringComparison.Ordinal)))
            .WithMessage(string.Format(NamePatternMessage, StandInConstants.IndexPlaceholder, StandInConstants.MaxNamePatternLength));

        RuleFor(x => x.Resource.GroupId)
            .NotEqual(Guid.Empty)
            .WithMessage(GroupRequiredMessage);

        RuleFor(x => x.Resource.ValidFrom)
            .NotEqual(default(DateOnly))
            .WithMessage(ValidFromRequiredMessage);

        RuleFor(x => x.Resource.ValidUntil)
            .Must((command, until) => until == null || until.Value >= command.Resource.ValidFrom)
            .WithMessage(PeriodMessage);

        RuleFor(x => x.Resource.QualificationIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage(DuplicateQualificationMessage);
    }
}
