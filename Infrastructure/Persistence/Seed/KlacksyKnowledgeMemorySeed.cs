// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the markdown knowledge "Happen" from Infrastructure/Persistence/Seed/KlacksyKnowledge/
/// into the agent_memories table for the default Klacksy agent. Idempotent: each file is hashed
/// and only re-written when the hash changes; unchanged files are skipped. Removed files leave
/// existing memories intact (manual cleanup if desired).
/// </summary>
/// <param name="agentRepository">Default-agent lookup</param>
/// <param name="memoryRepository">Read/write access to agent_memories</param>
/// <param name="environment">Provides ContentRootPath for locating the markdown folder</param>
/// <param name="logger">Diagnostic output</param>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class KlacksyKnowledgeMemorySeed
{
    private const string KnowledgeFolder = "Infrastructure/Persistence/Seed/KlacksyKnowledge";
    private const string Category = MemoryCategories.SystemKnowledge;
    private const int KnowledgeImportance = 8;
    private const string MetadataHashKey = "contentHash";
    private const string MetadataFileKey = "sourceFile";

    private static readonly Regex FrontmatterRegex = new(
        @"\A---\s*\n(?<body>.+?)\n---\s*\n(?<content>.*)\z",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex NameFieldRegex = new(
        @"^name:\s*(?<value>\S+)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<KlacksyKnowledgeMemorySeed> _logger;

    public KlacksyKnowledgeMemorySeed(
        IAgentRepository agentRepository,
        IAgentMemoryRepository memoryRepository,
        IWebHostEnvironment environment,
        ILogger<KlacksyKnowledgeMemorySeed> logger)
    {
        _agentRepository = agentRepository;
        _memoryRepository = memoryRepository;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            _logger.LogWarning("No default agent found. Skipping KlacksyKnowledge memory seed.");
            return;
        }

        var folder = Path.Combine(_environment.ContentRootPath, KnowledgeFolder);
        if (!Directory.Exists(folder))
        {
            _logger.LogInformation("KlacksyKnowledge folder not found at {Folder}. Skipping.", folder);
            return;
        }

        var files = Directory.EnumerateFiles(folder, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogInformation("KlacksyKnowledge folder is empty. Nothing to seed.");
            return;
        }

        var existingByKey = (await _memoryRepository.GetByCategoryAsync(agent.Id, Category, cancellationToken))
            .Where(m => m.Source == MemorySources.SystemImport)
            .ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var file in files)
        {
            var parsed = ParseMarkdown(file);
            if (parsed == null)
            {
                _logger.LogWarning("Could not parse frontmatter for {File}. Skipping.", Path.GetFileName(file));
                skipped++;
                continue;
            }

            var fileName = Path.GetFileName(file);
            var contentHash = HashHex(parsed.Content);

            if (existingByKey.TryGetValue(parsed.Key, out var existing))
            {
                if (GetMetadataValue(existing.Metadata, MetadataHashKey) == contentHash)
                {
                    skipped++;
                    continue;
                }

                existing.Content = parsed.Content;
                existing.Metadata = BuildMetadata(contentHash, fileName);
                existing.SourceRef = $"{KnowledgeFolder}/{fileName}";
                existing.Embedding = null;
                await _memoryRepository.UpdateAsync(existing, cancellationToken);
                updated++;
            }
            else
            {
                var memory = new AgentMemory
                {
                    Id = Guid.NewGuid(),
                    AgentId = agent.Id,
                    Category = Category,
                    Key = parsed.Key,
                    Content = parsed.Content,
                    Importance = KnowledgeImportance,
                    IsPinned = false,
                    Source = MemorySources.SystemImport,
                    SourceRef = $"{KnowledgeFolder}/{fileName}",
                    Metadata = BuildMetadata(contentHash, fileName),
                };
                await _memoryRepository.AddAsync(memory, cancellationToken);
                inserted++;
            }
        }

        _logger.LogInformation(
            "KlacksyKnowledge memory seed: {Inserted} inserted, {Updated} updated, {Skipped} skipped (of {Total} files).",
            inserted, updated, skipped, files.Count);
    }

    private static ParsedMarkdown? ParseMarkdown(string path)
    {
        var raw = File.ReadAllText(path);
        var fm = FrontmatterRegex.Match(raw);
        if (!fm.Success) return null;

        var nameMatch = NameFieldRegex.Match(fm.Groups["body"].Value);
        if (!nameMatch.Success) return null;

        return new ParsedMarkdown(
            Key: nameMatch.Groups["value"].Value.Trim(),
            Content: fm.Groups["content"].Value.TrimStart('\n', '\r').TrimEnd());
    }

    private static string HashHex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildMetadata(string contentHash, string fileName)
    {
        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [MetadataHashKey] = contentHash,
            [MetadataFileKey] = fileName,
        });
    }

    private static string? GetMetadataValue(string metadataJson, string key)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            return doc.RootElement.TryGetProperty(key, out var prop)
                ? prop.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ParsedMarkdown(string Key, string Content);
}
