// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that finds a client, group or shift by name/abbreviation and returns the navigation
/// route to its editor. Single matches navigate directly (shifts additionally carry the
/// sealed-order lock-guidance note); ambiguous matches return a candidate list instructing
/// the model to let the user choose and then call navigate_to with the chosen GUID.
/// </summary>
/// <param name="clientSearchRepository">Search repository for clients (name, IdNumber)</param>
/// <param name="groupSearchRepository">Search repository for groups (name)</param>
/// <param name="shiftSearchRepository">Search repository for shifts (name, abbreviation, client name)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Guidance;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_and_navigate")]
public class SearchAndNavigateSkill : BaseSkillImplementation
{
    private const string ClientEditRoute = "/workplace/edit-address";
    private const string ClientListRoute = "/workplace/client";
    private const string GroupEditRoute = "/workplace/edit-group";
    private const string GroupListRoute = "/workplace/group";
    private const string ShiftEditRoute = "/workplace/edit-shift";
    private const string ShiftListRoute = "/workplace/shift";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IGroupSearchRepository _groupSearchRepository;
    private readonly IShiftSearchRepository _shiftSearchRepository;

    public SearchAndNavigateSkill(
        IClientSearchRepository clientSearchRepository,
        IGroupSearchRepository groupSearchRepository,
        IShiftSearchRepository shiftSearchRepository)
    {
        _clientSearchRepository = clientSearchRepository;
        _groupSearchRepository = groupSearchRepository;
        _shiftSearchRepository = shiftSearchRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entityType = GetRequiredString(parameters, "entityType").ToLower();
        var searchQuery = GetRequiredString(parameters, "searchQuery");
        var action = GetParameter<string>(parameters, "action", "edit");
        var target = GetParameter<string>(parameters, "target");

        var resolvedAction = action ?? "edit";
        return entityType switch
        {
            "client" => await SearchClientsAsync(searchQuery, resolvedAction, target, cancellationToken),
            "group" => await SearchGroupsAsync(searchQuery, resolvedAction, target, cancellationToken),
            "shift" => await SearchShiftsAsync(searchQuery, resolvedAction, target, cancellationToken),
            _ => SkillResult.Error($"Unknown entity type: {entityType}")
        };
    }

    private async Task<SkillResult> SearchClientsAsync(
        string searchQuery,
        string action,
        string? target,
        CancellationToken cancellationToken)
    {
        var result = await _clientSearchRepository.SearchAsync(
            searchTerm: searchQuery,
            limit: 10,
            cancellationToken: cancellationToken);

        if (result.TotalCount == 0)
        {
            return SkillResult.Error($"No clients found matching '{searchQuery}'.");
        }

        var clients = result.Items.Select(c => new
        {
            c.Id,
            c.IdNumber,
            c.FirstName,
            c.LastName
        }).ToList();

        if (clients.Count == 1)
        {
            var client = clients[0];
            return CreateNavigationResult(
                "client",
                client.Id,
                $"{client.FirstName} {client.LastName}",
                action,
                target);
        }

        var navigationData = new
        {
            EntityType = "client",
            SearchQuery = searchQuery,
            Results = clients,
            Count = clients.Count,
            Message = $"Found {clients.Count} clients. Please select one."
        };

        return new SkillResult
        {
            Success = true,
            Data = navigationData,
            Message = $"Found {clients.Count} clients matching '{searchQuery}'. Show the user only each " +
                "candidate's name and IdNumber, nothing else — do not compare or mention any other field (e.g. " +
                "company), they are not populated for most clients and only confuse the user. Then ask which " +
                "one. Once they choose, call search_and_navigate again with searchQuery set to that person's " +
                "name plus their IdNumber (e.g. 'Lastname Firstname 6556') so it resolves to exactly one match " +
                "— carry the same 'target' parameter forward if the user originally asked to go to a specific " +
                "section (e.g. notes). Never pass the IdNumber, or any other number shown to the user, directly " +
                "as an entityId to navigate_to — it is not the database id.",
            Type = SkillResultType.Data
        };
    }

