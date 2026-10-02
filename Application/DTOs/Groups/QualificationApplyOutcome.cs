// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of one attempt of the apply transaction in PartitionClientsByQualificationCommandHandler. Returned
/// instead of mutating captured state because the unit of work's execution strategy may retry the lambda.
/// </summary>
/// <param name="VerifiedCount">Number of new memberships confirmed by the post-commit re-read.</param>
/// <param name="ParentGroupId">Id of the parent group (the created or reused qualifications root, or the scope group).</param>
/// <param name="GroupIds">Database id of every planned qualification group, keyed by its name.</param>
public sealed record QualificationApplyOutcome(
    int VerifiedCount,
    Guid ParentGroupId,
    IReadOnlyDictionary<string, Guid> GroupIds);
