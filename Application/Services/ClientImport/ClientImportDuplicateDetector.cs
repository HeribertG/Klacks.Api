// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds duplicates of import rows, first against the existing clients and then among the earlier rows
/// of the same file, always within the same group: customers among customers, employees and external
/// employees among each other (ClientImportDuplicateKeys). The same e-mail address, the same first name,
/// last name and birthdate, or for a customer with company the same company, postcode and street is
/// a conflict: by default the row is skipped, the policy "create anyway" or a per-row choice keeps it. The
/// same name, or the same company at another address, is only a hint. Rows the user skipped take no part,
/// and only rows that will actually be written count as the earlier row of an in-file duplicate (a skipped
/// former stint of a rehired employee must not suppress the current one). A conflict found through the
/// company carries the reason "same-company" so the preview names the right criterion.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportDuplicateDetector
{
    private const string NameSeparator = " ";

    public static void Detect(IReadOnlyList<ClientImportDraft> drafts, ClientImportDuplicateMode mode, ClientImportLookup lookup)
    {
        var existing = new ClientImportDuplicateIndex<ClientImportExistingClient>();
        foreach (var client in lookup.ExistingClients)
        {
            existing.Add(ClientImportDuplicateKeys.ForExisting(client), client);
        }

        var earlier = new ClientImportDuplicateIndex<ClientImportDraft>();

        foreach (var draft in drafts.Where(d => !d.SkippedByUser))
        {
            var keys = ClientImportDuplicateKeys.ForDraft(draft);
            var field = draft.LegalEntity ? ClientImportFields.Company : ClientImportFields.LastName;
            var createDuplicate = lookup.RowOverrides.GetValueOrDefault(draft.RowIndex)?.CreateDuplicate;

            if (existing.FindConflict(keys) is { } inDatabase)
            {
                draft.DuplicateOfClientId = inDatabase.Value.Id;
                draft.DuplicateOfName = DisplayName(inDatabase.Value.FirstName, inDatabase.Value.Name, inDatabase.Value.Company);
                MarkConflict(draft, field, mode, createDuplicate, ClientImportIssueCodes.DuplicateInDatabase,
                    WithReason(ClientImportIssueArgs.Name, draft.DuplicateOfName, inDatabase.BySameCompany));
            }
            else if (earlier.FindConflict(keys) is { } inFile)
            {
                draft.DuplicateOfRowIndex = inFile.Value.RowIndex;
                MarkConflict(draft, field, mode, createDuplicate, ClientImportIssueCodes.DuplicateInFile,
                    WithReason(ClientImportIssueArgs.Row, inFile.Value.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), inFile.BySameCompany));
            }
            else if (existing.FindSameName(keys) is { } sameName)
            {
                draft.AddIssue(field, ClientImportIssueSeverity.Info, ClientImportIssueCodes.PossibleDuplicateName,
                    WithReason(ClientImportIssueArgs.Name, DisplayName(sameName.FirstName, sameName.Name, sameName.Company), false));
            }
            else if (existing.FindSameCompany(keys) is { } sameCompany)
            {
                draft.AddIssue(field, ClientImportIssueSeverity.Info, ClientImportIssueCodes.PossibleDuplicateName,
                    WithReason(ClientImportIssueArgs.Name, DisplayName(sameCompany.FirstName, sameCompany.Name, sameCompany.Company), true));
            }

            if (draft.FormerEmployeeSkipped || draft.DuplicateSkipped)
            {
                continue;
            }

            earlier.Add(keys, draft);
        }
    }

    private static void MarkConflict(
        ClientImportDraft draft, string field, ClientImportDuplicateMode mode, bool? createDuplicate, string code, Dictionary<string, string> args)
    {
        draft.DuplicateConflict = true;
        draft.DuplicateSkipped = !(createDuplicate ?? mode == ClientImportDuplicateMode.CreateAnyway);
        draft.AddIssue(field, ClientImportIssueSeverity.Warning, code, args);
    }

    private static Dictionary<string, string> WithReason(string key, string value, bool bySameCompany)
    {
        var args = new Dictionary<string, string> { [key] = value };
        if (bySameCompany)
        {
            args[ClientImportIssueArgs.Reason] = ClientImportIssueArgs.ReasonSameCompany;
        }

        return args;
    }

    private static string DisplayName(string? firstName, string? lastName, string? company)
    {
        var personName = string.Join(NameSeparator, new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return personName.Length > 0 ? personName : company?.Trim() ?? string.Empty;
    }
}
