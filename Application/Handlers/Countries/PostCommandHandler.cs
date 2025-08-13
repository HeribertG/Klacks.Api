using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class PostCommandHandler : IRequestHandler<PostCommand<CountryResource>, CountryResource?>
{
    private readonly CountryApplicationService _countryApplicationService;

    public PostCommandHandler(CountryApplicationService countryApplicationService)
    {
        _countryApplicationService = countryApplicationService;
    }

    public async Task<CountryResource?> Handle(PostCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        return await _countryApplicationService.CreateCountryAsync(request.Resource, cancellationToken);
    }
}
