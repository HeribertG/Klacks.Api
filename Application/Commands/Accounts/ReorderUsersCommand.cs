// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

/// <param name="OrderedUserIds">The full desired display order, first user first.</param>
public record ReorderUsersCommand(IReadOnlyList<string> OrderedUserIds) : IRequest<HttpResultResource>;
