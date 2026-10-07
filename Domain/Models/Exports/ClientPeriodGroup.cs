// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Groups all work entries for a single client (employee or external employee) within
/// a client period export.
/// @param ClientId - ID of the client this group belongs to
/// @param ClientType - Employee or ExternEmp (Customer clients are never included)
/// @param WorkEntries - The work entries performed by this client within the export period; the earliest work of a day lists that day's absences
/// @param Absences - Absences on days on which the client has no work entry (vacation, sickness, on-call duty without call-out)
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Exports;

public class ClientPeriodGroup
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int ClientIdNumber { get; set; }

    public EntityTypeEnum ClientType { get; set; }

    public List<ClientWorkExportEntry> WorkEntries { get; set; } = [];

    public List<BreakExportEntry> Absences { get; set; } = [];

    public List<ClientPeriodHoursExportEntry> PeriodHours { get; set; } = [];
}
