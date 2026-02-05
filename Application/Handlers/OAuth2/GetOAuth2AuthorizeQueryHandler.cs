using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.OAuth2;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;

namespace Klacks.Api.Application.Handlers.OAuth2;

public class GetOAuth2AuthorizeQueryHandler : IRequestHandler<GetOAuth2AuthorizeQuery, OAuth2AuthorizeResponse>
{
    private readonly IIdentityProviderRepository _providerRepository;
    private readonly IOAuth2Service _oauth2Service;
    private readonly ILogger<GetOAuth2AuthorizeQueryHandler> _logger;

    public GetOAuth2AuthorizeQueryHandler(
        IIdentityProviderRepository providerRepository,
        IOAuth2Service oauth2Service,
        ILogger<GetOAuth2AuthorizeQueryHandler> logger)
    {
        _providerRepository = providerRepository;
        _oauth2Service = oauth2Service;
        _logger = logger;
    }

    public async Task<OAuth2AuthorizeResponse> Handle(GetOAuth2AuthorizeQuery request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.Get(request.ProviderId);
        if (provider == null)
        {
            throw new NotFoundException("Provider not found");
        }

        if (provider.Type != IdentityProviderType.OAuth2 && provider.Type != IdentityProviderType.OpenIdConnect)
        {
            throw new BadRequestException("Provider is not an OAuth2/OpenID Connect provider");
        }

        var state = $"{request.ProviderId}_{Guid.NewGuid():N}";
        var authUrl = _oauth2Service.GetAuthorizationUrl(provider, request.RedirectUri, state);

        _logger.LogInformation("[OAUTH2] Authorization URL generated for provider {Provider}", provider.Name);

        return new OAuth2AuthorizeResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        };
    }
}
