using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    /// <summary>
    /// CQRS Handler for getting group tree structure
    /// Refactored to use Application Service following Clean Architecture
    /// </summary>
    public class GetGroupTreeQueryHandler : IRequestHandler<GetGroupTreeQuery, GroupTreeResource>
    {
        private readonly GroupApplicationService _groupApplicationService;
        private readonly ILogger<GetGroupTreeQueryHandler> _logger;

        public GetGroupTreeQueryHandler(
            GroupApplicationService groupApplicationService,
            ILogger<GetGroupTreeQueryHandler> logger)
        {
            _groupApplicationService = groupApplicationService;
            _logger = logger;
        }

        public async Task<GroupTreeResource> Handle(GetGroupTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing GetGroupTreeQuery with rootId: {request.RootId}");

                // Clean Architecture: Delegate complex tree operations to Application Service
                var result = await _groupApplicationService.GetGroupTreeAsync(request.RootId, cancellationToken);

                _logger.LogInformation($"Retrieved tree with {result.Nodes.Count} root nodes");

                return result;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Group tree with root {request.RootId} not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing GetGroupTreeQuery with rootId: {request.RootId}");
                throw;
            }
        }
    }
}