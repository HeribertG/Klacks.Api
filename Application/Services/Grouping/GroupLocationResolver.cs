// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a single group's coordinates automatically: skips groups that already have coordinates,
/// classifies whether the group is a real place (LLM + parent hierarchy via <see cref="IGroupPlaceClassifier"/>),
/// and only when the verdict is a place with sufficient confidence geocodes the name and persists the
/// coordinates. Every uncertain path leaves the group untouched, because a wrong coordinate would feed
/// a wrong customer assignment downstream. Idempotent and safe to re-run.
/// </summary>
/// <param name="groupRepository">Loads the group + its parent path and persists the resolved coordinates.</param>
/// <param name="classifier">Decides whether the group name denotes a place (with confidence).</param>
/// <param name="geocoder">Resolves the place name to coordinates.</param>
/// <param name="unitOfWork">Commits the coordinate change.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupLocationResolver : IGroupLocationResolver
{
    private const double AcceptThreshold = 0.75;

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupPlaceClassifier _classifier;
    private readonly IGroupGeocoder _geocoder;
    private readonly IUnitOfWork _unitOfWork;

    public GroupLocationResolver(
        IGroupRepository groupRepository,
        IGroupPlaceClassifier classifier,
        IGroupGeocoder geocoder,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _classifier = classifier;
        _geocoder = geocoder;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupLocationResolveResult> ResolveAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return new GroupLocationResolveResult(groupId, string.Empty, GroupLocationResolveOutcome.NotFound, null, null);
        }

        if (group.Latitude.HasValue && group.Longitude.HasValue)
        {
            return new GroupLocationResolveResult(group.Id, group.Name, GroupLocationResolveOutcome.AlreadySet,
                group.Latitude, group.Longitude);
        }

        var path = await _groupRepository.GetPath(groupId);
        var parentHierarchy = path
            .Where(g => g.Id != groupId)
            .Select(g => g.Name)
            .ToList();

        var classification = await _classifier.ClassifyAsync(group.Name, parentHierarchy, cancellationToken);
        if (!classification.IsPlace || classification.Confidence < AcceptThreshold)
        {
            return new GroupLocationResolveResult(group.Id, group.Name, GroupLocationResolveOutcome.NotAPlace, null, null);
        }

        var placeName = string.IsNullOrWhiteSpace(classification.CanonicalName) ? group.Name : classification.CanonicalName;
        var (latitude, longitude) = await _geocoder.GeocodeAsync(placeName, cancellationToken);
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return new GroupLocationResolveResult(group.Id, group.Name, GroupLocationResolveOutcome.GeocodeFailed, null, null);
        }

        group.Latitude = latitude;
        group.Longitude = longitude;
        await _groupRepository.Put(group);
        await _unitOfWork.CompleteAsync();

        return new GroupLocationResolveResult(group.Id, group.Name, GroupLocationResolveOutcome.Resolved, latitude, longitude);
    }
}
