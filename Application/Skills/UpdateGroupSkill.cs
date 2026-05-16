// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing group's name, description, validity dates. Reparenting must go
/// through MoveNode (separate skill — not implemented in S5) to keep the nested-set
/// invariants intact.
/// </summary>
/// <param name="groupId">Required. UUID of the group to update.</param>
/// <param name="name">Optional. New display name.</param>
/// <param name="description">Optional. New description.</param>
/// <param name="validFrom">Optional. ISO date.</param>
/// <param name="validUntil">Optional. ISO date.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_group")]
public class UpdateGroupSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGroupSkill(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID '{groupId}' not found.");
        }

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name != group.Name)
        {
            group.Name = name;
            changed.Add("name");
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description != group.Description)
        {
            group.Description = description;
            changed.Add("description");
        }

        var validFromStr = GetParameter<string>(parameters, "validFrom");
        if (!string.IsNullOrEmpty(validFromStr) && DateTime.TryParse(validFromStr, out var parsedValidFrom))
        {
            if (group.ValidFrom != parsedValidFrom)
            {
                group.ValidFrom = parsedValidFrom;
                changed.Add("validFrom");
            }
        }

        var validUntilStr = GetParameter<string>(parameters, "validUntil");
        if (validUntilStr != null)
        {
            DateTime? newValidUntil = null;
            if (validUntilStr.Length > 0 && DateTime.TryParse(validUntilStr, out var parsedValidUntil))
            {
                newValidUntil = parsedValidUntil;
            }
            if (group.ValidUntil != newValidUntil)
            {
                group.ValidUntil = newValidUntil;
                changed.Add("validUntil");
            }
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { GroupId = groupId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — group left unchanged.");
        }

        group.UpdateTime = DateTime.UtcNow;
        group.CurrentUserUpdated = context.UserName;

        await _groupRepository.Put(group);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                GroupId = groupId,
                ChangedFields = changed,
                group.Name,
                group.ValidFrom,
                group.ValidUntil
            },
            $"Group '{group.Name}' updated ({string.Join(", ", changed)}).");
    }
}
