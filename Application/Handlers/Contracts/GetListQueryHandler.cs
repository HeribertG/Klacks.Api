using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<ContractResource>, IEnumerable<ContractResource>>
    {
        private readonly IContractRepository _contractRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IContractRepository contractRepository, IMapper mapper, ILogger<GetListQueryHandler> logger)
        {
            _contractRepository = contractRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ContractResource>> Handle(ListQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching contracts list");
            
            try
            {
                var contracts = await _contractRepository.List();
                var contractsList = contracts.ToList();
                
                _logger.LogInformation($"Retrieved {contractsList.Count} contracts");
                
                return _mapper.Map<IEnumerable<ContractResource>>(contractsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching contracts");
                throw new InvalidRequestException($"Failed to retrieve contracts: {ex.Message}");
            }
        }
    }
}