// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that removes ALL availability records of a client in a date range, returning those days to
/// the default "fully open for planning" state — the counterpart of set_client_availability, since
/// a positively marked day restricts work to the marked hours and only deleting the records fully
/// reopens it. The soft-delete is self-verifying: it runs in a transaction and the range must read
/// back empty before success is reported.
/// </summary>
/// <param name="clientId">UUID of the client whose availability records are cleared</param>
/// <param name="startDate">First day of the range (yyyy-MM-dd)</param>
/// <param name="endDate">Optional last day of the range (yyyy-MM-dd, defaults to startDate)</param>

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

        if (endDate < startDate)
        {
            return SkillResult.Error("Parameter 'endDate' must not be before 'startDate'.");
        }

        if (endDate.DayNumber - startDate.DayNumber + 1 > MaxRangeDays)
        {
            return SkillResult.Error($"Date range too large: maximum {MaxRangeDays} days per call.");
        }

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var entries = await _availabilityRepository.GetByClientAndDateRange(clientId, startDate, endDate);
        if (entries.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, StartDate = startDate, EndDate = endDate, RemovedCount = 0 },
                $"No availability records exist for client {clientId} between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd} — those days are already fully open.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var entry in entries)
            {
                _availabilityRepository.Remove(entry);
            }

            await _unitOfWork.CompleteAsync();

            var remaining = await _availabilityRepository.GetByClientAndDateRange(clientId, startDate, endDate);
            if (remaining.Count > 0)
            {
                throw new Domain.Exceptions.SkillVerificationException(
                    SkillName,
                    $"Database verification failed: {remaining.Count} availability record(s) for client {clientId} " +
                    "are still visible after the delete — the change was rolled back.");
            }

            return true;
        });

        return SkillResult.SuccessResult(
            new { ClientId = clientId, StartDate = startDate, EndDate = endDate, RemovedCount = entries.Count },
            $"Removed {entries.Count} availability record(s) for client {clientId} between {startDate:yyyy-MM-dd} " +
            $"and {endDate:yyyy-MM-dd} and confirmed the range is empty (verified) — those days are fully open for planning again.");
    }
}
