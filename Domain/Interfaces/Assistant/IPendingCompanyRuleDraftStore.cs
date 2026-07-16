// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Short-lived store for a company-rule draft being collected across dialog turns: Set persists the
/// draft for a user/conversation, Get returns the outstanding draft (or null once it has expired) and
/// Clear removes it once the rule is applied or abandoned. Entries are scoped per (user, conversation).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingCompanyRuleDraftStore
{
    void Set(Guid userId, string conversationId, CompanyRuleDraft draft);

    CompanyRuleDraft? Get(Guid userId, string conversationId);

    void Clear(Guid userId, string conversationId);
}
