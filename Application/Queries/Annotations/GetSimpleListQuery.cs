// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Annotation
{
    public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
