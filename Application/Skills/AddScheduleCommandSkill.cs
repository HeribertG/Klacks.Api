// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that places a key command on a client/date (the admin-configured tokens for FREE, -FREE,
/// EARLY, -EARLY, LATE, -LATE, NIGHT, -NIGHT — see IScheduleCommandKeywordProvider). Key commands
/// are seeds the wizards must respect — e.g. EARLY = only an early shift may be assigned that day.
/// </summary>
/// <param name="clientId">UUID of the client.</param>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
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

[SkillImplementation("add_schedule_command")]
public class AddScheduleCommandSkill : BaseSkillImplementation
{
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;

    public AddScheduleCommandSkill(
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
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var rawKeyword = GetRequiredString(parameters, "commandKeyword").Trim();
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
        if (!configuredKeywords.TryResolveToken(rawKeyword, out var keyword))
        {
            return SkillResult.Error(
                $"Invalid commandKeyword '{rawKeyword}'. Must be one of: {string.Join(", ", configuredKeywords.ValidTokens)}.");
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

        var entity = new ScheduleCommand
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CurrentDate = date,
            CommandKeyword = keyword,
            AnalyseToken = analyseToken,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _scheduleCommandRepository.Add(entity);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                Id = entity.Id,
                ClientId = clientId,
                Date = date,
                Keyword = keyword,
                AnalyseToken = analyseToken
            },
            $"Schedule command '{keyword}' placed for client {clientId} on {date}. " +
            "Wizards 1+2+3 will respect this constraint.");
    }
}
