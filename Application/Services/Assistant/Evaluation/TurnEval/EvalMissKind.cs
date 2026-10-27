// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public enum EvalMissKind
{
    Correct,
    ReachedViaFollowUp,
    LookupFirst,
    SurfaceSearch,
    WrongWrite,
    WrongRead,
    NoToolText,
    NotInToolsetTruncated,
    NotInToolsetNotRetrieved,
    ToolNotRegistered,
    NotApplicable
}
