// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;

namespace Klacks.Api.Application.Queries.OAuth2;

public record GetOAuth2LogoutUrlQuery(Guid ProviderId, string? PostLogoutRedirectUri) : IRequest<OAuth2LogoutUrlResponse>;
