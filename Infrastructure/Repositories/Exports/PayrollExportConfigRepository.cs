// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the installation-wide payroll-export configuration. The default target system is taken from the
/// DEFAULT_PAYROLL_TARGET_SYSTEM setting, validated against the format keys of the registered payroll formatters
/// (canonical casing wins), and falls back to DATEV Lohn &amp; Gehalt when the setting is missing, blank or unknown.
/// </summary>
/// <param name="settingsReader">Read-only settings access used to resolve the default target system</param>
/// <param name="formatters">Registered payroll formatters whose FormatKey values validate the setting</param>
/// <param name="logger">Logger for warnings about unknown target-system setting values</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

public class PayrollExportConfigRepository : IPayrollExportConfigRepository
{
    private readonly ISettingsReader _settingsReader;
    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly ILogger<PayrollExportConfigRepository> _logger;

    public PayrollExportConfigRepository(
        ISettingsReader settingsReader,
        IEnumerable<IPayrollExportFormatter> formatters,
        ILogger<PayrollExportConfigRepository> logger)
    {
        _settingsReader = settingsReader;
        _formatters = formatters;
        _logger = logger;
    }

    public async Task<PayrollExportGroupConfig> GetAsync(CancellationToken cancellationToken = default)
    {
        return new PayrollExportGroupConfig
        {
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
