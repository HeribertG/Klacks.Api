using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Resource.Id);
            if (existingMembership == null)
            {
                throw new KeyNotFoundException($"Membership with ID {request.Resource.Id} not found.");
            }

            _mapper.Map(request.Resource, existingMembership);
            await _membershipRepository.Put(existingMembership);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<MembershipResource>(existingMembership);
        }, 
        "updating membership", 
        new { MembershipId = request.Resource.Id });
    }
}
