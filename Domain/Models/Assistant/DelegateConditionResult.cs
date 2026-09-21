// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a "mach du" delegation plus, for a refusal, the reason in plain English so the client can
/// show the person WHY they may not delegate this one - the outcome alone cannot tell "the remediation
/// needs a permission you do not hold" from "this finding type has nothing Klacksy could carry out".
/// </summary>
/// <param name="Outcome">Delegated, NotFound or Forbidden; see DelegateConditionOutcome for what each hides.</param>
/// <param name="Reason">Null unless the outcome is Forbidden.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record DelegateConditionResult(DelegateConditionOutcome Outcome, string? Reason = null)
{
    public static DelegateConditionResult Delegated { get; } = new(DelegateConditionOutcome.Delegated);

    public static DelegateConditionResult NotFound { get; } = new(DelegateConditionOutcome.NotFound);

    public static DelegateConditionResult Forbidden(string reason) => new(DelegateConditionOutcome.Forbidden, reason);
}
