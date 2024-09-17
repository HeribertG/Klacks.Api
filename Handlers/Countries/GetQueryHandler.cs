using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Settings.Countries
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
      return this.mapper.Map<Models.Settings.Countries, CountryResource>(country!);
    }
  }
}
