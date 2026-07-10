// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the enabled override row for a format key and applies its patch over the runtime parameter
/// object. A broken patch never fails the export: it is logged and skipped, so a bad support hotfix
/// degrades to default behaviour instead of blocking the customer's export.
/// </summary>
/// <param name="repository">Override rows per format key</param>
/// <param name="logger">Logger for diagnostic output</param>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Exports;

public class ExportFormatOverrideApplier : IExportFormatOverrideApplier
{
    private readonly IExportFormatOverrideRepository _repository;
    private readonly ILogger<ExportFormatOverrideApplier> _logger;

    public ExportFormatOverrideApplier(IExportFormatOverrideRepository repository, ILogger<ExportFormatOverrideApplier> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<bool> ApplyAsync(string formatKey, ExportOptions options, CancellationToken cancellationToken = default)
    {
        return ApplyCoreAsync(formatKey, patch => patch.ApplyTo(options), cancellationToken);
    }

    public Task<bool> ApplyAsync(string formatKey, PayrollExportGroupConfig config, CancellationToken cancellationToken = default)
    {
        return ApplyCoreAsync(formatKey, patch => patch.ApplyTo(config), cancellationToken);
    }

    private async Task<bool> ApplyCoreAsync(string formatKey, Action<ExportFormatOverridePatch> apply, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByFormatKeyAsync(formatKey, cancellationToken);
        if (entry is null || !entry.IsEnabled)
        {
            return false;
        }

        try
        {
            var patch = ExportFormatOverridePatch.Parse(entry.PatchJson);
            apply(patch);
            _logger.LogInformation("Export format override applied for '{FormatKey}' (created under version {Version}).", formatKey, entry.CreatedUnderVersion);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export format override for '{FormatKey}' could not be applied; exporting with defaults.", formatKey);
            return false;
        }
    }
}
