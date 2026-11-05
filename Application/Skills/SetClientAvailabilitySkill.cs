// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets a client's availability (available/unavailable) per day and hour via
/// BulkUpdateClientAvailabilityCommand. The backend upserts on (clientId, date, hour), so calling
/// the skill again for the same slots overwrites the previous value. Accepts either an explicit
/// list of single days or a date range, plus an optional hour window (defaults to the whole day).
/// The write is verified by re-reading the affected range and recounting the persisted slots.
/// </summary>
/// <param name="clientId">UUID of the client; optional when firstName and lastName name the person.</param>
/// <param name="firstName">Optional first name of the person, used together with lastName when no clientId is known.</param>
/// <param name="lastName">Optional last name of the person; several people with the same name are reported with their client numbers.</param>
/// <param name="idNumber">Optional client number that picks one person when first and last name match several.</param>
/// <param name="dates">Optional comma-separated single days in ISO yyyy-MM-dd; takes precedence over startDate/endDate.</param>
/// <param name="startDate">Range start in ISO yyyy-MM-dd (required when 'dates' is not given).</param>
/// <param name="endDate">Optional range end in ISO yyyy-MM-dd; defaults to startDate.</param>
/// <param name="startHour">Optional first hour of day (0-23); defaults to 0.</param>
/// <param name="endHour">Optional last hour of day (0-23, inclusive); defaults to 23.</param>
/// <param name="isAvailable">true = available, false = unavailable (required).</param>

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
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
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public SetClientAvailabilitySkill(
        IMediator mediator,
        IClientAvailabilityRepository availabilityRepository,
        IClientRepository clientRepository,
        IClientSearchRepository clientSearchRepository,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _mediator = mediator;
        _availabilityRepository = availabilityRepository;
        _clientRepository = clientRepository;
        _clientSearchRepository = clientSearchRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
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
                if (!SkillCalendarStringParser.TryParseDateOnly(token, context.UserLanguage, out var parsed))
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

        var (resolvedClientId, clientError) = await ClientResolver.ResolveIdByIdOrNameAsync(
            _clientSearchRepository, _clientRepository, _clientVisibilityGuard,
            GetParameter<string>(parameters, ClientResolver.ClientIdParameterName),
            GetParameter<string>(parameters, ClientResolver.FirstNameParameterName),
            GetParameter<string>(parameters, ClientResolver.LastNameParameterName),
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName),
            cancellationToken);
        if (resolvedClientId == null)
        {
            return SkillResult.Error(clientError!);
        }

        var clientId = resolvedClientId.Value;

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

        var requestedDays = days.ToHashSet();
        var persisted = await _availabilityRepository.GetByClientAndDateRange(clientId, days.Min(), days.Max());
        var persistedLookup = persisted
            .Where(e => requestedDays.Contains(e.Date))
            .GroupBy(e => (e.Date, e.Hour))
            .ToDictionary(g => g.Key, g => g.First().IsAvailable);
        var verifiedCount = items.Count(i => persistedLookup.TryGetValue((i.Date, i.Hour), out var v) && v == isAvailable);
        var mismatchCount = items.Count - verifiedCount;

        var availabilityLabel = isAvailable ? "available" : "unavailable";
        var verifyNote = mismatchCount > 0
            ? $" WARNING: only {verifiedCount} of {items.Count} slots could be confirmed in the database."
            : string.Empty;
        var semanticsNote = isAvailable
            ? " Note: on a positively marked day only the marked hours are usable for planning; days without records stay fully open. Use clear_client_availability to fully reopen a day."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                Days = days,
                StartHour = startHour,
                EndHour = endHour,
                IsAvailable = isAvailable,
                UpdatedCount = updatedCount,
                VerifiedCount = verifiedCount
            },
            $"Marked client {clientId} as {availabilityLabel} for {days.Count} day(s), hours {startHour}-{endHour} " +
            $"and confirmed {verifiedCount} of {items.Count} slots in the database (verified).{verifyNote}{semanticsNote}");
    }
}
