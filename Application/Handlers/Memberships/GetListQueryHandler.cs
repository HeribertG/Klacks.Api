using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IMembershipRepository membershipRepository, ScheduleMapper scheduleMapper, ILogger<GetListQueryHandler> logger)
        {
            _membershipRepository = membershipRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MembershipResource>> Handle(ListQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching memberships list");
            
            try
            {
                var memberships = await _membershipRepository.List();
                var membershipsList = memberships.ToList();

                _logger.LogInformation($"Retrieved {membershipsList.Count} memberships");

                return membershipsList.Select(m => _scheduleMapper.ToMembershipResource(m)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching memberships list");
                throw new InvalidRequestException($"Failed to retrieve memberships list: {ex.Message}");
            }
        }
    }
}
