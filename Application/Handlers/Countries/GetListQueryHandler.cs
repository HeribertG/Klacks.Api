using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CountryResource>, IEnumerable<CountryResource>>
    {
        private readonly IMapper mapper;
        private readonly ICountryRepository repository;

        public GetListQueryHandler(IMapper mapper,
                                   ICountryRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<CountryResource>> Handle(ListQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            var country = await this.repository.List();
            return this.mapper.Map<List<Klacks.Api.Domain.Models.Settings.Countries>, List<CountryResource>>(country!);
        }
    }
}
