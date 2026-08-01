// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes which optional export formats are switched on and which payroll system is the default
/// target. Only the supplied parameters are written; the rest keep their stored value.
/// </summary>
/// <param name="enabledExportFormats">Comma-separated keys of the optional formats to switch on.</param>
/// <param name="defaultPayrollTargetSystem">Payroll system figures are handed over to by default.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_export_formats_settings")]
public class UpdateExportFormatsSettingsSkill : SettingsWriterSkillBase
{
    public UpdateExportFormatsSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
        : base(settingsRepository, unitOfWork)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var pending = new List<PendingSetting>();

        CollectText(pending, parameters, "enabledExportFormats", SettingKeys.EnabledExportFormats);
        CollectText(pending, parameters, "defaultPayrollTargetSystem", SettingKeys.DefaultPayrollTargetSystem);

        return await PersistAsync(pending, "Export format settings");
    }
}
