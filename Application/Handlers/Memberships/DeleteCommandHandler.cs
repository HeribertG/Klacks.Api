using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingMembership = await _membershipRepository.Get(request.Id);
            if (existingMembership == null)
            {
                _logger.LogWarning("Membership with ID {MembershipId} not found for deletion.", request.Id);
                return null;
            }

            var membershipResource = _mapper.Map<MembershipResource>(existingMembership);
            await _membershipRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return membershipResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting membership with ID {MembershipId}.", request.Id);
            throw;
        }
    }
}
