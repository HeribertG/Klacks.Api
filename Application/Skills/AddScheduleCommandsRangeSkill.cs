// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that places the same key command (the admin-configured tokens for FREE, -FREE, EARLY,
/// -EARLY, LATE, -LATE, NIGHT, -NIGHT — see IScheduleCommandKeywordProvider) on a client for every
/// day of a date range — the range variant of add_schedule_command, iterating server-side because
/// callers (recipes, orchestrators) cannot loop. Days that already carry the keyword are skipped
/// instead of duplicated. If any day still to be placed is a sealed main-plan day (IDayLockService), the whole range is
/// refused with an error result and nothing is written, like the bulk work/break handlers; scenario writes are exempt.
/// A client hidden from the caller by group visibility is answered exactly like an unknown client id.
/// </summary>
/// <param name="clientId">UUID of the client.</param>
/// <param name="fromDate">First day of the range in ISO yyyy-MM-dd.</param>
/// <param name="untilDate">Last day of the range in ISO yyyy-MM-dd (equal to fromDate for one day).</param>
/// <param name="commandKeyword">One of the currently configured planning-command tokens (English defaults: FREE / -FREE / EARLY / -EARLY / LATE / -LATE / NIGHT / -NIGHT).</param>
/// <param name="analyseToken">Optional scenario UUID; null = main schedule.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_schedule_commands_range")]
public class AddScheduleCommandsRangeSkill : BaseSkillImplementation
{
    private const int MaxRangeDays = 92;

    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;
    private readonly IDayLockService _dayLockService;

    public AddScheduleCommandsRangeSkill(
        IScheduleCommandRepository scheduleCommandRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        IScheduleCommandKeywordProvider keywordProvider,
        IDayLockService dayLockService)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _keywordProvider = keywordProvider;
        _dayLockService = dayLockService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate")
            ?? throw new ArgumentException("Required parameter 'fromDate' is missing");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate")
            ?? throw new ArgumentException("Required parameter 'untilDate' is missing");
        var rawKeyword = GetRequiredString(parameters, "commandKeyword").Trim();

        var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
        if (!configuredKeywords.TryResolveToken(rawKeyword, out var keyword))
        {
            return SkillResult.Error(
                $"Invalid commandKeyword '{rawKeyword}'. Must be one of: {string.Join(", ", configuredKeywords.ValidTokens)}.");
        }

        if (untilDate < fromDate)
        {
            return SkillResult.Error($"untilDate ({untilDate}) must not be before fromDate ({fromDate}).");
        }

        var totalDays = untilDate.DayNumber - fromDate.DayNumber + 1;
        if (totalDays > MaxRangeDays)
        {
            return SkillResult.Error(
                $"Range spans {totalDays} days; the maximum is {MaxRangeDays}. Split the request into smaller ranges.");
        }

        if (!await ClientResolver.ExistsVisibleAsync(_clientRepository, _clientVisibilityGuard, clientId, cancellationToken))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var existingDates = (await _scheduleCommandRepository.GetByClientsAndDateRangeAsync(
                [clientId], fromDate, untilDate, analyseToken, cancellationToken))
            .Where(c => string.Equals(c.CommandKeyword, keyword, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.CurrentDate)
            .ToHashSet();

        var datesToPlace = new List<DateOnly>();
        for (var date = fromDate; date <= untilDate; date = date.AddDays(1))
        {
            if (!existingDates.Contains(date))
            {
                datesToPlace.Add(date);
            }
        }

        try
        {
            await _dayLockService.EnsureNoneLockedAsync(
                datesToPlace.Select(date => (date, clientId, analyseToken)).ToList(),
                cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        var placed = new List<DateOnly>();
        foreach (var date in datesToPlace)
        {
            await _scheduleCommandRepository.Add(new ScheduleCommand
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CurrentDate = date,
                CommandKeyword = keyword,
                AnalyseToken = analyseToken,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = context.UserName
            });
            placed.Add(date);
        }

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                Keyword = keyword,
                FromDate = fromDate,
                UntilDate = untilDate,
                PlacedCount = placed.Count,
                SkippedExisting = totalDays - placed.Count,
                AnalyseToken = analyseToken
            },
            $"Schedule command '{keyword}' placed for client {clientId} on {placed.Count} day(s) " +
            $"between {fromDate} and {untilDate} ({totalDays - placed.Count} already present, skipped). " +
            "Wizards 1+2+3 will respect these constraints.");
    }
}
