// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum WorkChangeType
{
    CorrectionEnd = 0,
    CorrectionStart = 1,
    ReplacementStart = 2,
    ReplacementEnd = 3,
    TravelStart = 4,
    TravelEnd = 5,
    TravelWithin = 6,
    Briefing = 7,
    Debriefing = 8,
}
