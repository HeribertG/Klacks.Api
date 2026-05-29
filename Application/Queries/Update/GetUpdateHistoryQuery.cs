// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Update;

public record GetUpdateHistoryQuery(int Take) : IRequest<IReadOnlyList<UpdateHistoryItem>>;
