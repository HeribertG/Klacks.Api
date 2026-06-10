// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists client availability entries (per client, date and hour granularity) for a date range via
/// ListClientAvailabilitiesQuery. Use before planning to see when employees declared themselves
/// available or unavailable; entries are written with set_client_availability.
/// </summary>
/// <param name="startDate">Range start in ISO yyyy-MM-dd (required, inclusive).</param>
/// <param name="endDate">Range end in ISO yyyy-MM-dd (required, inclusive).</param>

using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_client_availabilities")]
public class ListClientAvailabilitiesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListClientAvailabilitiesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");

        if (endDate < startDate)
        {
            return SkillResult.Error("Parameter 'endDate' must not be before 'startDate'.");
        }

        var availabilities = (await _mediator.Send(
            new ListClientAvailabilitiesQuery(startDate, endDate), cancellationToken)).ToList();

        return SkillResult.SuccessResult(
            new
            {
                Count = availabilities.Count,
                StartDate = startDate,
                EndDate = endDate,
                Availabilities = availabilities
            },
            $"Found {availabilities.Count} availability entries between {startDate} and {endDate}.");
    }
}
