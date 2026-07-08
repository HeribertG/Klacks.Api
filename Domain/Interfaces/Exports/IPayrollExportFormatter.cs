// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Strategy interface for country-pack payroll export formats. Unlike IExportFormatter (order/booking
/// centric) it consumes the employee-centric PayrollExportData and the per-group configuration that
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
