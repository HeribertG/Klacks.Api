using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CountryResource>, IEnumerable<CountryResource>>
    {
        private readonly CountryApplicationService _countryApplicationService;

        public GetListQueryHandler(CountryApplicationService countryApplicationService)
        {
            _countryApplicationService = countryApplicationService;
        }

        public async Task<IEnumerable<CountryResource>> Handle(ListQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            return await _countryApplicationService.GetAllCountriesAsync(cancellationToken);
        }
    }
}
