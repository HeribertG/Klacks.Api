// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists available contracts with optional canton filter via CalendarSelection.
/// </summary>
/// <param name="activeOnly">If true, only return currently valid contracts</param>
/// <param name="canton">Optional canton/state code (e.g. "BE") to filter contracts by CalendarSelection</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_contracts")]
public class ListContractsSkill : BaseSkillImplementation
{
    private const string DefaultCountry = "CH";

    private readonly IContractRepository _contractRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public ListContractsSkill(
        IContractRepository contractRepository,
        ICalendarSelectionRepository calendarSelectionRepository)
    {
        _contractRepository = contractRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var activeOnly = GetParameter<bool>(parameters, "activeOnly", true);
        var canton = GetParameter<string>(parameters, "canton");

        var allContracts = await _contractRepository.List();
        var today = DateTime.UtcNow.Date;

        var filteredContracts = allContracts
            .Where(c => !c.IsDeleted)
            .Where(c => !activeOnly || (c.ValidFrom <= today && (c.ValidUntil == null || c.ValidUntil >= today)));

        if (!string.IsNullOrWhiteSpace(canton))
        {
            var calendarSelectionIds = await _calendarSelectionRepository
                .GetIdsByStateAsync(DefaultCountry, canton.Trim().ToUpperInvariant(), cancellationToken);

            if (calendarSelectionIds.Count > 0)
            {
                filteredContracts = filteredContracts
                    .Where(c => c.CalendarSelectionId != null && calendarSelectionIds.Contains(c.CalendarSelectionId.Value));
            }
        }

        var contracts = filteredContracts
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.GuaranteedHours,
                c.MinimumHours,
                c.MaximumHours,
                c.FullTime,
                c.PaymentInterval,
                c.ValidFrom,
                c.ValidUntil,
                c.NightRate,
                c.HolidayRate,
                c.SaRate,
                c.SoRate
            })
            .ToList();

        var resultData = new
        {
            Contracts = contracts,
            TotalCount = contracts.Count,
            ActiveOnly = activeOnly,
            Canton = canton
        };

        var message = $"Found {contracts.Count} contract(s)" +
                      (activeOnly ? " (active only)" : "") +
                      (!string.IsNullOrWhiteSpace(canton) ? $" for canton {canton.ToUpperInvariant()}" : "") +
                      ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
