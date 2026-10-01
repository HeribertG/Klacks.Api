// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shapes evaluated import drafts into the preview answer: one record per row exactly as it will be
/// written (dates as yyyy-MM-dd, phone numbers with their prefix, state and country only together with
/// an address, because ClientImportClientFactory writes them only on the address), its status and findings, the
/// summary counts, the columns nobody mapped and the mapped targets that are deliberately not imported.
/// </summary>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportPreviewBuilder
{
    private static readonly ClientImportTarget[] NotImportedTargets = [ClientImportTarget.PersonnelNumber];

    public static ClientImportPreviewResult Build(ClientImportRequest request, IReadOnlyList<ClientImportDraft> drafts)
    {
        var rows = drafts.Select(ToPreviewRow).ToList();
        var mappedColumns = request.Mapping
            .Where(m => m.Target != ClientImportTarget.Ignore)
            .Select(m => m.ColumnIndex)
            .ToHashSet();

        return new ClientImportPreviewResult
        {
            Rows = rows,
            Summary = new ClientImportSummary
            {
                Total = rows.Count,
                Ready = rows.Count(r => r.Status == ClientImportRowStatus.Ready),
                Skipped = rows.Count(r => r.Status == ClientImportRowStatus.Skipped),
                Errors = rows.Count(r => r.Status == ClientImportRowStatus.Error),
                Duplicates = drafts.Count(d => d.DuplicateConflict),
                Warnings = rows.Count(r => r.Issues.Any(i => i.Severity == ClientImportIssueSeverity.Warning))
            },
            UnmappedColumns = Enumerable.Range(0, request.Columns.Count).Where(c => !mappedColumns.Contains(c)).ToList(),
            IgnoredTargets = request.Mapping.Select(m => m.Target).Where(NotImportedTargets.Contains).Distinct().ToList()
        };
    }

    private static ClientImportPreviewRow ToPreviewRow(ClientImportDraft draft) => new()
    {
        RowIndex = draft.RowIndex,
        Status = draft.Status,
        Issues = draft.Issues,
        DuplicateOfClientId = draft.DuplicateOfClientId,
        DuplicateOfName = draft.DuplicateOfName,
        DuplicateOfRowIndex = draft.DuplicateOfRowIndex,
        Record = new ClientImportRecord
        {
            FirstName = draft.FirstName,
            LastName = draft.LastName,
            Title = draft.Title,
            Gender = draft.Gender?.ToString(),
            Birthdate = FormatDate(draft.Birthdate),
            Street = draft.Street,
            AddressLine2 = draft.AddressLine2,
            Zip = draft.Zip,
            City = draft.City,
            State = draft.HasAddress ? draft.State : null,
            Country = draft.HasAddress ? draft.CountryCode : null,
            Email = draft.Email,
            Phone = FormatPhone(draft.PhonePrefix, draft.PhoneNumber),
            Mobile = FormatPhone(draft.MobilePrefix, draft.MobileNumber),
            EntryDate = ClientImportDateParser.Format(draft.EntryDate),
            ExitDate = FormatDate(draft.ExitDate),
            ContractName = draft.Contract?.Name,
            GroupName = draft.Group?.Name,
            Note = draft.Note
        }
    };

    private static string? FormatDate(DateTime? value) => value.HasValue ? ClientImportDateParser.Format(value.Value) : null;

    private static string? FormatPhone(string? prefix, string? number) =>
        number == null ? null : string.IsNullOrEmpty(prefix) ? number : string.Concat(prefix, " ", number);
}
