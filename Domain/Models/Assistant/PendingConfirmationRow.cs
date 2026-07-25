// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a one-time skill-confirmation token. Replaces the in-memory pending-confirmation
/// store so an outstanding confirmation survives a backend restart instead of being silently dropped
/// between the gate holding a sensitive skill and the user's reply. Deliberately not a
/// <c>BaseEntity</c>: the row is ephemeral and is hard-deleted on consumption, mismatch or expiry
/// rather than soft-deleted. Purpose keeps the two writers of this single table apart (see
/// PendingConfirmationPurposes); an empty value is read as GateReplay so rows written before the
/// discriminator existed keep their original meaning.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingConfirmationRow
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string ParametersJson { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Purpose { get; set; } = PendingConfirmationPurposes.GateReplay;
}
