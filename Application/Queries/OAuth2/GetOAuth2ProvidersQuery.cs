using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.OAuth2;

namespace Klacks.Api.Application.Queries.OAuth2;

public record GetOAuth2ProvidersQuery() : IRequest<IEnumerable<OAuth2ProviderResource>>;
