using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.OAuth2;

namespace Klacks.Api.Application.Queries.OAuth2;

public record GetOAuth2AuthorizeQuery(Guid ProviderId, string RedirectUri) : IRequest<OAuth2AuthorizeResponse>;
