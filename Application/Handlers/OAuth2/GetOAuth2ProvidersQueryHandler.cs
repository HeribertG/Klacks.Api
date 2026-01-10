using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.OAuth2;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.OAuth2;

namespace Klacks.Api.Application.Handlers.OAuth2;

public class GetOAuth2ProvidersQueryHandler : IRequestHandler<GetOAuth2ProvidersQuery, IEnumerable<OAuth2ProviderResource>>
{
    private readonly IIdentityProviderRepository _providerRepository;

    public GetOAuth2ProvidersQueryHandler(IIdentityProviderRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<IEnumerable<OAuth2ProviderResource>> Handle(GetOAuth2ProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await _providerRepository.GetAuthenticationProviders();

        return providers
            .Where(p => p.Type == IdentityProviderType.OAuth2 || p.Type == IdentityProviderType.OpenIdConnect)
            .Select(p => new OAuth2ProviderResource
            {
                Id = p.Id,
                Name = p.Name,
                Type = p.Type
            });
    }
}
