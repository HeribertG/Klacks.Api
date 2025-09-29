using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(IMembershipRepository membershipRepository, IMapper mapper, ILogger<GetListQueryHandler> logger)
        {
            _membershipRepository = membershipRepository;
            _mapper = mapper;
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
                
                return _mapper.Map<IEnumerable<MembershipResource>>(membershipsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching memberships list");
                throw new InvalidRequestException($"Failed to retrieve memberships list: {ex.Message}");
            }
        }
    }
}
