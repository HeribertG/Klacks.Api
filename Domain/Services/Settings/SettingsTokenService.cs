using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingsTokenService : ISettingsTokenService
{
    private readonly IStateRepository _stateRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ILogger<SettingsTokenService> _logger;

    public SettingsTokenService(
        IStateRepository stateRepository,
        ICountryRepository countryRepository,
        ILogger<SettingsTokenService> logger)
    {
        _stateRepository = stateRepository;
        _countryRepository = countryRepository;
        this._logger = logger;
    }

    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenListAsync(bool isSelected)
    {
        _logger.LogInformation("Getting rule token list with isSelected: {IsSelected}", isSelected);
        
        var states = await _stateRepository.List();
        var countries = await _countryRepository.List();
        var tokens = new List<StateCountryToken>();
        
        foreach (var state in states)
        {
            var country = countries.FirstOrDefault(c => c.Abbreviation == state.CountryPrefix);
            if (country != null)
            {
                tokens.Add(new StateCountryToken
                {
                    Id = state.Id,
                    State = state.Abbreviation,
                    StateName = state.Name,
                    Country = country.Abbreviation,
                    CountryName = country.Name,
                    Select = isSelected
                });
            }
        }
        
        foreach (var country in countries)
        {
            tokens.Add(new StateCountryToken
            {
                Id = country.Id,
                Country = country.Abbreviation,
                CountryName = country.Name,
                State = country.Abbreviation,
                StateName = country.Name,
                Select = isSelected
            });
        }
        
        _logger.LogInformation("Retrieved {Count} rule tokens", tokens.Count);
        return tokens;
    }
}