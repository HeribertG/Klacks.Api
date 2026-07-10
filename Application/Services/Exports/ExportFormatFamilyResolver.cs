// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps override-capable format keys to their family from the registered formatter sets: order
/// formatters, payroll formatters, and client-period formatters (exposed with the clientperiod-
/// prefix as used in the export log).
/// </summary>
/// <param name="orderFormatters">Registered order export formatters</param>
/// <param name="payrollFormatters">Registered payroll export formatters</param>
/// <param name="clientPeriodFormatters">Registered client-period export formatters</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Exports;

namespace Klacks.Api.Application.Services.Exports;

public class ExportFormatFamilyResolver : IExportFormatFamilyResolver
{
    public const string FamilyClientPeriod = "clientperiod";

    private readonly IEnumerable<IExportFormatter> _orderFormatters;
    private readonly IEnumerable<IPayrollExportFormatter> _payrollFormatters;
    private readonly IEnumerable<IClientPeriodExportFormatter> _clientPeriodFormatters;

    public ExportFormatFamilyResolver(
        IEnumerable<IExportFormatter> orderFormatters,
        IEnumerable<IPayrollExportFormatter> payrollFormatters,
        IEnumerable<IClientPeriodExportFormatter> clientPeriodFormatters)
    {
        _orderFormatters = orderFormatters;
        _payrollFormatters = payrollFormatters;
        _clientPeriodFormatters = clientPeriodFormatters;
    }

    public IReadOnlyList<(string FormatKey, string Family)> GetAll()
    {
        var result = new List<(string, string)>();
        result.AddRange(_orderFormatters.Select(f => (f.FormatKey, ExportConstants.FormatFamilyOrder)));
        result.AddRange(_payrollFormatters.Select(f => (f.FormatKey, ExportConstants.FormatFamilyPayroll)));
        result.AddRange(_clientPeriodFormatters.Select(f =>
            ($"{ExportOverrideConstants.ClientPeriodFormatKeyPrefix}{f.FormatKey}", FamilyClientPeriod)));
        return result;
    }

    public bool TryResolve(string formatKey, out string family)
    {
        var match = GetAll().FirstOrDefault(e => e.FormatKey == formatKey);
        family = match.Family ?? string.Empty;
        return match.FormatKey is not null;
    }
}
