// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns one raw import row into a draft: splits names and "postcode city", parses dates in the chosen
/// order, derives the gender from a gender or salutation column (or the user's per-row choice), resolves
/// country, contract and group by name, splits phone numbers and applies the import policy for missing
/// entry date, contract, group and former employees. Every substitution is recorded as an issue so the
/// preview explains what will be written.
/// </summary>
/// <param name="catalog">Gender and salutation vocabulary of all supported languages</param>

using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportTransformer
{
    private readonly ClientImportSynonymCatalog _catalog;

    public ClientImportTransformer(ClientImportSynonymCatalog catalog)
    {
        _catalog = catalog;
    }

    public ClientImportDraft Transform(ClientImportRequest request, ClientImportColumnMap columns, int rowIndex, ClientImportLookup lookup)
    {
        var row = new ClientImportRowReader(request.Rows[rowIndex], columns);
        var rowOverride = lookup.RowOverrides.GetValueOrDefault(rowIndex);
        var draft = new ClientImportDraft
        {
            RowIndex = rowIndex,
            SkippedByUser = rowOverride?.Skip == true,
            Title = row.Value(ClientImportTarget.Title),
            Note = row.Value(ClientImportTarget.Note)
        };

        ApplyNames(draft, row, request.NameOrder);
        ApplyGender(draft, row, rowOverride);
        ApplyDates(draft, row, request, lookup);
        ApplyAddress(draft, row, lookup);
        ApplyCommunications(draft, row, lookup);
        ApplyAssignments(draft, row, lookup);

        if (row.Value(ClientImportTarget.PersonnelNumber) != null)
        {
            draft.AddIssue(null, ClientImportIssueSeverity.Info, ClientImportIssueCodes.PersonnelNumberNotImported);
        }

        return draft;
    }

    private static void ApplyNames(ClientImportDraft draft, ClientImportRowReader row, ClientImportNameOrder nameOrder)
    {
        draft.FirstName = row.Value(ClientImportTarget.FirstName);
        draft.LastName = row.Value(ClientImportTarget.LastName);

        var fullName = row.Value(ClientImportTarget.FullName);
        if (fullName == null || (draft.FirstName != null && draft.LastName != null))
        {
            return;
        }

        var parts = ClientImportNameSplitter.Split(fullName, nameOrder);
        draft.FirstName ??= parts.FirstName;
        draft.LastName ??= parts.LastName;

        if (parts.MultiWord)
        {
            draft.AddIssue(ClientImportFields.LastName, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.MultiWordName,
                Args(ClientImportIssueArgs.Value, fullName));
        }
    }

    private void ApplyGender(ClientImportDraft draft, ClientImportRowReader row, ClientImportRowOverride? rowOverride)
    {
        if (rowOverride?.Gender != null)
        {
            draft.Gender = rowOverride.Gender;
            return;
        }

        draft.Gender = _catalog.ResolveGender(row.Value(ClientImportTarget.Gender));
        if (draft.Gender != null)
        {
            return;
        }

        var salutation = row.Value(ClientImportTarget.Salutation);
        draft.Gender = _catalog.ResolveGender(salutation);
        if (draft.Gender != null)
        {
            draft.AddIssue(ClientImportFields.Gender, ClientImportIssueSeverity.Info, ClientImportIssueCodes.GenderFromSalutation,
                Args(ClientImportIssueArgs.Value, salutation!));
        }
    }

    private static void ApplyDates(ClientImportDraft draft, ClientImportRowReader row, ClientImportRequest request, ClientImportLookup lookup)
    {
        var currentYear = lookup.Today.Year;

        draft.Birthdate = ParseDate(draft, row, ClientImportTarget.Birthdate, ClientImportFields.Birthdate, request.DateFormat, currentYear);
        if (draft.Birthdate > lookup.Today)
        {
            AddInvalidDate(draft, ClientImportFields.Birthdate, row.Value(ClientImportTarget.Birthdate)!, ClientImportIssueArgs.ReasonFuture);
            draft.Birthdate = null;
        }

        var entryDate = ParseDate(draft, row, ClientImportTarget.EntryDate, ClientImportFields.EntryDate, request.DateFormat, currentYear);
        draft.EntryDate = entryDate ?? lookup.PolicyEntryDate;
        if (!entryDate.HasValue && row.Value(ClientImportTarget.EntryDate) == null)
        {
            draft.AddIssue(ClientImportFields.EntryDate, ClientImportIssueSeverity.Info, ClientImportIssueCodes.EntryDateFromPolicy,
                Args(ClientImportIssueArgs.Date, ClientImportDateParser.Format(draft.EntryDate)));
        }

        draft.ExitDate = ParseDate(draft, row, ClientImportTarget.ExitDate, ClientImportFields.ExitDate, request.DateFormat, currentYear);
        if (draft.ExitDate.HasValue && draft.ExitDate.Value < lookup.Today
            && request.Policy.FormerEmployees == ClientImportFormerEmployeesMode.Skip)
        {
            draft.FormerEmployeeSkipped = true;
            draft.AddIssue(ClientImportFields.ExitDate, ClientImportIssueSeverity.Info, ClientImportIssueCodes.FormerEmployeeSkipped,
                Args(ClientImportIssueArgs.Date, ClientImportDateParser.Format(draft.ExitDate.Value)));
        }
    }

    private static DateTime? ParseDate(
        ClientImportDraft draft, ClientImportRowReader row, ClientImportTarget target, string field, ClientImportDateFormat format, int currentYear)
    {
        var value = row.Value(target);
        if (value == null)
        {
            return null;
        }

        if (!ClientImportDateParser.TryParse(value, format, currentYear, out var date, out var twoDigitYear))
        {
            AddInvalidDate(draft, field, value, null);
            return null;
        }

        if (twoDigitYear)
        {
            var args = Args(ClientImportIssueArgs.Value, value);
            args[ClientImportIssueArgs.Date] = ClientImportDateParser.Format(date);
            draft.AddIssue(field, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.AmbiguousTwoDigitYear, args);
        }

        return date;
    }

    private static void AddInvalidDate(ClientImportDraft draft, string field, string value, string? reason)
    {
        var args = Args(ClientImportIssueArgs.Value, value);
        if (reason != null)
        {
            args[ClientImportIssueArgs.Reason] = reason;
        }

        draft.AddIssue(field, ClientImportIssueSeverity.Error, ClientImportIssueCodes.InvalidDate, args);
    }

    private static void ApplyAddress(ClientImportDraft draft, ClientImportRowReader row, ClientImportLookup lookup)
    {
        draft.Street = ClientImportAddressSplitter.CombineStreet(row.Value(ClientImportTarget.Street), row.Value(ClientImportTarget.HouseNumber));
        draft.AddressLine2 = row.Value(ClientImportTarget.AddressLine2);
        draft.Zip = row.Value(ClientImportTarget.Zip);
        draft.City = row.Value(ClientImportTarget.City);
        draft.State = row.Value(ClientImportTarget.State);

        var zipCity = row.Value(ClientImportTarget.ZipCity);
        if (zipCity != null && draft.Zip == null && draft.City == null)
        {
            ApplyZipCity(draft, zipCity);
        }

        draft.CountryCode = ResolveCountry(draft, row.Value(ClientImportTarget.Country), lookup)?.Abbreviation;

        if (!draft.HasAddress)
        {
            draft.AddIssue(ClientImportFields.Street, ClientImportIssueSeverity.Info, ClientImportIssueCodes.MissingAddress);
        }
    }

    private static void ApplyZipCity(ClientImportDraft draft, string zipCity)
    {
        var args = Args(ClientImportIssueArgs.Value, zipCity);

        if (ClientImportAddressSplitter.TrySplitZipCity(zipCity, out var zip, out var city))
        {
            draft.Zip = zip;
            draft.City = city;
            draft.AddIssue(ClientImportFields.City, ClientImportIssueSeverity.Info, ClientImportIssueCodes.ZipCitySplit, args);
            return;
        }

        draft.City = zipCity;
        args[ClientImportIssueArgs.Reason] = ClientImportIssueArgs.ReasonNotSplit;
        draft.AddIssue(ClientImportFields.City, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.ZipCitySplit, args);
    }

    private static Countries? ResolveCountry(ClientImportDraft draft, string? value, ClientImportLookup lookup)
    {
        if (value == null)
        {
            return lookup.DefaultCountry;
        }

        if (lookup.CountriesByValue.TryGetValue(value, out var country) && country != null)
        {
            return country;
        }

        draft.AddIssue(ClientImportFields.Country, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.UnknownCountry,
            Args(ClientImportIssueArgs.Value, value));
        return lookup.DefaultCountry;
    }

    private static void ApplyCommunications(ClientImportDraft draft, ClientImportRowReader row, ClientImportLookup lookup)
    {
        var email = row.Value(ClientImportTarget.Email);
        if (email != null)
        {
            if (ClientImportValueClassifier.IsEmail(email))
            {
                draft.Email = email;
            }
            else
            {
                draft.AddIssue(ClientImportFields.Email, ClientImportIssueSeverity.Warning, ClientImportIssueCodes.InvalidEmail,
                    Args(ClientImportIssueArgs.Value, email));
            }
        }

        var countryPrefix = CountryPrefix(draft.CountryCode, lookup);
        (draft.PhonePrefix, draft.PhoneNumber) = SplitPhone(row.Value(ClientImportTarget.Phone), countryPrefix);
        (draft.MobilePrefix, draft.MobileNumber) = SplitPhone(row.Value(ClientImportTarget.Mobile), countryPrefix);
    }

    private static string? CountryPrefix(string? countryCode, ClientImportLookup lookup)
    {
        if (countryCode == null)
        {
            return null;
        }

        if (string.Equals(lookup.DefaultCountry?.Abbreviation, countryCode, StringComparison.OrdinalIgnoreCase))
        {
            return lookup.DefaultCountry!.Prefix;
        }

        return lookup.CountriesByValue.Values
            .FirstOrDefault(c => c != null && string.Equals(c.Abbreviation, countryCode, StringComparison.OrdinalIgnoreCase))?.Prefix;
    }

    private static (string? Prefix, string? Number) SplitPhone(string? value, string? countryPrefix)
    {
        if (value == null)
        {
            return (null, null);
        }

        var (prefix, number) = PhoneNumberSplitter.Split(value, countryPrefix);
        return number.Length == 0 ? (null, null) : (prefix, number);
    }

    private static void ApplyAssignments(ClientImportDraft draft, ClientImportRowReader row, ClientImportLookup lookup)
    {
        draft.Contract = ResolveNamed(draft, row.Value(ClientImportTarget.Contract), lookup.Contracts, lookup.PolicyContract,
            ClientImportFields.Contract, ClientImportIssueCodes.UnknownContract, ClientImportIssueCodes.ContractFromPolicy);
        draft.Group = ResolveNamed(draft, row.Value(ClientImportTarget.Group), lookup.Groups, lookup.PolicyGroup,
            ClientImportFields.Group, ClientImportIssueCodes.UnknownGroup, ClientImportIssueCodes.GroupFromPolicy);
    }

    private static ClientImportNamedEntity? ResolveNamed(
        ClientImportDraft draft,
        string? value,
        IReadOnlyList<ClientImportNamedEntity> known,
        ClientImportNamedEntity? fromPolicy,
        string field,
        string unknownCode,
        string fromPolicyCode)
    {
        if (value != null)
        {
            var matches = known.Where(k => string.Equals(k.Name.Trim(), value, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }

            var args = Args(ClientImportIssueArgs.Value, value);
            if (matches.Count > 1)
            {
                args[ClientImportIssueArgs.Reason] = ClientImportIssueArgs.ReasonAmbiguous;
            }

            draft.AddIssue(field, ClientImportIssueSeverity.Warning, unknownCode, args);
        }

        if (fromPolicy != null)
        {
            draft.AddIssue(field, ClientImportIssueSeverity.Info, fromPolicyCode, Args(ClientImportIssueArgs.Name, fromPolicy.Name));
        }

        return fromPolicy;
    }

    private static Dictionary<string, string> Args(string key, string value) => new() { [key] = value };
}
