using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Settings.CalendarRules;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Settings.CalendarRules;

public class RuleTokenListHandler : IRequestHandler<RuleTokenList, IEnumerable<StateCountryToken>>
{
  private readonly ICountryRepository _countryRepository;
  private readonly IStateRepository _repository;

  public RuleTokenListHandler(IStateRepository settingsRepository, ICountryRepository countryRepository)
  {
    _repository = settingsRepository;
    _countryRepository = countryRepository;
  }

  public async Task<IEnumerable<StateCountryToken>> Handle(RuleTokenList request, CancellationToken cancellationToken)
  {
    var states = await _repository.List();
    var countries = await _countryRepository.List();
    var result = new List<StateCountryToken>();
    foreach (var item in states)
    {
      var token = new StateCountryToken()
      {
        Id = item.Id,
        State = item.Abbreviation,
        StateName = item.Name,
        Country = item.CountryPrefix,
        CountryName = countries.Where(x => x.Abbreviation == item.CountryPrefix).Select(c => c.Name).First(),
        Select = request.IsSelected,
      };
      result.Add(token);
    }

    foreach (var item in countries)
    {
      var token = new StateCountryToken()
      {
        Id = item.Id,
        State = item.Abbreviation,
        StateName = item.Name,
        Country = item.Abbreviation,
        CountryName = item.Name,
        Select = request.IsSelected,
      };
      result.Add(token);
    }

    return result.OrderBy(x => x.Country == x.State).ThenBy(x => x.Country).ThenBy(x => x.State);
  }
}
