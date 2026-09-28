// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

public record CreateUserAbsencePeriodCommand(
    string AppUserId, DateOnly StartDate, DateOnly EndDate, string? Reason) : IRequest<UserAbsencePeriodResource>;
