// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Short-lived store for a planning-profile draft being collected across dialog turns: Set persists the
/// draft for a user/conversation, Get returns the outstanding draft (or null once it has expired) and
/// Clear removes it once the profile is applied or abandoned. Entries are scoped per (user, conversation).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingPlanningProfileDraftStore
{
    void Set(Guid userId, string conversationId, PlanningProfileDraft draft);

    PlanningProfileDraft? Get(Guid userId, string conversationId);

    void Clear(Guid userId, string conversationId);
}
