// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strategy interface for country-pack payroll export formats. Unlike IExportFormatter (order/booking
/// centric) it consumes the employee-centric PayrollExportData and the installation-wide configuration that
/// supplies delimiter, encoding and the wage-type / absence-key mapping.
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Domain.Interfaces.Exports;

public interface IPayrollExportFormatter
{
    string FormatKey { get; }

    string ContentType { get; }

    string FileExtension { get; }

    PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config);
}
