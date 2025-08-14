using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PutCommandHandler : IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingMembership = await _membershipRepository.Get(request.Resource.Id);
            if (existingMembership == null)
            {
                _logger.LogWarning("Membership with ID {MembershipId} not found.", request.Resource.Id);
                return null;
            }

            _mapper.Map(request.Resource, existingMembership);
            await _membershipRepository.Put(existingMembership);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<MembershipResource>(existingMembership);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating membership with ID {MembershipId}.", request.Resource.Id);
            throw;
        }
    }
}
