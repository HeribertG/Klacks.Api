using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CountryResource>, IEnumerable<CountryResource>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(ICountryRepository countryRepository, IMapper mapper)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CountryResource>> Handle(ListQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            var countries = await _countryRepository.List();
            return _mapper.Map<IEnumerable<CountryResource>>(countries);
        }
    }
}
