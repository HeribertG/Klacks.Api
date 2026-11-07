// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates several stand-ins in one save, so either all of them are stored or none. Each one gets the display name of the
/// pattern with the next free index (StandInNamePattern, counted over the names of every client that is not deleted),
/// a membership and a membership of the group for the given period, and the given qualifications at
/// StandInConstants.QualificationLevel. A group outside the caller's visibility is answered exactly like a missing one,
/// and an unknown or deleted qualification refuses the request. Two concurrent requests with the same pattern can hand
/// out the same index: client names carry no unique index, so the names are then equal but the clients distinct.
/// </summary>
/// <param name="clientRepository">Reads the names in the pattern's frame and stages the new clients</param>
/// <param name="groupRepository">Checks that the group exists</param>
/// <param name="qualificationRepository">Checks that the qualifications exist</param>
/// <param name="groupVisibilityGuard">Checks that the caller sees the group</param>
/// <param name="unitOfWork">Saves all new clients at once</param>

using Klacks.Api.Application.Commands.Clients;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients;

public class CreateStandInsCommandHandler : BaseHandler, IRequestHandler<CreateStandInsCommand, List<CreatedStandInResource>>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found";
    private const string QualificationNotFoundMessage = "Qualification with ID {0} not found.";
    private const string IndexExhaustedMessage = "The name pattern has no free index left.";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStandInsCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IQualificationRepository qualificationRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<CreateStandInsCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _qualificationRepository = qualificationRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CreatedStandInResource>> Handle(CreateStandInsCommand request, CancellationToken cancellationToken)
    {
        var resource = request.Resource;

        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(resource.GroupId, cancellationToken)
                || !await _groupRepository.Exists(resource.GroupId))
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, resource.GroupId));
            }

            await EnsureQualificationsExistAsync(resource.QualificationIds ?? [], cancellationToken);

            var existingNames = await _clientRepository.ListNamesInFrameAsync(
                StandInNamePattern.Prefix(resource.NamePattern),
                StandInNamePattern.Suffix(resource.NamePattern),
                cancellationToken);
            var firstIndex = StandInNamePattern.NextFreeIndex(resource.NamePattern, existingNames);
            if (firstIndex + resource.Count - 1 > int.MaxValue)
            {
                throw new InvalidRequestException(IndexExhaustedMessage);
            }

            var standIns = Enumerable.Range((int)firstIndex, resource.Count)
                .Select(index => BuildStandIn(resource, StandInNamePattern.Expand(resource.NamePattern, index)))
                .ToList();

            foreach (var standIn in standIns)
            {
                await _clientRepository.Add(standIn);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Stand-ins created: {Count} in group {GroupId}", standIns.Count, resource.GroupId);

            return standIns
                .Select(standIn => new CreatedStandInResource { Id = standIn.Id, IdNumber = standIn.IdNumber, Name = standIn.Name })
                .ToList();
        },
        "creating stand-ins",
        new { resource.Count, resource.GroupId });
    }

    private async Task EnsureQualificationsExistAsync(IReadOnlyCollection<Guid> qualificationIds, CancellationToken cancellationToken)
    {
        if (qualificationIds.Count == 0)
        {
            return;
        }

        var existingIds = (await _qualificationRepository.GetAllAsync(cancellationToken)).Select(q => q.Id).ToHashSet();
        var missing = qualificationIds.Where(id => !existingIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidRequestException(string.Format(QualificationNotFoundMessage, missing[0]));
        }
    }

    private static Client BuildStandIn(CreateStandInsResource resource, string name)
    {
        var validFrom = resource.ValidFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var validUntil = resource.ValidUntil?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var standIn = new Client
        {
            Type = EntityTypeEnum.StandIn,
            Name = name,
            FirstName = string.Empty,
            Membership = new Membership { ValidFrom = validFrom, ValidUntil = validUntil },
        };

        standIn.GroupItems.Add(new GroupItem { GroupId = resource.GroupId, ValidFrom = validFrom, ValidUntil = validUntil });

        foreach (var qualificationId in resource.QualificationIds ?? [])
        {
            standIn.Qualifications.Add(new ClientQualification
            {
                QualificationId = qualificationId,
                Level = StandInConstants.QualificationLevel,
            });
        }

        return standIn;
    }
}
