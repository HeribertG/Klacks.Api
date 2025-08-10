using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Settings.Countries
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly IMapper mapper;
        private readonly ICountryRepository repository;

        public GetQueryHandler(IMapper mapper,
                               ICountryRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<CountryResource> Handle(GetQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            var country = await this.repository.Get(request.Id);
            return this.mapper.Map<Klacks.Api.Domain.Models.Settings.Countries, CountryResource>(country!);
        }
    }
}
