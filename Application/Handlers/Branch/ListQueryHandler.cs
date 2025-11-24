using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Branch;
using Klacks.Api.Domain.Exceptions;
using MediatR;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Branch>>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly ILogger<ListQueryHandler> _logger;

        public ListQueryHandler(IBranchRepository branchRepository, ILogger<ListQueryHandler> logger)
        {
            _branchRepository = branchRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Branch>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching branches list");

            try
            {
                var branches = await _branchRepository.List();
                var branchesList = branches;

                _logger.LogInformation($"Retrieved {branchesList.Count} branches");

                return branchesList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching branches list");
                throw new InvalidRequestException($"Failed to retrieve branches list: {ex.Message}");
            }
        }
    }
}
