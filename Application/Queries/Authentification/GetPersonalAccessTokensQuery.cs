// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Authentification;

public record GetPersonalAccessTokensQuery(string UserId) : IRequest<List<PersonalAccessTokenListItemDto>>;
