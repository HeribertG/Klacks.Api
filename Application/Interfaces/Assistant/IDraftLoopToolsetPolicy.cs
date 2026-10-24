// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Decides, per turn, which skills the open draft loops of a conversation keep in the toolset and which they keep out
/// (planning-profile setup, order review). Visibility only: every call still runs through the autonomy gate.
/// </summary>
public interface IDraftLoopToolsetPolicy
{
    DraftLoopToolset Resolve(string userId, string? conversationId);
}
