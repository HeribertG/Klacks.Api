// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bulk-links every shift whose name/abbreviation matches a search term to a named group in one
/// server-side round trip. With apply=false (default) it returns a read-only preview of which shifts would
/// be linked; with apply=true it persists the links inside a transaction and re-reads each one from the
/// database to confirm it before committing, rolling the whole batch back on any mismatch.
/// </summary>
/// <param name="groupName">Name (or partial name) of the target group.</param>
/// <param name="searchTerm">Text matched against shift name/abbreviation to select the shifts to link.</param>
/// <param name="apply">When true the matches are linked; when false (default) only a preview is returned.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("bulk_add_shifts_to_group")]
public class BulkAddShiftsToGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "bulk_add_shifts_to_group";
    private const int MaxShifts = 100;

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public BulkAddShiftsToGroupSkill(
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var searchTerm = (GetParameter<string>(parameters, "searchTerm") ?? string.Empty).Trim();
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return SkillResult.Error("Parameter 'searchTerm' is required to select the shifts to add.");
        }

        var groups = await _groupRepository.List();
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        var filter = new ShiftFilter
        {
            SearchString = searchTerm,
            NumberOfItemsPerPage = MaxShifts,
            ActiveDateRange = true,
            FormerDateRange = true,
            FutureDateRange = true,
            IncludeClientName = false
        };

        var search = await _mediator.Send(new GetTruncatedListQuery(filter), cancellationToken);
        var matchedShifts = (search.Shifts ?? new List<ShiftResource>()).ToList();

        if (matchedShifts.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { Applied = apply, GroupName = group.Name, TotalMatchCount = 0, AddedCount = 0, VerifiedCount = 0, AlreadyLinkedCount = 0 },
                $"No shifts match '{searchTerm}'.");
        }

        var alreadyLinked = (await _groupItemRepository
            .GetShiftIdsByGroupIds(new List<Guid> { group.Id }, cancellationToken)).ToHashSet();

        var now = DateTime.UtcNow;
        var skipped = 0;
        var newItems = new List<GroupItem>();
        foreach (var shift in matchedShifts)
        {
            if (alreadyLinked.Contains(shift.Id))
            {
                skipped++;
                continue;
            }

            newItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ShiftId = shift.Id,
                GroupId = group.Id,
                ValidFrom = now,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
        }

        if (!apply)
        {
            var previewNames = string.Join(", ",
                matchedShifts.Where(s => !alreadyLinked.Contains(s.Id)).Select(s => s.Name));
            var skippedNote = skipped > 0 ? $" ({skipped} already linked)" : string.Empty;
            return SkillResult.SuccessResult(
                new { Applied = false, GroupName = group.Name, TotalMatchCount = matchedShifts.Count, AddedCount = 0, VerifiedCount = 0, AlreadyLinkedCount = skipped },
                $"Preview: {newItems.Count} of {matchedShifts.Count} shift(s) matching '{searchTerm}' " +
                $"would be added to group '{group.Name}'{skippedNote}: {previewNames}. Nothing was changed yet. " +
                "Ask the user to confirm, then call again with apply=true.");
        }

        var verified = 0;
        if (newItems.Count > 0)
        {
            try
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
                            $"Database verification failed: expected {newItems.Count} new shift links in group " +
                            $"'{group.Name}' but only {confirmed} were confirmed — the changes were rolled back.");
                    }

                    return confirmed;
                });
            }
            catch (SkillVerificationException ex)
            {
                return SkillResult.Error(ex.Message);
            }
        }

        var alreadyNote = skipped > 0 ? $" ({skipped} were already linked)" : string.Empty;
        return SkillResult.SuccessResult(
            new { Applied = true, GroupName = group.Name, TotalMatchCount = matchedShifts.Count, AddedCount = newItems.Count, VerifiedCount = verified, AlreadyLinkedCount = skipped },
            $"Added {newItems.Count} shift(s) to group '{group.Name}' " +
            $"and confirmed {verified} in the database (verified){alreadyNote}.");
    }
}
