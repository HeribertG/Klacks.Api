// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Application-side implementation of ISkillInverseResolver over InverseSkillRegistry. An undo is only
/// ever built for a call that actually changed something and succeeded: a read-only call has nothing to
/// undo, and a failed call changed nothing - offering to undo either is the over-repair the design
/// warns about.
/// </summary>

using Klacks.Api.Application.Skills.Meta;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillInverseResolver : ISkillInverseResolver
{
    public bool TryResolve(AssistantLastActionCall call, out SkillUndoInvocation? undo)
    {
        undo = null;
        if (call.IsReadOnly || !call.Success)
        {
            return false;
        }

        return InverseSkillRegistry.TryBuildUndo(
            call.SkillName, call.ArgumentsJson, call.ResultDataJson, out undo);
    }
}
