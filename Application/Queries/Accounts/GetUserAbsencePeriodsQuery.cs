// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetUserAbsencePeriodsQuery(string AppUserId) : IRequest<IReadOnlyList<UserAbsencePeriodResource>>;
