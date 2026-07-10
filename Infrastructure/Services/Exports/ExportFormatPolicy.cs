// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the combined order+payroll export format catalog from the registered formatters of
/// both families and the ENABLED_EXPORT_FORMATS setting. Fixed formats are always enabled; when
/// no setting exists yet, every optional format (order or payroll) is treated as enabled to
/// preserve prior behaviour.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ExportFormatPolicy : IExportFormatPolicy
{
    private readonly IEnumerable<IExportFormatter> _orderFormatters;
    private readonly IEnumerable<IPayrollExportFormatter> _payrollFormatters;
    private readonly ISettingsReader _settingsReader;

    public ExportFormatPolicy(
        IEnumerable<IExportFormatter> orderFormatters,
        IEnumerable<IPayrollExportFormatter> payrollFormatters,
        ISettingsReader settingsReader)
    {
        _orderFormatters = orderFormatters;
        _payrollFormatters = payrollFormatters;
        _settingsReader = settingsReader;
    }

    public async Task<IReadOnlyList<ExportFormatResource>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var enabledOptional = await GetEnabledOptionalKeysAsync();

        return AllFormatKeys()
            .Select(key => new ExportFormatResource
            {
                Key = key,
                Fixed = IsFixed(key),
                Enabled = IsFixed(key) || enabledOptional.Contains(key)
            })
            .ToList();
    }

    public async Task<bool> IsEnabledAsync(string formatKey, CancellationToken cancellationToken)
    {
        if (IsFixed(formatKey))
        {
            return true;
        }

        var enabledOptional = await GetEnabledOptionalKeysAsync();
        return enabledOptional.Contains(formatKey);
    }

    private async Task<HashSet<string>> GetEnabledOptionalKeysAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.EnabledExportFormats);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return AllFormatKeys().Where(key => !IsFixed(key)).ToHashSet();
        }

        return setting.Value
            .Split(ExportConstants.EnabledFormatSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();
    }

    private IEnumerable<string> AllFormatKeys()
    {
        return _orderFormatters.Select(f => f.FormatKey)
            .Concat(_payrollFormatters.Select(f => f.FormatKey));
    }

    private static bool IsFixed(string formatKey) => ExportConstants.FixedFormatKeys.Contains(formatKey);
}
