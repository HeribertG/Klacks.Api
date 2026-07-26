// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that places the same key command (the admin-configured tokens for FREE, -FREE, EARLY,
/// -EARLY, LATE, -LATE, NIGHT, -NIGHT — see IScheduleCommandKeywordProvider) on a client for every
/// day of a date range — the range variant of add_schedule_command, iterating server-side because
/// callers (recipes, orchestrators) cannot loop. Days that already carry the keyword are skipped
/// instead of duplicated.
/// </summary>
/// <param name="clientId">UUID of the client.</param>
/// <param name="fromDate">First day of the range in ISO yyyy-MM-dd.</param>
/// <param name="untilDate">Last day of the range in ISO yyyy-MM-dd (equal to fromDate for one day).</param>
/// <param name="commandKeyword">One of the currently configured planning-command tokens (English defaults: FREE / -FREE / EARLY / -EARLY / LATE / -LATE / NIGHT / -NIGHT).</param>
/// <param name="analyseToken">Optional scenario UUID; null = main schedule.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;

    public AddScheduleCommandsRangeSkill(
        IScheduleCommandRepository scheduleCommandRepository,
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork,
        IScheduleCommandKeywordProvider keywordProvider)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
        _keywordProvider = keywordProvider;
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
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
        var keyword = configuredKeywords.ValidTokens
            .FirstOrDefault(t => string.Equals(t, rawKeyword, StringComparison.OrdinalIgnoreCase));
        if (keyword == null)
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

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw) && Guid.TryParse(analyseTokenRaw, out var atParsed))
        {
            analyseToken = atParsed;
        }

        var existingDates = (await _scheduleCommandRepository.GetByClientsAndDateRangeAsync(
                [clientId], fromDate, untilDate, analyseToken, cancellationToken))
            .Where(c => string.Equals(c.CommandKeyword, keyword, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.CurrentDate)
            .ToHashSet();

        var placed = new List<DateOnly>();
        for (var date = fromDate; date <= untilDate; date = date.AddDays(1))
        {
            if (existingDates.Contains(date))
            {
                continue;
            }

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
