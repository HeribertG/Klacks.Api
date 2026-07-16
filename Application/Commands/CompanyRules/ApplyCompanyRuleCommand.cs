// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the pending company-rule draft for a user/conversation: writes the target entity
/// (surcharge settings, a counter rule or a custom macro), records a registry row and clears the draft.
/// </summary>
/// <param name="UserId">Owner of the pending draft.</param>
/// <param name="ConversationKey">Conversation scope key the draft is stored under.</param>

using System;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.CompanyRules;

public record ApplyCompanyRuleCommand(Guid UserId, string ConversationKey) : IRequest<CompanyRuleResource?>;
