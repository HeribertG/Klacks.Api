using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class CountryApplicationService
{
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CountryApplicationService> _logger;

    public CountryApplicationService(
        ICountryRepository countryRepository,
        IMapper mapper,
        ILogger<CountryApplicationService> logger)
    {
        _countryRepository = countryRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CountryResource?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var country = await _countryRepository.Get(id);
        return country != null ? _mapper.Map<CountryResource>(country) : null;
    }

    public async Task<List<CountryResource>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _countryRepository.List();
        return _mapper.Map<List<CountryResource>>(countries);
    }

    public async Task<CountryResource> CreateCountryAsync(CountryResource countryResource, CancellationToken cancellationToken = default)
    {
        var country = _mapper.Map<Countries>(countryResource);
        await _countryRepository.Add(country);
        return _mapper.Map<CountryResource>(country);
    }

    public async Task<CountryResource> UpdateCountryAsync(CountryResource countryResource, CancellationToken cancellationToken = default)
    {
        var country = _mapper.Map<Countries>(countryResource);
        var updatedCountry = await _countryRepository.Put(country);
        return _mapper.Map<CountryResource>(updatedCountry);
    }

    public async Task DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _countryRepository.Delete(id);
    }
}