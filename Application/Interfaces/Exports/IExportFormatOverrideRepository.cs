// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatOverrideRepository
{
    Task<IReadOnlyList<ExportFormatOverride>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExportFormatOverride?> GetByFormatKeyAsync(string formatKey, CancellationToken cancellationToken = default);

    Task AddAsync(ExportFormatOverride entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(ExportFormatOverride entry, CancellationToken cancellationToken = default);
}
