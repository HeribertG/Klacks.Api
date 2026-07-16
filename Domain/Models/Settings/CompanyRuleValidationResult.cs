// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of validating a single company-rule parameter value: Ok carries no message; an invalid value
/// carries an English error message describing why it was rejected.
/// </summary>
namespace Klacks.Api.Domain.Models.Settings;

public sealed class CompanyRuleValidationResult
{
    private CompanyRuleValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public string? ErrorMessage { get; }

    public static CompanyRuleValidationResult Ok() => new(true, null);

    public static CompanyRuleValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
