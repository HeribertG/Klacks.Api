using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IMembershipRepository membershipRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _membershipRepository = membershipRepository;
            _mapper = mapper;
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
                
                var result = _mapper.Map<MembershipResource>(membership);
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
