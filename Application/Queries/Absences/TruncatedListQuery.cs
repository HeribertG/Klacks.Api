// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Absences
{
    public record TruncatedListQuery(AbsenceFilter Filter) : IRequest<TruncatedAbsence>;
}
