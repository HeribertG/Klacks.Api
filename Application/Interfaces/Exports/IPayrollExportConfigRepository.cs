// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the installation-wide payroll-export configuration. Always returns a usable configuration:
/// the default target system comes from the DEFAULT_PAYROLL_TARGET_SYSTEM setting, so a country-pack
/// handler never fails on a missing configuration.
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollExportConfigRepository
{
    Task<PayrollExportGroupConfig> GetAsync(CancellationToken cancellationToken = default);
}
