// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only advisory skill that summarises a client's absences for one year, mirroring the value
/// column of the absence gantt: per absence type it reports planned days (placeholders, clipped to
/// the year), booked days (Breaks) and the weighted value (days x the type's DefaultValue).
/// </summary>
/// <param name="clientId">UUID of the client to summarise</param>
/// <param name="year">Calendar year to summarise (default: current year)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_client_absence_summary")]
public class GetClientAbsenceSummarySkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IAbsenceRepository _absenceRepository;

    public GetClientAbsenceSummarySkill(
        IClientRepository clientRepository,
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IBreakRepository breakRepository,
        IAbsenceRepository absenceRepository)
    {
        _clientRepository = clientRepository;
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _breakRepository = breakRepository;
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var year = GetParameter<int?>(parameters, "year") ?? DateTime.UtcNow.Year;

        var client = await _clientRepository.Get(clientId);
        if (client is null)
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);
        var yearStartUtc = yearStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var yearEndUtc = yearEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var absences = await _absenceRepository.List();
        var catalogue = absences.Where(a => !a.IsDeleted).ToDictionary(a => a.Id);

        var placeholders = await _breakPlaceholderRepository
            .GetByClientAndRangeAsync(clientId, yearStartUtc, yearEndUtc, cancellationToken);
        var booked = await _breakRepository
            .GetByClientAndDateRangeAsync(clientId, yearStart, yearEnd, cancellationToken);

        var plannedDaysByType = placeholders
            .GroupBy(bp => bp.AbsenceId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(bp => ClippedDays(bp.From, bp.Until, yearStartUtc, yearEndUtc)));

        var bookedDaysByType = booked
            .GroupBy(b => b.AbsenceId)
            .ToDictionary(g => g.Key, g => g.Select(b => b.CurrentDate).Distinct().Count());

        var typeIds = plannedDaysByType.Keys.Union(bookedDaysByType.Keys).ToList();

        var rows = typeIds
            .Select(id =>
            {
                var plannedDays = plannedDaysByType.GetValueOrDefault(id);
                var bookedDays = bookedDaysByType.GetValueOrDefault(id);
                var defaultValue = catalogue.TryGetValue(id, out var absence) ? absence.DefaultValue : 0;
                return new
                {
                    AbsenceType = catalogue.TryGetValue(id, out var a) ? Label(a.Name) : "Unknown",
                    PlannedDays = plannedDays,
                    BookedDays = bookedDays,
                    TotalDays = plannedDays + bookedDays,
                    Value = (plannedDays + bookedDays) * defaultValue
                };
            })
            .OrderByDescending(r => r.TotalDays)
            .ToList();

        var clientName = $"{client.FirstName} {client.Name}".Trim();
        var totalDays = rows.Sum(r => r.TotalDays);

        var message = rows.Count == 0
            ? $"{clientName} has no absences in {year}."
            : $"{clientName} has {totalDays} absence day(s) in {year} across {rows.Count} type(s): " +
              string.Join(", ", rows.Select(r =>
                  $"{r.AbsenceType} {r.TotalDays}d ({r.PlannedDays} planned / {r.BookedDays} booked)")) +
              ". Planned = placeholder wish in the absence calendar; booked = placed in the schedule.";

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                ClientName = clientName,
                Year = year,
                TotalDays = totalDays,
                ByType = rows
            },
            message);
    }

    private static int ClippedDays(DateTime from, DateTime until, DateTime windowStart, DateTime windowEnd)
    {
        var start = from > windowStart ? from : windowStart;
        var end = until < windowEnd ? until : windowEnd;
        return end < start ? 0 : (int)(end.Date - start.Date).TotalDays + 1;
    }

    private static string Label(MultiLanguage name) =>
        new[] { name.De, name.En, name.Fr, name.It }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "absence";
}
