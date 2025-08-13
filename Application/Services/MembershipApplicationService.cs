using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class MembershipApplicationService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<MembershipApplicationService> _logger;

    public MembershipApplicationService(
        IMembershipRepository membershipRepository,
        IMapper mapper,
        ILogger<MembershipApplicationService> logger)
    {
        _membershipRepository = membershipRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<MembershipResource?> GetMembershipByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.Get(id);
        return membership != null ? _mapper.Map<MembershipResource>(membership) : null;
    }

    public async Task<List<MembershipResource>> GetAllMembershipsAsync(CancellationToken cancellationToken = default)
    {
        var memberships = await _membershipRepository.List();
        return _mapper.Map<List<MembershipResource>>(memberships);
    }

    public async Task<MembershipResource> CreateMembershipAsync(MembershipResource membershipResource, CancellationToken cancellationToken = default)
    {
        var membership = _mapper.Map<Membership>(membershipResource);
        await _membershipRepository.Add(membership);
        return _mapper.Map<MembershipResource>(membership);
    }

    public async Task<MembershipResource> UpdateMembershipAsync(MembershipResource membershipResource, CancellationToken cancellationToken = default)
    {
        var membership = _mapper.Map<Membership>(membershipResource);
        var updatedMembership = await _membershipRepository.Put(membership);
        return _mapper.Map<MembershipResource>(updatedMembership);
    }

    public async Task DeleteMembershipAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _membershipRepository.Delete(id);
    }
}