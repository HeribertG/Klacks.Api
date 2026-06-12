// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Authentification;

public record GetPersonalAccessTokensQuery(string UserId) : IRequest<List<PersonalAccessTokenListItemDto>>;
