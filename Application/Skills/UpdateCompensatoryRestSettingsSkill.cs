// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes how time off owed for work on a protected day is handled. Only the supplied parameters
/// are written; the rest keep their stored value.
/// </summary>
/// <param name="enabled">Whether the claim is tracked at all.</param>
/// <param name="deadlineDays">Days within which the time off has to be granted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_compensatory_rest_settings")]
public class UpdateCompensatoryRestSettingsSkill : SettingsWriterSkillBase
{
    public UpdateCompensatoryRestSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var pending = new List<PendingSetting>();

        CollectBoolean(pending, parameters, "enabled", SettingKeys.ComplianceCompensatoryRestEnabled);
        CollectInteger(pending, parameters, "deadlineDays", SettingKeys.ComplianceCompensatoryRestDeadlineDays);

        return await PersistAsync(pending, "Compensatory rest settings");
    }
}
