// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Navigates the Klacks UI to the order export card on the period-closing page. The export file
/// is generated server-side but streamed as a direct browser download, so the chat cannot hand
/// out the file itself; this skill returns a navigate UiAction and the user completes order,
/// format and download selection in the UI. Validates a requested format key early so an
/// unsupported format fails in the chat instead of in the UI.
/// </summary>
/// <param name="format">Optional. Desired export format key (csv, json, xml, datev, bmd).</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("open_order_export")]
public class OpenOrderExportSkill : BaseSkillImplementation
{
    private const string PeriodClosingRoute = "/workplace/period-closing";
    private const string ExportsTarget = "period-closing-exports-tab";

    private static readonly string[] SupportedFormats =
    [
        ExportConstants.FormatCsv,
        ExportConstants.FormatJson,
        ExportConstants.FormatXml,
        ExportConstants.FormatDatev,
        ExportConstants.FormatBmd
    ];

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var format = GetParameter<string>(parameters, "format");
        string? normalizedFormat = null;

        if (!string.IsNullOrWhiteSpace(format))
        {
            normalizedFormat = format.Trim().ToLowerInvariant();
            if (!SupportedFormats.Contains(normalizedFormat))
            {
                return Task.FromResult(SkillResult.Error(
                    $"Unsupported export format '{format}'. Supported formats: {string.Join(", ", SupportedFormats)}."));
            }
        }

        var message = normalizedFormat == null
            ? "Opened the period-closing page. Please select the sealed order and export format in the Exports card to download the file."
            : $"Opened the period-closing page. Please select the sealed order in the Exports card and download it as {normalizedFormat}.";

        return Task.FromResult(SkillResult.Navigation(
            new
            {
                Route = PeriodClosingRoute,
                Target = ExportsTarget,
                Format = normalizedFormat
            },
            message));
    }
}
