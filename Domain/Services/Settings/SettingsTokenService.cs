using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.Extensions.Logging;

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
        _logger = logger;
    }

    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenListAsync(bool isSelected)
    {
        _logger.LogInformation("Getting rule token list with isSelected: {IsSelected}", isSelected);
        
        var states = await _stateRepository.List();
        var countries = await _countryRepository.List();
        var tokens = new List<StateCountryToken>();
        
        // Transform states into tokens
        foreach (var state in states)
        {
            tokens.Add(new StateCountryToken
            {
                Id = state.Id,
                State = state.Abbreviation,
                StateName = state.Name,
                Country = string.Empty,
                CountryName = new MultiLanguage(),
                Select = isSelected
            });
        }
        
        // Transform countries into tokens
        foreach (var country in countries)
        {
            tokens.Add(new StateCountryToken
            {
                Id = country.Id,
                Country = country.Abbreviation,
                CountryName = country.Name,
                State = string.Empty,
                StateName = new MultiLanguage(),
                Select = isSelected
            });
        }
        
        _logger.LogInformation("Retrieved {Count} rule tokens", tokens.Count);
        return tokens;
    }
}