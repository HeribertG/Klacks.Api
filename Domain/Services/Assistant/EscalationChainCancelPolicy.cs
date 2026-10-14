// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Who may cancel an escalation chain (Owner decision B7): an administrator, a supervisor (Authorised), or a roster
/// member who still holds an open (Pending or Notified) stage of THIS chain. Everybody else may look but not stop it.
/// </summary>
/// <param name="chain">Chain with its stages</param>
/// <param name="userId">Id of the calling user</param>
/// <param name="isAdmin">True when the caller holds the Admin role</param>
/// <param name="isSupervisor">True when the caller holds the Authorised role</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Services.Assistant;

public static class EscalationChainCancelPolicy
{
    public static bool MayCancel(EscalationChain chain, string userId, bool isAdmin, bool isSupervisor)
    {
        if (isAdmin || isSupervisor)
        {
            return true;
        }

        return chain.Stages.Any(s =>
            s.UserId == userId && (s.Status == EscalationStageStatus.Pending || s.Status == EscalationStageStatus.Notified));
    }
}
