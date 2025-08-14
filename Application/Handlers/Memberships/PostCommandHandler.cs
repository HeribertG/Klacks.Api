using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PostCommandHandler : IRequestHandler<PostCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(PostCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var membership = _mapper.Map<Membership>(request.Resource);
            await _membershipRepository.Add(membership);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<MembershipResource>(membership);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new membership.");
            throw;
        }
    }
}
