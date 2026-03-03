// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.Absences;

public record VisibleAbsencesQuery : IRequest<List<AbsenceResource>>;
