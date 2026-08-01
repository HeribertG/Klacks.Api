// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores the adjustment for one export format via SaveExportFormatOverrideCommand. This is an
/// upsert: the same call creates the entry the first time and replaces it afterwards. An unknown
/// formatKey is answered with the keys that actually exist instead of a bare rejection, and
/// patchJson is parsed before sending so a malformed body fails with a readable message.
/// </summary>
/// <param name="formatKey">Key of the export format to adjust (required).</param>
/// <param name="patchJson">JSON object with the fields that deviate from the delivered definition.</param>
/// <param name="isEnabled">Whether the adjustment takes effect; defaults to true.</param>
/// <param name="note">Optional remark explaining why the adjustment exists.</param>

using System.Text.Json;
using Klacks.Api.Application.Commands.Exports;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_export_format_override")]
public class SetExportFormatOverrideSkill : BaseSkillImplementation
{
    private const string EmptyPatch = "{}";

    private readonly IMediator _mediator;

    public SetExportFormatOverrideSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var formatKey = GetParameter<string>(parameters, "formatKey")?.Trim();
        if (string.IsNullOrWhiteSpace(formatKey))
        {
            return SkillResult.Error("Parameter 'formatKey' is required.");
        }

        var catalog = await _mediator.Send(new ListExportFormatOverridesQuery(), cancellationToken);
        var format = catalog.Formats
            .FirstOrDefault(f => string.Equals(f.FormatKey, formatKey, StringComparison.OrdinalIgnoreCase));

        if (format == null)
        {
            return SkillResult.Error(
                $"Unknown formatKey '{formatKey}'. Available formats: " +
                string.Join(", ", catalog.Formats.Select(f => f.FormatKey)));
        }

        var patchJson = GetParameter<string>(parameters, "patchJson")?.Trim();
        if (string.IsNullOrWhiteSpace(patchJson))
        {
            patchJson = EmptyPatch;
        }

        try
        {
            using var parsed = JsonDocument.Parse(patchJson);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object)
            {
                return SkillResult.Error("patchJson must be a JSON object, for example {\"delimiter\": \";\"}.");
            }
        }
        catch (JsonException exception)
        {
            return SkillResult.Error($"patchJson is not valid JSON: {exception.Message}");
        }

        var isEnabled = GetParameter<bool?>(parameters, "isEnabled") ?? true;
        var note = GetParameter<string>(parameters, "note")?.Trim();

        ExportFormatOverrideResource saved;
        try
        {
            saved = await _mediator.Send(
                new SaveExportFormatOverrideCommand(format.FormatKey, patchJson, isEnabled, note), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        var existedBefore = format.Override != null;

        return SkillResult.SuccessResult(
            new { saved.FormatKey, saved.PatchJson, saved.IsEnabled, saved.Note, saved.CreatedUnderVersion },
            existedBefore
                ? $"Adjustment for export format '{saved.FormatKey}' replaced."
                : $"Adjustment for export format '{saved.FormatKey}' stored.");
    }
}
