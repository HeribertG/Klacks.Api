// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds the clients the user ticked in the UI list (the current selection, carried in the execution
/// context) to a group resolved by name, in a single server-side round trip. With apply=false (default)
/// it returns a read-only preview of who would be added; with apply=true it persists the memberships and
/// reports the database-verified count. Acts only on the ambient selection — the client ids are not a
/// parameter — so the recipe only needs the target group name.
/// </summary>
/// <param name="groupName">Name (or partial name) of the target group.</param>
/// <param name="apply">When true the selected clients are added; when false (default) only a preview is returned.</param>

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_selected_clients_to_group")]
public class AddSelectedClientsToGroupSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public AddSelectedClientsToGroupSkill(
        IGroupRepository groupRepository, IMediator mediator, ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _mediator = mediator;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        var selectedIds = context.SelectedEntityIds;
        if (selectedIds is not { Count: > 0 })
        {
            return SkillResult.Error(
                "No rows are selected in the list. Ask the user to tick the employees or clients in the " +
                "list first, then try again — this skill only acts on the current UI selection.");
        }

        var groups = await _groupRepository.List();
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        if (apply && validFrom is null)
        {
            return SkillResult.Error(
                SkillDateParser.MissingStartDateMessage(
                    $"add the selected client(s) to group '{group.Name}'"));
        }

        AddSelectedClientsToGroupResult result;
        try
        {
            result = await _mediator.Send(
                new AddSelectedClientsToGroupCommand(
                    group.Id, group.Name, selectedIds, validFrom, apply, context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (result.FoundCount == 0)
        {
            return SkillResult.Error(
                $"None of the {result.RequestedCount} selected row(s) match an existing client — the " +
                "selection may be stale. Ask the user to refresh the list and select again.");
        }

        var clientNames = string.Join(", ",
            result.Clients.Select(c => $"{c.FirstName} {c.LastName}".Trim()));
        var notFoundNote = result.NotFoundCount > 0
            ? $" ({result.NotFoundCount} selected row(s) no longer exist and were ignored)"
            : string.Empty;

        if (!apply)
        {
            if (result.EligibleCount == 0)
            {
                return SkillResult.SuccessResult(
                    result,
                    $"All {result.AlreadyMemberCount} selected client(s) are already members of group " +
                    $"'{result.GroupName}'. Nothing to add{notFoundNote}.");
            }

            var alreadyPreviewNote = result.AlreadyMemberCount > 0
                ? $" ({result.AlreadyMemberCount} already members)"
                : string.Empty;
            var validFromAsk = validFrom is null ? SkillDateParser.AskForStartDateInPreview : string.Empty;
            return SkillResult.SuccessResult(
                result,
                $"Preview: {result.EligibleCount} selected client(s) would be added to group " +
                $"'{result.GroupName}': {clientNames}{alreadyPreviewNote}{notFoundNote}. Nothing was changed " +
                $"yet.{validFromAsk} Ask the user to confirm, then call THIS skill " +
                "(add_selected_clients_to_group) again with apply=true and the same validFrom. The clients are " +
                "already identified by the selection — do NOT look anyone up or add them individually by name " +
                "(that is unreliable when several people share a name).");
        }

        if (result.AddedCount == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"No new memberships were created — all {result.AlreadyMemberCount} selected client(s) were " +
                $"already in group '{result.GroupName}'{notFoundNote}.");
        }

        var alreadyNote = result.AlreadyMemberCount > 0
            ? $" ({result.AlreadyMemberCount} were already members)"
            : string.Empty;
        return SkillResult.SuccessResult(
            result,
            $"Added {result.AddedCount} selected client(s) to group '{result.GroupName}' and confirmed " +
            $"{result.VerifiedCount} in the database (verified){alreadyNote}{notFoundNote}.");
    }
}
