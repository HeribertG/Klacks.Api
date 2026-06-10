// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets a client's availability (available/unavailable) per day and hour via
/// BulkUpdateClientAvailabilityCommand. The backend upserts on (clientId, date, hour), so calling
/// the skill again for the same slots overwrites the previous value. Accepts either an explicit
/// list of single days or a date range, plus an optional hour window (defaults to the whole day).
/// </summary>
/// <param name="clientId">UUID of the client (required).</param>
/// <param name="dates">Optional comma-separated single days in ISO yyyy-MM-dd; takes precedence over startDate/endDate.</param>
/// <param name="startDate">Range start in ISO yyyy-MM-dd (required when 'dates' is not given).</param>
/// <param name="endDate">Optional range end in ISO yyyy-MM-dd; defaults to startDate.</param>
/// <param name="startHour">Optional first hour of day (0-23); defaults to 0.</param>
/// <param name="endHour">Optional last hour of day (0-23, inclusive); defaults to 23.</param>
/// <param name="isAvailable">true = available, false = unavailable (required).</param>

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_client_availability")]
public class SetClientAvailabilitySkill : BaseSkillImplementation
{
    private const int MinHour = 0;
    private const int MaxHour = 23;
    private const int MaxRangeDays = 92;
    private const char DateListSeparator = ',';

    private readonly IMediator _mediator;

    public SetClientAvailabilitySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var isAvailable = GetParameter<bool?>(parameters, "isAvailable")
            ?? throw new ArgumentException("Required parameter 'isAvailable' is missing");
        var startHour = GetParameter<int?>(parameters, "startHour") ?? MinHour;
        var endHour = GetParameter<int?>(parameters, "endHour") ?? MaxHour;

        if (startHour < MinHour || endHour > MaxHour || startHour > endHour)
        {
            return SkillResult.Error(
                $"Invalid hour window: 'startHour' and 'endHour' must be between {MinHour} and {MaxHour} and 'startHour' must not exceed 'endHour'.");
        }

        List<DateOnly> days;
        var datesRaw = GetParameter<string>(parameters, "dates");
        if (!string.IsNullOrWhiteSpace(datesRaw))
        {
            days = [];
            foreach (var token in datesRaw.Split(DateListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!DateOnly.TryParse(token, out var parsed))
                {
                    return SkillResult.Error($"Invalid date '{token}' in parameter 'dates'. Use ISO format yyyy-MM-dd.");
                }

                days.Add(parsed);
            }

            days = days.Distinct().OrderBy(d => d).ToList();
        }
        else
        {
            var startDate = GetParameter<DateOnly?>(parameters, "startDate")
                ?? throw new ArgumentException("Required parameter 'startDate' is missing when 'dates' is not given");
            var endDate = GetParameter<DateOnly?>(parameters, "endDate") ?? startDate;

            if (endDate < startDate)
            {
                return SkillResult.Error("Parameter 'endDate' must not be before 'startDate'.");
            }

            var rangeDays = endDate.DayNumber - startDate.DayNumber + 1;
            if (rangeDays > MaxRangeDays)
            {
                return SkillResult.Error($"Date range too large: maximum {MaxRangeDays} days per call.");
            }

            days = Enumerable.Range(0, rangeDays)
                .Select(startDate.AddDays)
                .ToList();
        }

        if (days.Count == 0)
        {
            return SkillResult.Error("No valid days supplied — provide 'dates' or 'startDate'/'endDate'.");
        }

        var items = days
            .SelectMany(day => Enumerable.Range(startHour, endHour - startHour + 1)
                .Select(hour => new ClientAvailabilityResource
                {
                    ClientId = clientId,
                    Date = day,
                    Hour = hour,
                    IsAvailable = isAvailable
                }))
            .ToList();

        var updatedCount = await _mediator.Send(
            new BulkUpdateClientAvailabilityCommand(new ClientAvailabilityBulkRequest { Items = items }),
            cancellationToken);

        var availabilityLabel = isAvailable ? "available" : "unavailable";

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                Days = days,
                StartHour = startHour,
                EndHour = endHour,
                IsAvailable = isAvailable,
                UpdatedCount = updatedCount
            },
            $"Marked client {clientId} as {availabilityLabel} for {days.Count} day(s), hours {startHour}-{endHour} ({updatedCount} slots written).");
    }
}
