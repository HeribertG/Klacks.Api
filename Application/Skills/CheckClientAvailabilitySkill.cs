// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only advisory skill that answers "is this client deployable on that day / in that hour
/// window?" by applying the REAL planning semantics: booked absences (Breaks) override schedule
/// keywords, keywords override availability, and availability itself is opt-in per date (no record
/// = fully open; a day with available-hour records restricts work to exactly those hours; an
/// only-false day blocks just those hours — legacy negative data).
/// </summary>
/// <param name="clientId">UUID of the client to check</param>
/// <param name="date">The day to check (yyyy-MM-dd)</param>
/// <param name="startHour">Optional first hour of the window (0-23, default 0)</param>
/// <param name="endHour">Optional last hour of the window (0-23 inclusive, default 23)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_client_availability")]
public class CheckClientAvailabilitySkill : BaseSkillImplementation
{
    private const int MinHour = 0;
    private const int MaxHour = 23;

    private readonly IClientRepository _clientRepository;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IScheduleCommandRepository _scheduleCommandRepository;

    public CheckClientAvailabilitySkill(
        IClientRepository clientRepository,
        IClientAvailabilityRepository availabilityRepository,
        IBreakRepository breakRepository,
        IScheduleCommandRepository scheduleCommandRepository)
    {
        _clientRepository = clientRepository;
        _availabilityRepository = availabilityRepository;
        _breakRepository = breakRepository;
        _scheduleCommandRepository = scheduleCommandRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var startHour = GetParameter<int?>(parameters, "startHour") ?? MinHour;
        var endHour = GetParameter<int?>(parameters, "endHour") ?? MaxHour;

        if (startHour < MinHour || endHour > MaxHour || startHour > endHour)
        {
            return SkillResult.Error(
                $"Invalid hour window: 'startHour' and 'endHour' must be between {MinHour} and {MaxHour} and 'startHour' must not exceed 'endHour'.");
        }

        var client = await _clientRepository.Get(clientId);
        if (client is null)
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var clientName = $"{client.FirstName} {client.Name}".Trim();
        var window = $"{date:yyyy-MM-dd} hours {startHour}-{endHour}";

        var bookedAbsences = await _breakRepository.GetByClientAndDateRangeAsync(clientId, date, date, cancellationToken);
        var commands = await _scheduleCommandRepository.GetByClientsAndDateRangeAsync(
            [clientId], date, date, null, cancellationToken);
        var keywords = commands
            .Where(c => ScheduleCommandKeywordMapper.TryMap(c.CommandKeyword, out _))
            .Select(c => c.CommandKeyword.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var entries = await _availabilityRepository.GetByClientAndDateRange(clientId, date, date);
        var availableHours = entries.Where(e => e.IsAvailable).Select(e => e.Hour).Distinct().OrderBy(h => h).ToList();
        var unavailableHours = entries.Where(e => !e.IsAvailable).Select(e => e.Hour).Distinct().OrderBy(h => h).ToList();

        var dayMode = entries.Count == 0
            ? "open"
            : availableHours.Count > 0 ? "positive" : "legacy-negative";

        var windowStart = new TimeOnly(startHour, 0);
        var windowEnd = endHour == MaxHour ? new TimeOnly(23, 59) : new TimeOnly(endHour + 1, 0);
        var availabilityBlocks = AvailabilityMatcher.IsUnavailable(entries, date, windowStart, windowEnd);

        string governingLayer;
        bool deployable;
        string verdictText;

        if (bookedAbsences.Count > 0)
        {
            governingLayer = "absence";
            deployable = false;
            verdictText = $"{clientName} has a booked absence on {date:yyyy-MM-dd} — not deployable; availability records are irrelevant that day.";
        }
        else if (keywords.Count > 0)
        {
            governingLayer = "keyword";
            deployable = !keywords.Contains("FREE");
            verdictText = $"{clientName}'s {date:yyyy-MM-dd} is governed by schedule keyword(s) {string.Join(", ", keywords)} — " +
                          "keywords override availability records entirely; deployability follows the keyword.";
        }
        else if (availabilityBlocks)
        {
            governingLayer = "availability";
            deployable = false;
            verdictText = dayMode == "positive"
                ? $"{clientName} is NOT deployable for {window}: the day is positively marked, only hours {string.Join(",", availableHours)} are usable."
                : $"{clientName} is NOT deployable for {window}: hours {string.Join(",", unavailableHours)} are explicitly blocked.";
        }
        else
        {
            governingLayer = dayMode == "open" ? "open" : "availability";
            deployable = true;
            verdictText = dayMode == "open"
                ? $"{clientName} has no availability restriction on {date:yyyy-MM-dd} — the whole day is open for planning."
                : $"{clientName} is deployable for {window} (the requested hours are within the declared availability).";
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                ClientName = clientName,
                Date = date,
                StartHour = startHour,
                EndHour = endHour,
                Deployable = deployable,
                GoverningLayer = governingLayer,
                DayMode = dayMode,
                AvailableHours = availableHours,
                UnavailableHours = unavailableHours,
                Keywords = keywords,
                BookedAbsenceIds = bookedAbsences.Select(b => b.Id).ToList()
            },
            verdictText);
    }
}
