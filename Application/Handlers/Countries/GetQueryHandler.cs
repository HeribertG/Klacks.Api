using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(ICountryRepository countryRepository, IMapper mapper)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
        }

        public async Task<CountryResource> Handle(GetQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            var country = await _countryRepository.Get(request.Id);
            return country != null ? _mapper.Map<CountryResource>(country) : new CountryResource();
        }
    }
}
