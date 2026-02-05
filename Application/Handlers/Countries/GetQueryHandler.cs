using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ICountryRepository countryRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<CountryResource> Handle(GetQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting country with ID: {Id}", request.Id);
                
                var country = await _countryRepository.Get(request.Id);
                
                if (country == null)
                {
                    throw new KeyNotFoundException($"Country with ID {request.Id} not found");
                }
                
                var result = _settingsMapper.ToCountryResource(country);
                _logger.LogInformation("Successfully retrieved country with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving country with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving country with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
