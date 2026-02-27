// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SpamRuleType
{
    SenderContains = 0,
    SenderDomain = 1,
    SubjectContains = 2,
    BodyContains = 3
}
