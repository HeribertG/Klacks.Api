// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds duplicates of import rows, first against the existing employees and then among the earlier
/// rows of the same file. The same e-mail address or the same first name, last name and birthdate is a
/// conflict: by default the row is skipped, the policy "create anyway" or a per-row choice keeps it. The
/// same name without matching birthdate is only a hint. Rows the user skipped take no part, and only
/// rows that will actually be written count as the earlier row of an in-file duplicate (a skipped former
/// stint of a rehired employee must not suppress the current one).
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportDuplicateDetector
{
    private const char KeySeparator = '|';

    public static void Detect(IReadOnlyList<ClientImportDraft> drafts, ClientImportDuplicateMode mode, ClientImportLookup lookup)
    {
        var existing = IndexExisting(lookup.ExistingClients);
        var earlierByEmail = new Dictionary<string, ClientImportDraft>(StringComparer.Ordinal);
        var earlierByPerson = new Dictionary<string, ClientImportDraft>(StringComparer.Ordinal);

        foreach (var draft in drafts.Where(d => !d.SkippedByUser))
        {
            var emailKey = draft.Email == null ? null : EmailKey(draft.Email);
            var nameKey = NameKey(draft.FirstName, draft.LastName);
            var personKey = PersonKey(nameKey, draft.Birthdate);
            var createDuplicate = lookup.RowOverrides.GetValueOrDefault(draft.RowIndex)?.CreateDuplicate;

            if (Find(emailKey, personKey, existing.ByEmail, existing.ByPerson) is { } inDatabase)
            {
                draft.DuplicateOfClientId = inDatabase.Id;
                draft.DuplicateOfName = DisplayName(inDatabase.FirstName, inDatabase.Name);
                MarkConflict(draft, mode, createDuplicate, ClientImportIssueCodes.DuplicateInDatabase,
                    new Dictionary<string, string> { [ClientImportIssueArgs.Name] = draft.DuplicateOfName });
            }
            else if (Find(emailKey, personKey, earlierByEmail, earlierByPerson) is { } earlier)
            {
                draft.DuplicateOfRowIndex = earlier.RowIndex;
                MarkConflict(draft, mode, createDuplicate, ClientImportIssueCodes.DuplicateInFile,
                    new Dictionary<string, string> { [ClientImportIssueArgs.Row] = earlier.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            else if (nameKey != null && existing.ByName.TryGetValue(nameKey, out var sameName))
            {
                draft.AddIssue(ClientImportFields.LastName, ClientImportIssueSeverity.Info, ClientImportIssueCodes.PossibleDuplicateName,
                    new Dictionary<string, string> { [ClientImportIssueArgs.Name] = DisplayName(sameName.FirstName, sameName.Name) });
            }

            if (draft.FormerEmployeeSkipped || draft.DuplicateSkipped)
            {
                continue;
            }

            if (emailKey != null)
            {
                earlierByEmail.TryAdd(emailKey, draft);
            }

            if (personKey != null)
            {
                earlierByPerson.TryAdd(personKey, draft);
            }
        }
    }

    private static void MarkConflict(
        ClientImportDraft draft, ClientImportDuplicateMode mode, bool? createDuplicate, string code, Dictionary<string, string> args)
    {
        draft.DuplicateConflict = true;
        draft.DuplicateSkipped = !(createDuplicate ?? mode == ClientImportDuplicateMode.CreateAnyway);
        draft.AddIssue(ClientImportFields.LastName, ClientImportIssueSeverity.Warning, code, args);
    }

    public static string EmailKey(string email) => email.Trim().ToLowerInvariant();

    private static string? NameKey(string? firstName, string? lastName)
    {
        var first = ClientImportTextNormalizer.Normalize(firstName);
        var last = ClientImportTextNormalizer.Normalize(lastName);
        return last.Length == 0 ? null : string.Concat(first, KeySeparator, last);
    }

    private static string? PersonKey(string? nameKey, DateTime? birthdate) =>
        nameKey == null || !birthdate.HasValue ? null : string.Concat(nameKey, KeySeparator, ClientImportDateParser.Format(birthdate.Value));

    private static string DisplayName(string? firstName, string lastName) =>
        string.IsNullOrWhiteSpace(firstName) ? lastName : string.Concat(firstName, " ", lastName);

    private static (Dictionary<string, ClientImportExistingClient> ByEmail,
        Dictionary<string, ClientImportExistingClient> ByPerson,
        Dictionary<string, ClientImportExistingClient> ByName) IndexExisting(IEnumerable<ClientImportExistingClient> clients)
    {
        var byEmail = new Dictionary<string, ClientImportExistingClient>(StringComparer.Ordinal);
        var byPerson = new Dictionary<string, ClientImportExistingClient>(StringComparer.Ordinal);
        var byName = new Dictionary<string, ClientImportExistingClient>(StringComparer.Ordinal);

        foreach (var client in clients)
        {
            var nameKey = NameKey(client.FirstName, client.Name);
            if (nameKey != null)
            {
                byName.TryAdd(nameKey, client);
            }

            if (PersonKey(nameKey, client.Birthdate) is { } personKey)
            {
                byPerson.TryAdd(personKey, client);
            }

            foreach (var email in client.Emails)
            {
                byEmail.TryAdd(EmailKey(email), client);
            }
        }

        return (byEmail, byPerson, byName);
    }

    private static TValue? Find<TValue>(string? emailKey, string? personKey, Dictionary<string, TValue> byEmail, Dictionary<string, TValue> byPerson)
        where TValue : class
    {
        if (emailKey != null && byEmail.TryGetValue(emailKey, out var found))
        {
            return found;
        }

        return personKey != null && byPerson.TryGetValue(personKey, out found) ? found : null;
    }
}
