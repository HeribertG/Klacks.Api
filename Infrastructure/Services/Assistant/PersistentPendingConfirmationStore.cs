// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF-backed store for one-time skill-confirmation tokens, so a confirmation held by the autonomy gate survives a
/// backend restart instead of living only in an in-memory singleton. Each operation runs in its OWN service scope (via
/// IServiceScopeFactory) rather than a request-scoped DbContext, mirroring PersistentPendingRecipeStore and
/// PersistentPendingCompanyRuleDraftStore: the chat pipeline can touch the request context concurrently, so a shared
/// context would race. The IPendingConfirmationStore interface is synchronous because callers (AutonomyGateService,
/// ConfirmPendingActionSkill) invoke it synchronously, so the short single-row repository calls are awaited via
/// GetAwaiter().GetResult(); ASP.NET Core has no synchronization context, so this cannot deadlock. Consume deletes the
/// row keyed on the token ALONE before validating expiry/user/skill, so a mismatched or expired token is still burned
/// on first use — exactly like the in-memory store it replaces. Proposal hints share this table and differ only in
/// their purpose column: they are written without invocation parameters, deliberately keep the SAME row TTL as
/// gate-replay rows (PeekLatestForUser reconstructs a row's creation time as ExpiresAtUtc - ConfirmationTtlMinutes, so
/// a shorter row TTL would make a fresh hint look stale immediately), and are instead bounded by the shorter maxAge the
/// reader passes. They are dropped again via DiscardProposalHints, which reuses the existing GetActiveForUser/Consume
/// repository calls rather than adding a second persistence path. A correction-undo row is a third purpose in the same
/// table, written through Create and dropped by DiscardCorrectionUndo; both discards share one loop because they differ
/// only in the purpose they match and in whether they narrow by skill name. Writing one replaces its predecessor the
/// way a proposal hint does, so a user never has two redeemable undo offers at once: the offering turn cannot discard
/// its own fresh row (it runs after the write), and without this a second correction inside the force window would
/// leave the first offer redeemable by a "ja" that was never meant for it. A binding (ConfirmationBinding) is stored as
/// JSON in its own nullable column, never inside the parameters, because the parameters are what the model supplied and
/// are compared with the call that redeems a token directly. A binding that cannot be read back makes Consume return
/// null (logged): the token is void, which is more honest than reading corrupt data as "nothing is bound".
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>
/// <param name="logger">Reports a stored binding that cannot be read.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingConfirmationStore : IPendingConfirmationStore
{
    private const string EmptyParametersJson = "{}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PersistentPendingConfirmationStore> _logger;

    public PersistentPendingConfirmationStore(
        IServiceScopeFactory scopeFactory, ILogger<PersistentPendingConfirmationStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Create(
        Guid userId,
        string skillName,
        IReadOnlyDictionary<string, object> parameters,
        string purpose = PendingConfirmationPurposes.GateReplay,
        string channel = PendingConfirmationChannels.Chat,
        ConfirmationBinding? binding = null)
    {
        if (string.Equals(purpose, PendingConfirmationPurposes.CorrectionUndo, StringComparison.Ordinal))
        {
            DiscardCorrectionUndo(userId);
        }

        var bindingJson = binding == null ? null : JsonSerializer.Serialize(binding, JsonOptions);
        return CreateRow(
            userId, skillName, JsonSerializer.Serialize(parameters, JsonOptions), purpose, channel, bindingJson);
    }

    public void CreateProposalHint(Guid userId, string applySkillName)
    {
        DiscardProposalHints(userId);
        CreateRow(
            userId, applySkillName, EmptyParametersJson, PendingConfirmationPurposes.ProposalHint,
            PendingConfirmationChannels.Chat);
    }

    public void DiscardProposalHints(Guid userId, string? applySkillName = null)
    {
        DiscardRows(userId, PendingConfirmationPurposes.ProposalHint, applySkillName);
    }

    public void DiscardCorrectionUndo(Guid userId)
    {
        DiscardRows(userId, PendingConfirmationPurposes.CorrectionUndo, null);
    }

    public void DiscardByTokens(Guid userId, IReadOnlyCollection<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        DiscardRows(userId, row => tokens.Contains(row.Token, StringComparer.Ordinal));
    }

    private void DiscardRows(Guid userId, string purpose, string? skillName)
    {
        DiscardRows(userId, row => HasPurpose(row, purpose)
            && (skillName == null || string.Equals(row.SkillName, skillName, StringComparison.OrdinalIgnoreCase)));
    }

    private void DiscardRows(Guid userId, Func<PendingConfirmationRow, bool> matches)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingConfirmationRepository>();
        var rows = repository.GetActiveForUserAsync(userId, DateTime.UtcNow).GetAwaiter().GetResult();

        foreach (var row in rows.Where(matches))
        {
            repository.ConsumeAsync(row.Token).GetAwaiter().GetResult();
        }
    }

    private string CreateRow(
        Guid userId, string skillName, string parametersJson, string purpose, string channel, string? bindingJson = null)
    {
        var now = DateTime.UtcNow;
        var token = Guid.NewGuid().ToString("N");

        var row = new PendingConfirmationRow
        {
            Token = token,
            UserId = userId,
            SkillName = skillName,
            ParametersJson = parametersJson,
            Purpose = purpose,
            Channel = channel,
            BindingJson = bindingJson,
            ExpiresAtUtc = now.AddMinutes(AutonomyDefaults.ConfirmationTtlMinutes)
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingConfirmationRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.AddAsync(row).GetAwaiter().GetResult();
        return token;
    }

    public PendingConfirmation? Consume(
        string token,
        Guid userId,
        string? expectedSkillName = null,
        string channel = PendingConfirmationChannels.Chat)
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

        if (!HasChannel(row, channel))
        {
            return null;
        }

        if (expectedSkillName != null
            && !string.Equals(row.SkillName, expectedSkillName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryDeserializeBinding(row, out var binding))
        {
            return null;
        }

        return new PendingConfirmation(
            row.UserId,
            row.SkillName,
            DeserializeParameters(row.ParametersJson),
            row.ExpiresAtUtc,
            NormalizedPurpose(row),
            binding);
    }

    private bool TryDeserializeBinding(PendingConfirmationRow row, out ConfirmationBinding? binding)
    {
        binding = null;
        if (string.IsNullOrWhiteSpace(row.BindingJson))
        {
            return true;
        }

        try
        {
            binding = JsonSerializer.Deserialize<ConfirmationBinding>(row.BindingJson, JsonOptions);
        }
        catch (JsonException)
        {
            binding = null;
        }

        if (binding?.Items != null && !string.IsNullOrEmpty(binding.Kind) && !string.IsNullOrEmpty(binding.Fingerprint))
        {
            return true;
        }

        _logger.LogWarning(
            "The stored confirmation binding of a token for skill {SkillName} and user {UserId} cannot be read; "
            + "the token is void", row.SkillName, row.UserId);
        binding = null;
        return false;
    }

    public PendingConfirmationHandle? PeekLatestForUser(
        Guid userId,
        TimeSpan maxAge,
        string purpose = PendingConfirmationPurposes.GateReplay,
        string channel = PendingConfirmationChannels.Chat)
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
            if (!HasPurpose(row, purpose) || !HasChannel(row, channel))
            {
                continue;
            }

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

    private static bool HasPurpose(PendingConfirmationRow row, string purpose) =>
        string.Equals(NormalizedPurpose(row), purpose, StringComparison.OrdinalIgnoreCase);

    private static bool HasChannel(PendingConfirmationRow row, string channel) =>
        string.Equals(NormalizedChannel(row), channel, StringComparison.OrdinalIgnoreCase);

    private static string NormalizedChannel(PendingConfirmationRow row) =>
        string.IsNullOrWhiteSpace(row.Channel) ? PendingConfirmationChannels.Chat : row.Channel;

    private static string NormalizedPurpose(PendingConfirmationRow row) =>
        string.IsNullOrWhiteSpace(row.Purpose) ? PendingConfirmationPurposes.GateReplay : row.Purpose;

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
