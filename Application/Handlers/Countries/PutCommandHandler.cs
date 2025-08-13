using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class PutCommandHandler : IRequestHandler<PutCommand<CountryResource>, CountryResource?>
{
    private readonly CountryApplicationService _countryApplicationService;

    public PutCommandHandler(CountryApplicationService countryApplicationService)
    {
        _countryApplicationService = countryApplicationService;
    }

    public async Task<CountryResource?> Handle(PutCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        return await _countryApplicationService.UpdateCountryAsync(request.Resource, cancellationToken);
    }
}
