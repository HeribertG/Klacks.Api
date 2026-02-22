// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Annotation
{
    public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
