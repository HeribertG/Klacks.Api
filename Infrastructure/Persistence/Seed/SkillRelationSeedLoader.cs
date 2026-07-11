// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads experience-derived skill graph edges from skill-relation-seeds.json and inserts them for the
/// default agent. Insert-only and idempotent: existing edges are never touched so that later
/// experience-based learning is preserved; edges referencing skills that are not present (e.g. from
/// uninstalled feature plugins) are skipped.
/// </summary>
using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.SkillGraph;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Seed.Models;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class SkillRelationSeedLoader
{
    private readonly ISkillRelationRepository _skillRelationRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SkillRelationSeedLoader> _logger;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SeedFilePath = "Application/Skills/Definitions/skill-relation-seeds.json";

    public SkillRelationSeedLoader(
        ISkillRelationRepository skillRelationRepository,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        IWebHostEnvironment environment,
        ILogger<SkillRelationSeedLoader> logger)
    {
        _skillRelationRepository = skillRelationRepository;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _environment = environment;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, SeedFilePath);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Skill relation seed file not found at {FilePath}. Skipping.", filePath);
            return;
        }

        SkillRelationSeedFile seedFile;
        try
        {
            await using var stream = File.OpenRead(filePath);
            seedFile = await JsonSerializer.DeserializeAsync<SkillRelationSeedFile>(stream, JsonReadOptions, cancellationToken)
                       ?? throw new InvalidDataException("Skill relation seed file deserialized to null.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize skill relation seed file at {FilePath}.", filePath);
            return;
        }

        if (seedFile.Relations.Count == 0)
        {
            _logger.LogInformation("Skill relation seed file contains no relations. Skipping.");
            return;
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            _logger.LogWarning("No default agent found. Skipping skill relation seeding.");
            return;
        }

        var knownSkillNames = (await _agentSkillRepository.GetAllByAgentIdAsync(agent.Id, cancellationToken))
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingKeys = (await _skillRelationRepository.GetByAgentAsync(agent.Id, cancellationToken))
            .Select(r => (r.SkillAName, r.SkillBName, r.Type))
            .ToHashSet();

        var toAdd = new List<SkillRelation>();
        var skippedUnknownSkill = 0;

        foreach (var definition in seedFile.Relations)
        {
            if (string.IsNullOrWhiteSpace(definition.SkillAName) || string.IsNullOrWhiteSpace(definition.SkillBName))
            {
                continue;
            }

            if (existingKeys.Contains((definition.SkillAName, definition.SkillBName, definition.Type)))
            {
                continue;
            }

            if (!knownSkillNames.Contains(definition.SkillAName) || !knownSkillNames.Contains(definition.SkillBName))
            {
                skippedUnknownSkill++;
                continue;
            }

            toAdd.Add(new SkillRelation
            {
                AgentId = agent.Id,
                SkillAName = definition.SkillAName,
                SkillBName = definition.SkillBName,
                Type = definition.Type,
                Confidence = definition.Confidence,
                SupportCount = definition.SupportCount,
                ContradictionCount = definition.ContradictionCount,
                Provenance = SkillGraphConstants.SeededExperienceProvenancePrefix + definition.Provenance,
                Source = SkillRelationSource.Learned,
                Status = definition.Status,
            });
        }

        if (toAdd.Count > 0)
        {
            await _skillRelationRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        _logger.LogInformation(
            "Skill relation seeding complete: {Added} added, {Existing} already present, {SkippedUnknown} skipped (unknown skill).",
            toAdd.Count, seedFile.Relations.Count - toAdd.Count - skippedUnknownSkill, skippedUnknownSkill);
    }
}
