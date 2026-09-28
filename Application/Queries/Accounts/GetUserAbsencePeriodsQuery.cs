// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetUserAbsencePeriodsQuery(string AppUserId) : IRequest<IReadOnlyList<UserAbsencePeriodResource>>;
