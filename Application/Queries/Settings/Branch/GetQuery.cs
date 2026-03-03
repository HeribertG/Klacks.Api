// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.Branch;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.Branch>;
