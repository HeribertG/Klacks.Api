using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.Countries
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
      return this.mapper.Map<List<Models.Settings.Countries>, List<CountryResource>>(country!);
    }
  }
}
