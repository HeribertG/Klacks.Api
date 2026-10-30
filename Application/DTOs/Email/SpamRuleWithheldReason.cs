// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

public enum SpamRuleWithheldReason
{
    OwnDomain = 0,
    KnownContactDomain = 1,
    InactiveRuleExists = 2
}
