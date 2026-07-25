// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Outcome of applying the grouping proposal.
/// </summary>
/// <param name="MovedCount">Number of clients whose memberships actually changed.</param>
/// <param name="VerifiedCount">Number of created memberships re-read and confirmed in the database after the write.</param>
/// <param name="UnassignedCount">Number of clients the proposal could not place.</param>
/// <param name="EndedMembershipCount">Number of existing memberships that were actually ended, counting only those still present when the move ran — this is what the proposal announced as the memberships that would end.</param>
/// <param name="AppliedValidFrom">Start date actually written to the new memberships: either the date the caller asked for or the company's current date. Reported back so the answer can name the date that was really used instead of the one that was requested.</param>
public record CustomerGroupingApplyResult(
    int MovedCount,
    int VerifiedCount,
    int UnassignedCount,
    int EndedMembershipCount,
    DateTime AppliedValidFrom);