    private async Task<SkillResult> SearchGroupsAsync(
        string searchQuery,
        string action,
        string? target,
        CancellationToken cancellationToken)
    {
        var result = await _groupSearchRepository.SearchAsync(
            searchTerm: searchQuery,
            limit: 10,
            cancellationToken: cancellationToken);

        if (result.TotalCount == 0)
        {
            return SkillResult.Error($"No groups found matching '{searchQuery}'.");
        }

        var groups = result.Items.Select(g => new { g.Id, g.Name }).ToList();

        if (groups.Count == 1)
        {
            var group = groups[0];
            return CreateNavigationResult("group", group.Id, group.Name ?? searchQuery, action, target);
        }

        return new SkillResult
        {
            Success = true,
            Data = new { EntityType = "group", Results = groups, Count = groups.Count },
            Message = $"Found {groups.Count} groups matching '{searchQuery}'. Please select one.",
            Type = SkillResultType.Data
        };
    }

    private async Task<SkillResult> SearchShiftsAsync(
        string searchQuery,
        string action,
        string? target,
        CancellationToken cancellationToken)
    {
        var result = await _shiftSearchRepository.SearchAsync(
            searchTerm: searchQuery,
            limit: 10,
            cancellationToken: cancellationToken);

        if (result.TotalCount == 0)
        {
            return SkillResult.Error($"No shifts found matching '{searchQuery}'.");
        }

        var shifts = result.Items.Select(s => new
        {
            s.Id,
            s.FromDate,
            s.Abbreviation,
            s.Name,
            s.Status,
            ClientName = $"{s.ClientFirstName} {s.ClientLastName}".Trim(),
            DisplayName = BuildShiftDisplayName(s.Abbreviation, s.Name, s.ClientFirstName, s.ClientLastName)
        }).ToList();

        if (shifts.Count == 1)
        {
            var shift = shifts[0];
            return CreateNavigationResult(
                "shift",
                shift.Id,
                shift.DisplayName,
                action,
                target,
                extraGuidance: ShiftLockGuidanceText.ForStatus(shift.Status));
        }

        return new SkillResult
        {
            Success = true,
            Data = new { EntityType = "shift", Results = shifts, Count = shifts.Count },
            Message = $"Found {shifts.Count} shifts matching '{searchQuery}'. Show the user each " +
                "candidate's DisplayName and FromDate, then ask which one. Once they choose, call " +
                "navigate_to with page='edit-shift', entityId set to that shift's Id field (the GUID — " +
                "never its DisplayName, Name, or Abbreviation), and the same target if one was originally " +
                "requested. Do not call search_and_navigate again with the DisplayName as searchQuery — it " +
                "contains formatting (parentheses) that will not match.",
            Type = SkillResultType.Data
        };
    }

    private static string BuildShiftDisplayName(
        string? abbreviation,
        string? name,
        string? clientFirstName,
        string? clientLastName)
    {
        string? shiftName = null;
        if (!string.IsNullOrWhiteSpace(name))
        {
            shiftName = !string.IsNullOrWhiteSpace(abbreviation) ? $"{name} ({abbreviation})" : name;
        }
        else if (!string.IsNullOrWhiteSpace(abbreviation))
        {
            shiftName = abbreviation;
        }

        var clientName = $"{clientFirstName} {clientLastName}".Trim();

        if (shiftName != null && clientName.Length > 0) return $"{shiftName} – {clientName}";
        if (shiftName != null) return shiftName;
        if (clientName.Length > 0) return clientName;
        return "Shift";
    }

    private static SkillResult CreateNavigationResult(
        string entityType,
        Guid? entityId,
        string entityName,
        string action,
        string? target = null,
        string? extraGuidance = null)
    {
        var route = entityType switch
        {
            "client" => entityId.HasValue ? $"{ClientEditRoute}/{entityId}" : ClientListRoute,
            "group" => entityId.HasValue ? $"{GroupEditRoute}/{entityId}" : GroupListRoute,
            "shift" => entityId.HasValue ? $"{ShiftEditRoute}/{entityId}" : ShiftListRoute,
            _ => "/"
        };

        var navigationData = new
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Action = action,
            Route = route,
            Target = target
        };

        var message = entityId.HasValue
            ? $"Navigate to {entityType} '{entityName}' ({action})"
            : $"Navigate to {entityType} list";
        if (!string.IsNullOrEmpty(extraGuidance))
        {
            message += " " + extraGuidance;
        }

        return SkillResult.Navigation(navigationData, message);
    }
}
