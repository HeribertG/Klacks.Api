// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant.Learning;

public sealed record GetUnfulfillableWishesQuery(int Limit) : IRequest<IReadOnlyList<UnfulfillableWishDto>>;
