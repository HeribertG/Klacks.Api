using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ICountryRepository countryRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
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
                
                var result = _mapper.Map<CountryResource>(country);
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
