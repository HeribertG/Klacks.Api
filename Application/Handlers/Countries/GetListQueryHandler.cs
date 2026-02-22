// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CountryResource>, IEnumerable<CountryResource>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(ICountryRepository countryRepository, SettingsMapper settingsMapper, ILogger<GetListQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CountryResource>> Handle(ListQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching countries list");
            
            try
            {
                var countries = await _countryRepository.List();
                var countriesList = countries.ToList();
                
                _logger.LogInformation("Retrieved {Count} countries", countriesList.Count);
                
                return _settingsMapper.ToCountryResources(countriesList.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching countries");
                throw new InvalidRequestException($"Failed to retrieve countries: {ex.Message}");
            }
        }
    }
}
