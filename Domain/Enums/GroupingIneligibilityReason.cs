// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum GroupingIneligibilityReason
{
    NoActiveContract = 0,
    WeekdayNotAllowed = 1,
    NotShiftWorker = 2,
    MandatoryQualificationMissing = 3,
    Blacklisted = 4,
    Unavailable = 5,
}
