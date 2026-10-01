// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stable codes of the per-row findings of an employee import preview. The UI translates each as
/// clientImport.issue.&lt;code&gt;, so a code must never be renamed silently.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportIssueCodes
{
    public const string MissingLastName = "missing-last-name";
    public const string MissingFirstName = "missing-first-name";
    public const string MissingGender = "missing-gender";
    public const string GenderFromSalutation = "gender-from-salutation";
    public const string InvalidDate = "invalid-date";
    public const string AmbiguousTwoDigitYear = "ambiguous-two-digit-year";
    public const string InvalidEmail = "invalid-email";
    public const string UnknownCountry = "unknown-country";
    public const string UnknownContract = "unknown-contract";
    public const string UnknownGroup = "unknown-group";
    public const string ContractFromPolicy = "contract-from-policy";
    public const string GroupFromPolicy = "group-from-policy";
    public const string EntryDateFromPolicy = "entry-date-from-policy";
    public const string EntryDateImplausible = "entry-date-implausible";
    public const string FormerEmployeeSkipped = "former-employee-skipped";
    public const string DuplicateInFile = "duplicate-in-file";
    public const string DuplicateInDatabase = "duplicate-in-database";
    public const string PossibleDuplicateName = "possible-duplicate-name";
    public const string MissingAddress = "missing-address";
    public const string ZipCitySplit = "zip-city-split";
    public const string MultiWordName = "multi-word-name";
    public const string PersonnelNumberNotImported = "personnel-number-not-imported";
    public const string ValueTooLong = "value-too-long";
}
