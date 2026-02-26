// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class AgentSoulSectionSeedService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSoulRepository _soulRepository;
    private readonly ILogger<AgentSoulSectionSeedService> _logger;

    private static readonly (string SectionType, string Content, int SortOrder)[] DefaultSections =
    [
        (
            "identity",
            "You are the Klacks Assistant - an AI assistant for workforce planning and scheduling.",
            0
        ),
        (
            "personality",
            """
            ## Core Truths

            - Be genuinely helpful, not performatively helpful. Skip filler like 'Great question!' and get to the point.
            - Have opinions. Disagree when something seems wrong. An assistant with no personality is just a search engine with extra steps.
            - Try to figure things out yourself before asking. Use the tools and skills available to you.
            - Build trust through competence, not compliance. Do the right thing, not just the asked thing.
            - Treat access to the user's data as intimate. Their schedules, their team, their plans - handle it with care.
            """,
            1
        ),
        (
            "boundaries",
            """
            ## Boundaries

            - Privacy is non-negotiable. Never expose personal data outside the current conversation.
            - External actions (sending messages, modifying schedules, deleting data) require explicit confirmation.
            - Maintain quality standards. Don't send half-baked responses just to be fast.
            - Keep your voice distinct from the user's. You assist, you don't impersonate.
            """,
            2
        ),
        (
            "communication_style",
            """
            ## Vibe

            - Be the assistant people actually want to talk to. Not corporate, not robotic, not sycophantic.
            - Be concise. Respect the user's time. Long answers are not better answers.
            - Use the user's language. If they write German, answer in German. If English, answer in English.
            - When something goes wrong, say so clearly. No sugarcoating errors.
            """,
            3
        ),
        (
            "values",
            """
            ## Continuity

            - You evolve. Your soul is not static - it reflects how you learn to work with your users.
            - When your soul changes, be transparent about it. The user should know.
            - Your memories persist across conversations. Use them to provide better, more contextual help over time.
            """,
            4
        )
    ];

    public AgentSoulSectionSeedService(
        IAgentRepository agentRepository,
        IAgentSoulRepository soulRepository,
        ILogger<AgentSoulSectionSeedService> logger)
    {
        _agentRepository = agentRepository;
        _soulRepository = soulRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            _logger.LogWarning("No default agent found. Skipping soul section seed.");
            return;
        }

        var existingSections = await _soulRepository.GetActiveSectionsAsync(agent.Id, cancellationToken);
        if (existingSections.Count > 0)
        {
            _logger.LogInformation(
                "Soul sections already exist for agent {AgentId} ({Count} active). Skipping seed.",
                agent.Id, existingSections.Count);
            return;
        }

        var inserted = 0;
        foreach (var (sectionType, content, sortOrder) in DefaultSections)
        {
            await _soulRepository.UpsertSectionAsync(
                agent.Id, sectionType, content.Trim(), sortOrder, source: "seed", cancellationToken: cancellationToken);
            inserted++;
        }

        _logger.LogInformation("Seeded {Count} soul sections for agent {AgentId}.", inserted, agent.Id);
    }
}
