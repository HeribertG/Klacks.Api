// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the inverse invocation of a call that was made. The table lives in the Application layer
/// next to the skills it names; the Domain turn preparation only asks the question, which keeps the
/// dependency pointing inwards.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillInverseResolver
{
    bool TryResolve(AssistantLastActionCall call, out SkillUndoInvocation? undo);
}
