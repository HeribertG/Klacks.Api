using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ContractResource>, ContractResource>
    {
        private readonly IContractRepository _contractRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IContractRepository contractRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        {
            _contractRepository = contractRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<ContractResource> Handle(GetQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting contract with ID: {Id}", request.Id);
                
                var contract = await _contractRepository.Get(request.Id);
                
                if (contract == null)
                {
                    throw new KeyNotFoundException($"Contract with ID {request.Id} not found");
                }
                
                var result = _scheduleMapper.ToContractResource(contract);
                _logger.LogInformation("Successfully retrieved contract with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving contract with ID {request.Id}: {ex.Message}");
            }
        }
    }
}