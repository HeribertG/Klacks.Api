// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the per-group payroll-export configuration. Always returns a usable configuration:
/// when no row exists for the group a sensible default is returned so a country-pack handler
/// never fails on a missing configuration.
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollExportConfigRepository
{
    Task<PayrollExportGroupConfig> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
