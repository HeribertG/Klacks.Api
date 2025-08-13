using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CountryResource>, CountryResource?>
{
    private readonly CountryApplicationService _countryApplicationService;

    public DeleteCommandHandler(CountryApplicationService countryApplicationService)
    {
        _countryApplicationService = countryApplicationService;
    }

    public async Task<CountryResource?> Handle(DeleteCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        await _countryApplicationService.DeleteCountryAsync(request.Id, cancellationToken);
        return null;
    }
}
