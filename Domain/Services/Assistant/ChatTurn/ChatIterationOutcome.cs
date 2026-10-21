// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal enum ChatIterationOutcome
{
    NextIteration,
    LeaveLoop,
    EndTurn
}
