// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One import row after transformation: the values that will be written, the findings about the row,
/// and its duplicate and skip state. Preview shows it, commit builds the client from it, so both see the
/// identical result.
/// </summary>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportDraft
{
    public int RowIndex { get; init; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Title { get; set; }

    public GenderEnum? Gender { get; set; }

    public DateTime? Birthdate { get; set; }

    public string? Street { get; set; }

    public string? AddressLine2 { get; set; }

    public string? Zip { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? CountryCode { get; set; }

    public string? Email { get; set; }

    public string? PhonePrefix { get; set; }

    public string? PhoneNumber { get; set; }

    public string? MobilePrefix { get; set; }

    public string? MobileNumber { get; set; }

    public DateTime EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    public ClientImportNamedEntity? Contract { get; set; }

    public ClientImportNamedEntity? Group { get; set; }

    public string? Note { get; set; }

    public bool SkippedByUser { get; set; }

    public bool FormerEmployeeSkipped { get; set; }

    public bool DuplicateConflict { get; set; }

    public bool DuplicateSkipped { get; set; }

    public Guid? DuplicateOfClientId { get; set; }

    public string? DuplicateOfName { get; set; }

    public int? DuplicateOfRowIndex { get; set; }

    public List<ClientImportIssue> Issues { get; } = [];

    public bool HasAddress =>
        !string.IsNullOrWhiteSpace(Street) || !string.IsNullOrWhiteSpace(Zip) || !string.IsNullOrWhiteSpace(City);

    public bool HasErrors => Issues.Any(i => i.Severity == ClientImportIssueSeverity.Error);

    public ClientImportRowStatus Status =>
        SkippedByUser || FormerEmployeeSkipped || DuplicateSkipped
            ? ClientImportRowStatus.Skipped
            : HasErrors ? ClientImportRowStatus.Error : ClientImportRowStatus.Ready;

    public void AddIssue(string? field, ClientImportIssueSeverity severity, string code, Dictionary<string, string>? args = null) =>
        Issues.Add(new ClientImportIssue { Field = field, Severity = severity, Code = code, Args = args ?? [] });
}
