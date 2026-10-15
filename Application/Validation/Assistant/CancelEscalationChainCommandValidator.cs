// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects a cancel without a reason as a 400 before the chain service would throw on it (Owner decision B7:
/// the reason is mandatory).
/// </summary>

using FluentValidation;
using Klacks.Api.Application.Commands.Assistant;

namespace Klacks.Api.Application.Validation.Assistant;

public class CancelEscalationChainCommandValidator : AbstractValidator<CancelEscalationChainCommand>
{
    private const string ReasonRequiredMessage = "A reason is required to cancel an escalation chain.";

    public CancelEscalationChainCommandValidator()
    {
        RuleFor(x => x.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage(ReasonRequiredMessage);
    }
}
