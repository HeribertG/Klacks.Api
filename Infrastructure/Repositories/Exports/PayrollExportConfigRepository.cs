// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the per-group payroll-export configuration from the database. When no row exists for the
/// group, the default target system is taken from the DEFAULT_PAYROLL_TARGET_SYSTEM setting,
/// validated against the format keys of the registered payroll formatters (canonical casing wins),
/// and falls back to DATEV Lohn &amp; Gehalt when the setting is missing, blank or unknown.
/// </summary>
/// <param name="context">Database context providing the PayrollExportGroupConfig set</param>
/// <param name="settingsReader">Read-only settings access used to resolve the default target system</param>
/// <param name="formatters">Registered payroll formatters whose FormatKey values validate the setting</param>
/// <param name="logger">Logger for warnings about unknown target-system setting values</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

public class PayrollExportConfigRepository : IPayrollExportConfigRepository
{
    private readonly DataBaseContext _context;
    private readonly ISettingsReader _settingsReader;
    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly ILogger<PayrollExportConfigRepository> _logger;

    public PayrollExportConfigRepository(
        DataBaseContext context,
        ISettingsReader settingsReader,
        IEnumerable<IPayrollExportFormatter> formatters,
        ILogger<PayrollExportConfigRepository> logger)
    {
        _context = context;
        _settingsReader = settingsReader;
        _formatters = formatters;
        _logger = logger;
    }

    public async Task<PayrollExportGroupConfig> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var config = await _context.PayrollExportGroupConfig
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken);

        return config ?? await CreateDefaultAsync(groupId);
    }

    private async Task<PayrollExportGroupConfig> CreateDefaultAsync(Guid groupId)
    {
        return new PayrollExportGroupConfig
        {
            GroupId = groupId,
            TargetSystem = await ResolveDefaultTargetSystemAsync(),
            Delimiter = PayrollExportConstants.DefaultDelimiter,
            Encoding = PayrollExportConstants.DefaultEncoding,
            BaseWageType = string.Empty,
            SurchargeWageType = string.Empty,
            AbsenceMappingJson = "{}",
        };
    }

    private async Task<string> ResolveDefaultTargetSystemAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.DefaultPayrollTargetSystem);
        var configuredValue = setting?.Value?.Trim();

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return PayrollExportConstants.FormatKeyDatevLug;
        }

        var formatter = _formatters.FirstOrDefault(
            f => string.Equals(f.FormatKey, configuredValue, StringComparison.OrdinalIgnoreCase));

        if (formatter is null)
        {
            _logger.LogWarning(
                "Setting {SettingKey} contains unknown payroll target system '{TargetSystem}'; falling back to '{FallbackTargetSystem}'.",
                SettingKeys.DefaultPayrollTargetSystem,
                configuredValue,
                PayrollExportConstants.FormatKeyDatevLug);
            return PayrollExportConstants.FormatKeyDatevLug;
        }

        return formatter.FormatKey;
    }
}
