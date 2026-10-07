// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum ReplacementRequestOutcome
{
    Proposed = 0,
    Requested = 1,
    Accepted = 2,
    Declined = 3,
    NotReached = 4,
}
