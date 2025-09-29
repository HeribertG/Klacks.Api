using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using Klacks.Api.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Vat>>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<ListQueryHandler> _logger;

        public ListQueryHandler(ISettingsRepository settingsRepository, ILogger<ListQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Vat>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching VAT list");
            
            try
            {
                var vats = await _settingsRepository.GetVATList();
                var vatsList = vats.ToList();
                
                _logger.LogInformation($"Successfully retrieved {vatsList.Count} VAT entries");
                
                return vatsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching VAT list");
                throw new InvalidRequestException($"Failed to retrieve VAT list: {ex.Message}");
            }
        }
    }
}
