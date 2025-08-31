using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Id);
            if (existingMembership == null)
            {
                throw new KeyNotFoundException($"Membership with ID {request.Id} not found.");
            }

            var membershipResource = _mapper.Map<MembershipResource>(existingMembership);
            await _membershipRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return membershipResource;
        }, 
        "deleting membership", 
        new { MembershipId = request.Id });
    }
}
