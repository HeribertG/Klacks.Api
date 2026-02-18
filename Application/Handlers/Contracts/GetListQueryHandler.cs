using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<ContractResource>, IEnumerable<ContractResource>>
    {
        private readonly IContractRepository _contractRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IContractRepository contractRepository, ScheduleMapper scheduleMapper, ILogger<GetListQueryHandler> logger)
        {
            _contractRepository = contractRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ContractResource>> Handle(ListQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching contracts list");
            
            try
            {
                var contracts = await _contractRepository.List();
                var contractsList = contracts.ToList();

                _logger.LogInformation("Retrieved {Count} contracts", contractsList.Count);

                return contractsList.Select(c => _scheduleMapper.ToContractResource(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching contracts");
                throw new InvalidRequestException($"Failed to retrieve contracts: {ex.Message}");
            }
        }
    }
}