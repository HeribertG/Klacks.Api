// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks a transformed import row against the rules the manual client form enforces: a person
/// (ClientPersonRules: first name, last name, person gender) or a legal entity (ClientLegalEntityRules: an
/// address with postcode, city and country; company and customer type are guaranteed by the transformer,
/// the person name is an optional contact person and needs no gender).
/// It also checks the column lengths of the client and communication tables, the entry-date plausibility
/// window shared with create_employee (MembershipValidFromPlausibility) and an exit date that lies before
/// the entry date.
/// </summary>

using Klacks.Api.Application.Common;
using Klacks.Api.Application.Validation.Clients;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportValidator
{
    public static void Validate(ClientImportDraft draft, ClientImportLookup lookup)
    {
        if (draft.LegalEntity)
        {
            ValidateLegalEntity(draft);
        }
        else
        {
            ValidatePerson(draft);
        }

        CheckLength(draft, ClientImportFields.FirstName, draft.FirstName, ClientImportLimits.MaxPersonFieldLength);
        CheckLength(draft, ClientImportFields.LastName, draft.LastName, ClientImportLimits.MaxPersonFieldLength);
        CheckLength(draft, ClientImportFields.Company, draft.Company, ClientImportLimits.MaxCompanyLength);
        CheckLength(draft, ClientImportFields.Title, draft.Title, ClientImportLimits.MaxPersonFieldLength);
        CheckLength(draft, ClientImportFields.Email, draft.Email, ClientImportLimits.MaxCommunicationValueLength);
        CheckLength(draft, ClientImportFields.Phone, draft.PhoneNumber, ClientImportLimits.MaxCommunicationValueLength);
        CheckLength(draft, ClientImportFields.Mobile, draft.MobileNumber, ClientImportLimits.MaxCommunicationValueLength);

        var implausibility = MembershipValidFromPlausibility.Evaluate(draft.EntryDate, draft.Birthdate, lookup.Today);
        if (implausibility != null)
        {
            draft.AddIssue(ClientImportFields.EntryDate, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.EntryDateImplausible,
                new Dictionary<string, string> { [ClientImportIssueArgs.Date] = ClientImportDateParser.Format(draft.EntryDate) });
        }

        if (draft.ExitDate.HasValue && draft.ExitDate.Value < draft.EntryDate)
        {
            draft.AddIssue(ClientImportFields.ExitDate, ClientImportIssueSeverity.Error, ClientImportIssueCodes.InvalidDate,
                new Dictionary<string, string>
                {
                    [ClientImportIssueArgs.Value] = ClientImportDateParser.Format(draft.ExitDate.Value),
                    [ClientImportIssueArgs.Reason] = ClientImportIssueArgs.ReasonExitBeforeEntry
                });
        }
    }

    private static void ValidatePerson(ClientImportDraft draft)
    {
        if (!ClientPersonRules.HasName(draft.FirstName))
        {
            draft.AddIssue(ClientImportFields.FirstName, ClientImportIssueSeverity.Error, ClientImportIssueCodes.MissingFirstName);
        }

        if (!ClientPersonRules.HasName(draft.LastName))
        {
            draft.AddIssue(ClientImportFields.LastName, ClientImportIssueSeverity.Error, ClientImportIssueCodes.MissingLastName);
        }

        if (draft.Gender is not { } gender || !ClientPersonRules.IsPersonGender(gender))
        {
            draft.AddIssue(ClientImportFields.Gender, ClientImportIssueSeverity.Error, ClientImportIssueCodes.MissingGender);
        }
    }

    private static void ValidateLegalEntity(ClientImportDraft draft)
    {
        if (ClientLegalEntityRules.IsCompleteAddress(draft.Zip, draft.City, draft.CountryCode))
        {
            return;
        }

        var field = string.IsNullOrWhiteSpace(draft.Zip) ? ClientImportFields.Zip
            : string.IsNullOrWhiteSpace(draft.City) ? ClientImportFields.City
            : ClientImportFields.Country;
        draft.AddIssue(field, ClientImportIssueSeverity.Error, ClientImportIssueCodes.LegalEntityAddressIncomplete);
    }

    private static void CheckLength(ClientImportDraft draft, string field, string? value, int maxLength)
    {
        if (value != null && value.Length > maxLength)
        {
            draft.AddIssue(field, ClientImportIssueSeverity.Error, ClientImportIssueCodes.ValueTooLong,
                new Dictionary<string, string> { [ClientImportIssueArgs.MaxLength] = maxLength.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
    }
}
