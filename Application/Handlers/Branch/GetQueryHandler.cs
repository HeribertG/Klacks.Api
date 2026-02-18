using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Branch;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Branch
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Branch?>
    {
        private readonly IBranchRepository _branchRepository;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IBranchRepository branchRepository, ILogger<GetQueryHandler> logger)
        {
            _branchRepository = branchRepository;
            _logger = logger;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Branch?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching branch with ID: {Id}", request.Id);

            try
            {
                var branch = await _branchRepository.Get(request.Id);

                _logger.LogInformation("Successfully retrieved branch with ID: {Id}", request.Id);

                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching branch with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Failed to retrieve branch: {ex.Message}");
            }
        }
    }
}
