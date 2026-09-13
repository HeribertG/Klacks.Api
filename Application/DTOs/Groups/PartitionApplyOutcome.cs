// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of one attempt of the apply transaction in <see cref="Handlers.Groups.PartitionClientsByAddressCommandHandler"/>.
/// Returned instead of mutating captured state because the unit of work's execution strategy may retry
/// the transaction lambda, and a captured counter or dictionary would then accumulate across attempts.
/// </summary>
/// <param name="VerifiedCount">Number of new memberships confirmed by the post-commit re-read.</param>
/// <param name="AlreadyMemberCount">Number of planned assignments skipped because the client already held the membership.</param>
/// <param name="ResolvedGroupIds">Database id of every planned group node, keyed by its planning-run key, for this attempt.</param>
public sealed record PartitionApplyOutcome(
    int VerifiedCount,
    int AlreadyMemberCount,
    IReadOnlyDictionary<string, Guid> ResolvedGroupIds);
