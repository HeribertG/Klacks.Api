using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CountryResource>, IEnumerable<CountryResource>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(ICountryRepository countryRepository, IMapper mapper, ILogger<GetListQueryHandler> logger)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CountryResource>> Handle(ListQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching countries list");
            
            try
            {
                var countries = await _countryRepository.List();
                var countriesList = countries.ToList();
                
                _logger.LogInformation($"Retrieved {countriesList.Count} countries");
                
                return _mapper.Map<IEnumerable<CountryResource>>(countriesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching countries");
                throw new InvalidRequestException($"Failed to retrieve countries: {ex.Message}");
            }
        }
    }
}
