// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public enum ExplainDepthOverrideOutcome
{
    Stored,
    Cleared,
    UnknownDepth,
    NoUser,
    NoAgent,
}
