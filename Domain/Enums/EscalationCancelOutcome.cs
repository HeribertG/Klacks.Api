// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum EscalationCancelOutcome
{
    Cancelled = 0,
    AlreadyResolved = 1,
    Forbidden = 2,
}
