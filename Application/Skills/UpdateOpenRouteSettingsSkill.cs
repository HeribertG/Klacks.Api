// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes the travel-time settings: the key for the routing service and the shortest journey worth
/// counting per means of travel. Only the supplied parameters are written.
/// </summary>
/// <param name="apiKey">Key for the routing service.</param>
/// <param name="minTravelTimeByCar">Shortest journey by car worth counting, in minutes.</param>
/// <param name="minTravelTimeByBicycle">Shortest journey by bicycle worth counting, in minutes.</param>
/// <param name="minTravelTimeByFoot">Shortest journey on foot worth counting, in minutes.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_openroute_settings")]
public class UpdateOpenRouteSettingsSkill : SettingsWriterSkillBase
{
    public UpdateOpenRouteSettingsSkill(
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

        CollectText(pending, parameters, "apiKey", Settings.OPENROUTESERVICE_API_KEY);
        CollectInteger(pending, parameters, "minTravelTimeByCar", Settings.ROUTE_MIN_TRAVEL_TIME_BY_CAR);
        CollectInteger(pending, parameters, "minTravelTimeByBicycle", Settings.ROUTE_MIN_TRAVEL_TIME_BY_BICYCLE);
        CollectInteger(pending, parameters, "minTravelTimeByFoot", Settings.ROUTE_MIN_TRAVEL_TIME_BY_FOOT);

        return await PersistAsync(pending, "Travel time settings");
    }
}
