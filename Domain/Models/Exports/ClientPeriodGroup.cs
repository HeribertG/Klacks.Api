// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Groups all work entries for a single client (employee or external employee) within
/// a client period export.
/// @param ClientId - ID of the client this group belongs to
/// @param ClientType - Employee or ExternEmp (Customer clients are never included)
/// @param WorkEntries - The work entries performed by this client within the export period
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

    public List<ClientPeriodHoursExportEntry> PeriodHours { get; set; } = [];
}
