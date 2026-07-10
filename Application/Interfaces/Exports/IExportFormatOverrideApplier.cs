// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatOverrideApplier
{
    Task<bool> ApplyAsync(string formatKey, ExportOptions options, CancellationToken cancellationToken = default);

    Task<bool> ApplyAsync(string formatKey, PayrollExportGroupConfig config, CancellationToken cancellationToken = default);
}
