// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.States;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.State>;
