using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly CountryApplicationService _countryApplicationService;

        public GetQueryHandler(CountryApplicationService countryApplicationService)
        {
            _countryApplicationService = countryApplicationService;
        }

        public async Task<CountryResource> Handle(GetQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            return await _countryApplicationService.GetCountryByIdAsync(request.Id, cancellationToken) ?? new CountryResource();
        }
    }
}
