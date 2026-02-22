// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries;

public record ListQuery<TModel>() : IRequest<IEnumerable<TModel>>;
