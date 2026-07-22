// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed store for one-time skill-confirmation tokens, so a confirmation held by the autonomy gate
/// survives a backend restart instead of living only in an in-memory singleton. Each operation runs in
/// its OWN service scope (via IServiceScopeFactory) rather than a request-scoped DbContext, mirroring
/// PersistentPendingRecipeStore and PersistentPendingCompanyRuleDraftStore: the chat pipeline can touch
/// the request context concurrently, so a shared context would race. The IPendingConfirmationStore
/// interface is synchronous because callers (AutonomyGateService, ConfirmPendingActionSkill) invoke it
/// synchronously, so the short single-row repository calls are awaited via GetAwaiter().GetResult();
/// ASP.NET Core has no synchronization context, so this cannot deadlock. Consume deletes the row keyed
/// on the token ALONE before validating expiry/user/skill, so a mismatched or expired token is still
/// burned on first use — exactly like the in-memory store it replaces.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingConfirmationStore : IPendingConfirmationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentPendingConfirmationStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Create(Guid userId, string skillName, IReadOnlyDictionary<string, object> parameters)
    {
        var now = DateTime.UtcNow;
        var token = Guid.NewGuid().ToString("N");

        var row = new PendingConfirmationRow
        {
            Token = token,
            UserId = userId,
            SkillName = skillName,
            ParametersJson = JsonSerializer.Serialize(parameters, JsonOptions),
            ExpiresAtUtc = now.AddMinutes(AutonomyDefaults.ConfirmationTtlMinutes)
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingConfirmationRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.AddAsync(row).GetAwaiter().GetResult();
        return token;
    }

    public PendingConfirmation? Consume(string token, Guid userId, string? expectedSkillName = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingConfirmationRepository>();

        var row = repository.ConsumeAsync(token).GetAwaiter().GetResult();
        if (row == null)
        {
            return null;
        }

        if (row.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        if (row.UserId != userId)
        {
            return null;
        }

        if (expectedSkillName != null
            && !string.Equals(row.SkillName, expectedSkillName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new PendingConfirmation(
            row.UserId,
            row.SkillName,
            DeserializeParameters(row.ParametersJson),
            row.ExpiresAtUtc);
    }

    public PendingConfirmationHandle? PeekLatestForUser(Guid userId, TimeSpan maxAge)
    {
        var now = DateTime.UtcNow;
        var ttl = TimeSpan.FromMinutes(AutonomyDefaults.ConfirmationTtlMinutes);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingConfirmationRepository>();
        var rows = repository.GetActiveForUserAsync(userId, now).GetAwaiter().GetResult();

        string? latestToken = null;
        string? latestSkillName = null;
        var latestExpiry = DateTime.MinValue;

        foreach (var row in rows)
        {
            var createdAt = row.ExpiresAtUtc - ttl;
            if (now - createdAt > maxAge)
            {
                continue;
            }

            if (row.ExpiresAtUtc > latestExpiry)
            {
                latestExpiry = row.ExpiresAtUtc;
                latestToken = row.Token;
                latestSkillName = row.SkillName;
            }
        }

        return latestToken == null ? null : new PendingConfirmationHandle(latestToken, latestSkillName!);
    }

    private static IReadOnlyDictionary<string, object> DeserializeParameters(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson, JsonOptions);
            return parsed ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>();
        }
    }
}
