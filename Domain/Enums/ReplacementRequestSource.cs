// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum ReplacementRequestSource
{
    RecoveryEngine = 0,
    PlannerDialog = 1,
    ManualReplacement = 2,
    Messenger = 3,
}
