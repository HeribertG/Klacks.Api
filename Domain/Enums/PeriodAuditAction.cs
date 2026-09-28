// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum PeriodAuditAction
{
    Seal = 0,
    Unseal = 1,
    ApproveDay = 2,
    ConfirmWork = 3,
    ConfirmBreak = 4
}
