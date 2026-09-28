// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads closed work entries for employees and external employees within a date range,
/// grouped by client, for the client period export.
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IClientPeriodExportDataLoader
{
    Task<ClientPeriodExportData> LoadAsync(DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default);
}
