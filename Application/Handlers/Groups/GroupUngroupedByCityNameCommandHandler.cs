// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="GroupUngroupedByCityNameCommand"/>. Loads all employees together with their
/// addresses and group memberships, keeps those without any active (non-scenario) membership, and maps
/// each one to the group whose name exactly matches (case-insensitive, trimmed) the city of its newest
/// employee address. With Apply=false it only previews; with Apply=true it persists the new memberships
/// in a single transaction and re-reads them, rolling everything back on a verification mismatch. Cities
/// without an equally named group and employees without an address are reported back instead of being
/// silently dropped, and group names that are not unique are ignored so no employee is assigned wrongly.
/// </summary>
/// <param name="clientRepository">Loads employees with their addresses and memberships.</param>
/// <param name="groupRepository">Provides the groups whose names are matched against address cities.</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date used when no explicit ValidFrom is given.</param>
public sealed class GroupUngroupedByCityNameCommandHandler
    : IRequestHandler<GroupUngroupedByCityNameCommand, GroupUngroupedByCityNameResult>
{
    private const string SkillName = "group_ungrouped_by_city_name";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public GroupUngroupedByCityNameCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<GroupUngroupedByCityNameResult> Handle(
        GroupUngroupedByCityNameCommand request, CancellationToken cancellationToken)
    {
        var employees = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            EntityTypeEnum.Employee, cancellationToken);

        var ungrouped = employees
            .Where(c => !c.GroupItems.Any(gi => gi.AnalyseToken == null && !gi.IsDeleted))
            .ToList();

        var groups = await _groupRepository.List();
        var groupsByName = groups
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(byName => byName.Count() == 1)
            .ToDictionary(byName => byName.Key, byName => byName.First(), StringComparer.OrdinalIgnoreCase);

        var assignments = new List<GroupCityAssignment>();
        var unmatchedByCity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var noAddress = 0;

        foreach (var client in ungrouped)
        {
            var address = client.Addresses
                .Where(a => a.Type == AddressTypeEnum.Employee && !a.IsDeleted)
                .OrderByDescending(a => a.ValidFrom)
                .FirstOrDefault();

            var city = address?.City?.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                noAddress++;
                continue;
            }

            if (groupsByName.TryGetValue(city, out var group))
            {
                var name = $"{client.FirstName} {client.Name}".Trim();
                assignments.Add(new GroupCityAssignment(client.Id, name, city, group.Id, group.Name));
            }
            else
            {
                unmatchedByCity[city] = unmatchedByCity.GetValueOrDefault(city) + 1;
            }
        }

        if (request.Count is > 0 && assignments.Count > request.Count.Value)
        {
            assignments = assignments.Take(request.Count.Value).ToList();
        }

        var unmatchedCities = unmatchedByCity
            .Select(kv => new UnmatchedCityCount(kv.Key, kv.Value))
            .OrderByDescending(u => u.EmployeeCount)
            .ThenBy(u => u.City, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!request.Apply)
        {
            return new GroupUngroupedByCityNameResult(
                Applied: false,
                TotalUngrouped: ungrouped.Count,
                MatchCount: assignments.Count,
                AddedCount: 0,
                VerifiedCount: 0,
                NoAddressCount: noAddress,
                Assignments: assignments,
                UnmatchedCities: unmatchedCities);
        }

        var now = DateTime.UtcNow;
        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);
        var newItems = new List<GroupItem>();

        foreach (var assignment in assignments)
        {
            var existing = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, assignment.GroupId);
            if (existing != null && !existing.IsDeleted)
            {
                continue;
            }

            newItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = assignment.ClientId,
                GroupId = assignment.GroupId,
                ValidFrom = validFrom,
                CreateTime = now,
                CurrentUserCreated = request.UserName
            });
        }

        var verified = 0;
        if (newItems.Count > 0)
        {
            verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in newItems)
                {
                    await _groupItemRepository.Add(item);
                }

                await _unitOfWork.CompleteAsync();

                var confirmed = await _groupItemRepository.CountExistingByIds(
                    newItems.Select(i => i.Id).ToList(), cancellationToken);
                if (confirmed != newItems.Count)
                {
                    throw new SkillVerificationException(
                        SkillName,
                        $"Database verification failed: expected {newItems.Count} new memberships but only " +
                        $"{confirmed} were confirmed — the changes were rolled back.");
                }

                return confirmed;
            });
        }

        return new GroupUngroupedByCityNameResult(
            Applied: true,
            TotalUngrouped: ungrouped.Count,
            MatchCount: assignments.Count,
            AddedCount: newItems.Count,
            VerifiedCount: verified,
            NoAddressCount: noAddress,
            Assignments: assignments,
            UnmatchedCities: unmatchedCities);
    }
}
