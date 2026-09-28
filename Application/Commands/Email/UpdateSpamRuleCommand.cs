// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Email;

public record UpdateSpamRuleCommand(Guid Id, SpamRuleType RuleType, string Pattern, bool IsActive, int SortOrder) : IRequest<SpamRuleResource>;
