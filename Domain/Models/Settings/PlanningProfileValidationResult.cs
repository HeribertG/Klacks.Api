// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of validating a single planning-profile parameter value: Ok carries no message; an invalid
/// value carries an English error message describing why it was rejected so the assistant can relay it.
/// </summary>
namespace Klacks.Api.Domain.Models.Settings;

public sealed class PlanningProfileValidationResult
{
    private PlanningProfileValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public string? ErrorMessage { get; }

    public static PlanningProfileValidationResult Ok() => new(true, null);

    public static PlanningProfileValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
