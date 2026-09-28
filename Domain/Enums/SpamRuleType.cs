// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum SpamRuleType
{
    SenderContains = 0,
    SenderDomain = 1,
    SubjectContains = 2,
    BodyContains = 3
}
