// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports the aggregate progress of the background group-geocoding queue: how many groups already
/// have coordinates, how many were classified but stayed without one (not a place, or the lookup
/// failed), and how many were never actually processed yet. Read-only, no parameters.
/// </summary>

using Klacks.Api.Application.Queries.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_group_geocoding_status")]
public class CheckGroupGeocodingStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CheckGroupGeocodingStatusSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetGroupGeocodingStatusQuery(), cancellationToken);

        var message =
            $"{status.WithCoordinates} of {status.TotalGroups} group(s) have a location. " +
            $"{status.AttemptedNotAPlaceOrFailed} were checked and are not a real place or could not be " +
            $"geocoded — those stay without coordinates on purpose. " +
            (status.Pending > 0
                ? $"{status.Pending} group(s) have not been processed yet; re-run the bulk geocoding to " +
                  "work through them."
                : "None are still pending.");

        return SkillResult.SuccessResult(status, message);
    }
}
