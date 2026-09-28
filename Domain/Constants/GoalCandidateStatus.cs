// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class GoalCandidateStatus
{
    public const string Shadow = "shadow";
    public const string Proposed = "proposed";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Expired = "expired";

    public static bool IsTerminal(string? status) =>
        status is Approved or Rejected or Expired;
}
