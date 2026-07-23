// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the pending planning-profile draft for a user/conversation: copies the base industry's
/// template scheduling rules as customer-owned rows (or creates one blank rule when starting from
/// scratch), applies the collected field overrides, sets ACTIVE_INDUSTRIES to the custom marker and
/// clears the draft.
/// </summary>
/// <param name="UserId">Owner of the pending draft.</param>
/// <param name="ConversationKey">Conversation scope key the draft is stored under.</param>

using System;
using Klacks.Api.Application.DTOs.PlanningProfile;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningProfile;

public record ApplyPlanningProfileCommand(Guid UserId, string ConversationKey) : IRequest<PlanningProfileApplyResult?>;
