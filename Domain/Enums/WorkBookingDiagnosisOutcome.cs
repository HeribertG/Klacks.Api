// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum WorkBookingDiagnosisOutcome
{
    NotFound = 0,
    Bookable = 1,
    Refused = 2,
    GridOnly = 3
}
