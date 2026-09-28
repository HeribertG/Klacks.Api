// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Validation.CalendarSelections
{
    public class PutCommandValidator : AbstractValidator<PutCommand<CalendarSelectionResource>>
    {
        public PutCommandValidator()
        {
            RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
        }
    }
}
