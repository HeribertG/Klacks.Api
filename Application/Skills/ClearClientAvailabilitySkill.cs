// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that removes availability records of a client in a date range, optionally narrowed to an
/// hour window, returning those slots to the default "fully open for planning" state — the
/// counterpart of set_client_availability, since a positively marked day restricts work to the
/// marked hours and only deleting the records fully reopens it. The soft-delete is self-verifying:
/// it runs in a transaction and the range must read back empty before success is reported.
/// </summary>
/// <param name="clientId">UUID of the client whose availability records are cleared</param>
/// <param name="startDate">First day of the range (yyyy-MM-dd)</param>
/// <param name="endDate">Optional last day of the range (yyyy-MM-dd, defaults to startDate)</param>
/// <param name="startHour">Optional first hour of the window to clear (0-23); defaults to 0 (whole day)</param>
/// <param name="endHour">Optional last hour of the window to clear (0-23, inclusive); defaults to 23 (whole day)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class ClearClientAvailabilitySkill : BaseSkillImplementation
{
    private const string SkillName = "clear_client_availability";
    private const int MinHour = 0;
    private const int MaxHour = 23;
    private const int MaxRangeDays = 92;

    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClearClientAvailabilitySkill(
        IClientAvailabilityRepository availabilityRepository,
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork)
    {
        _availabilityRepository = availabilityRepository;
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate") ?? startDate;
        var startHour = GetParameter<int?>(parameters, "startHour") ?? MinHour;
        var endHour = GetParameter<int?>(parameters, "endHour") ?? MaxHour;

        if (endDate < startDate)
        {
            return SkillResult.Error("Parameter 'endDate' must not be before 'startDate'.");
        }

        if (endDate.DayNumber - startDate.DayNumber + 1 > MaxRangeDays)
        {
            return SkillResult.Error($"Date range too large: maximum {MaxRangeDays} days per call.");
        }

        if (startHour < MinHour || endHour > MaxHour || startHour > endHour)
        {
            return SkillResult.Error(
                $"Invalid hour window: 'startHour' and 'endHour' must be between {MinHour} and {MaxHour} and 'startHour' must not exceed 'endHour'.");
        }

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var entries = (await _availabilityRepository.GetByClientAndDateRange(clientId, startDate, endDate))
            .Where(e => e.Hour >= startHour && e.Hour <= endHour)
            .ToList();
        if (entries.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, StartDate = startDate, EndDate = endDate, StartHour = startHour, EndHour = endHour, RemovedCount = 0 },
                $"No availability records exist for client {clientId} between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd} " +
                $"for hours {startHour}-{endHour} — that window is already fully open.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var entry in entries)
            {
                _availabilityRepository.Remove(entry);
            }

            await _unitOfWork.CompleteAsync();

            var remaining = (await _availabilityRepository.GetByClientAndDateRange(clientId, startDate, endDate))
                .Where(e => e.Hour >= startHour && e.Hour <= endHour)
                .ToList();
            if (remaining.Count > 0)
            {
                throw new Domain.Exceptions.SkillVerificationException(
                    SkillName,
                    $"Database verification failed: {remaining.Count} availability record(s) for client {clientId} " +
                    $"in hours {startHour}-{endHour} are still visible after the delete — the change was rolled back.");
            }

            return true;
        });

        return SkillResult.SuccessResult(
            new { ClientId = clientId, StartDate = startDate, EndDate = endDate, StartHour = startHour, EndHour = endHour, RemovedCount = entries.Count },
            $"Removed {entries.Count} availability record(s) for client {clientId} between {startDate:yyyy-MM-dd} " +
            $"and {endDate:yyyy-MM-dd} for hours {startHour}-{endHour} and confirmed the window is empty (verified) " +
            "— those hours are fully open for planning again.");
    }
}
