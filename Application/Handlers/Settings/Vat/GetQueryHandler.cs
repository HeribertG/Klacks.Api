using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using Klacks.Api.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISettingsRepository settingsRepository, ILogger<GetQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching VAT with ID: {request.Id}");
            
            try
            {
                var vat = await _settingsRepository.GetVAT(request.Id);
                
                _logger.LogInformation($"Successfully retrieved VAT with ID: {request.Id}");
                
                return vat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching VAT with ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve VAT: {ex.Message}");
            }
        }
    }
}
