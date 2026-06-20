// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Emits at most one light "by the way" small-talk question per connected user, targeted at that
/// user, to learn a personal interest. Disabled by default (opt-in via configuration). Skips users
/// whose daily curiosity budget is already spent and questions whose topic the user has already
/// revealed (checked against their per-user memories), so Klacksy never asks twice.
/// </summary>
/// <param name="notificationService">Provides the list of currently connected users.</param>
/// <param name="memoryRepository">Reads the user's personal memories to skip answered topics.</param>
/// <param name="agentRepository">Resolves the default agent that owns the memories.</param>
/// <param name="rateLimiter">Per-user daily budget gate (curiosity is capped at one per day).</param>
/// <param name="configuration">Holds the opt-in flag; the feature is off unless explicitly enabled.</param>
/// <param name="logger">Structured log per scan.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Configuration;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class CuriosityQuestionDetector : IAgentTriggerDetector
{
    public const string EnabledConfigKey = "Assistant:Curiosity:Enabled";

    private readonly IAssistantNotificationService _notificationService;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentTriggerRateLimiter _rateLimiter;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CuriosityQuestionDetector> _logger;

    public CuriosityQuestionDetector(
        IAssistantNotificationService notificationService,
        IAgentMemoryRepository memoryRepository,
        IAgentRepository agentRepository,
        IAgentTriggerRateLimiter rateLimiter,
        IConfiguration configuration,
        ILogger<CuriosityQuestionDetector> logger)
    {
        _notificationService = notificationService;
        _memoryRepository = memoryRepository;
        _agentRepository = agentRepository;
        _rateLimiter = rateLimiter;
        _configuration = configuration;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.CuriosityQuestion;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue(EnabledConfigKey, false))
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var connectedUserIds = _notificationService.GetConnectedUserIds().ToList();
        if (connectedUserIds.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var userIdString in connectedUserIds)
        {
            if (!Guid.TryParse(userIdString, out var userId))
            {
                continue;
            }

            if (_rateLimiter.GetRemainingBudget(userIdString, AgentTriggerKinds.CuriosityQuestion) <= 0)
            {
                continue;
            }

            var memories = await _memoryRepository.GetByUserAsync(agent.Id, userId, cancellationToken);
            var blob = string.Join(" ", memories.Select(m => $"{m.Key} {m.Content}"));

            var question = CuriosityQuestions.Pool.FirstOrDefault(q =>
                !q.Keywords.Any(k => blob.Contains(k, StringComparison.OrdinalIgnoreCase)));

            if (question == null)
            {
                continue;
            }

            events.Add(new CuriosityQuestionTriggerEvent(question.Text, userId));
        }

        if (events.Count > 0)
        {
            _logger.LogInformation("Curiosity scan: {Count} question(s) queued for connected user(s)", events.Count);
        }

        return events;
    }
}
