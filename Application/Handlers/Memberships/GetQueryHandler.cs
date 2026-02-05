using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IMembershipRepository membershipRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        {
            _membershipRepository = membershipRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting membership with ID: {Id}", request.Id);
                
                var membership = await _membershipRepository.Get(request.Id);
                
                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {request.Id} not found");
                }
                
                var result = _scheduleMapper.ToMembershipResource(membership);
                _logger.LogInformation("Successfully retrieved membership with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving membership with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving membership with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
