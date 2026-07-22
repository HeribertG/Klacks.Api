// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a company-rule draft being collected across dialog turns. Replaces the in-memory
/// pending-draft store so a draft started with start_company_rule survives a backend restart instead of
/// being silently dropped before the admin finishes set_company_rule_parameters/apply. Deliberately not a
/// <c>BaseEntity</c>: the row is ephemeral and is hard-deleted on Clear/expiry rather than soft-deleted.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingCompanyRuleDraftRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string DraftJson { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
