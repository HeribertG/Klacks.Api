// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One person whose payroll content is new or changed since the last export of the period and format.
/// @param ClientId - The person
/// @param ClientName - Display name of the person
/// @param IdNumber - Personnel number of the person
/// @param IsNew - True when the person has never been exported for this period and format
/// @param PreviousRevision - Revision of the person's latest export; null when IsNew
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportPersonDto
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int IdNumber { get; set; }

    public bool IsNew { get; set; }

    public int? PreviousRevision { get; set; }
}
