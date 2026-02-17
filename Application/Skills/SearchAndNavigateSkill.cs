using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class SearchAndNavigateSkill : BaseSkill
{
    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IShiftRepository _shiftRepository;

    public override string Name => "search_and_navigate";

    public override string Description =>
        "Search for an entity (employee, customer, group, shift) by name and navigate directly to it. " +
        "Use this when the user wants to open or edit a specific person or entity " +
        "(e.g., 'Open customer Max Müller', 'Show me Heribert Gasparoli'). " +
        "If multiple matches are found, all will be shown.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewClients" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "entityType",
            "Type of entity to search for",
            SkillParameterType.Enum,
            Required: true,
            EnumValues: new List<string> { "client", "shift", "group" }),
        new SkillParameter(
            "searchQuery",
            "Name or search term to find the entity",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "action",
            "Action to take after finding: view or edit",
            SkillParameterType.Enum,
            Required: false,
            DefaultValue: "edit",
            EnumValues: new List<string> { "view", "edit" })
    };

    public SearchAndNavigateSkill(
        IClientSearchRepository clientSearchRepository,
        IGroupRepository groupRepository,
        IShiftRepository shiftRepository)
    {
        _clientSearchRepository = clientSearchRepository;
        _groupRepository = groupRepository;
        _shiftRepository = shiftRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entityType = GetRequiredString(parameters, "entityType").ToLower();
        var searchQuery = GetRequiredString(parameters, "searchQuery");
        var action = GetParameter<string>(parameters, "action", "edit");

        return entityType switch
        {
            "client" => await SearchClientsAsync(searchQuery, action, cancellationToken),
            "group" => await SearchGroupsAsync(searchQuery, action, cancellationToken),
            "shift" => await SearchShiftsAsync(searchQuery, action, cancellationToken),
            _ => SkillResult.Error($"Unknown entity type: {entityType}")
        };
    }

    private async Task<SkillResult> SearchClientsAsync(
        string searchQuery,
        string action,
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
            c.LastName,
            c.Company
        }).ToList();

        if (clients.Count == 1)
        {
            var client = clients[0];
            return CreateNavigationResult(
                "client",
                client.Id,
                $"{client.FirstName} {client.LastName}",
                action);
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
            Message = $"Found {clients.Count} clients matching '{searchQuery}'. Please select one to navigate.",
            Type = SkillResultType.Data
        };
    }

    private async Task<SkillResult> SearchGroupsAsync(
        string searchQuery,
        string action,
        CancellationToken cancellationToken)
    {
        var allGroups = await _groupRepository.List();
        var term = searchQuery.ToLower();

        var groups = allGroups
            .Where(g => !g.IsDeleted && g.Name != null && g.Name.ToLower().Contains(term))
            .OrderBy(g => g.Name)
            .Take(10)
            .Select(g => new { g.Id, g.Name })
            .ToList();

        if (groups.Count == 0)
        {
            return SkillResult.Error($"No groups found matching '{searchQuery}'.");
        }

        if (groups.Count == 1)
        {
            var group = groups[0];
            return CreateNavigationResult("group", group.Id, group.Name ?? searchQuery, action);
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
        CancellationToken cancellationToken)
    {
        var allShifts = await _shiftRepository.List();
        var term = searchQuery.ToLower();

        var shifts = allShifts
            .Where(s => !s.IsDeleted && s.Client != null &&
                       ((s.Client.FirstName != null && s.Client.FirstName.ToLower().Contains(term)) ||
                        (s.Client.Name != null && s.Client.Name.ToLower().Contains(term))))
            .OrderByDescending(s => s.FromDate)
            .Take(10)
            .Select(s => new
            {
                s.Id,
                s.FromDate,
                ClientName = s.Client != null ? $"{s.Client.FirstName} {s.Client.Name}" : "Unknown"
            })
            .ToList();

        if (shifts.Count == 0)
        {
            return SkillResult.Error($"No shifts found matching '{searchQuery}'.");
        }

        if (shifts.Count == 1)
        {
            var shift = shifts[0];
            return CreateNavigationResult("shift", shift.Id, shift.ClientName, action);
        }

        return new SkillResult
        {
            Success = true,
            Data = new { EntityType = "shift", Results = shifts, Count = shifts.Count },
            Message = $"Found {shifts.Count} shifts matching '{searchQuery}'. Please select one.",
            Type = SkillResultType.Data
        };
    }

    private static SkillResult CreateNavigationResult(
        string entityType,
        Guid? entityId,
        string entityName,
        string action)
    {
        var route = entityType switch
        {
            "client" => entityId.HasValue ? $"/employees/{entityId}" : "/employees",
            "group" => entityId.HasValue ? $"/groups/{entityId}" : "/groups",
            "shift" => entityId.HasValue ? $"/schedule?shiftId={entityId}" : "/schedule",
            _ => "/"
        };

        var navigationData = new
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Action = action,
            Route = route
        };

        return SkillResult.Navigation(
            navigationData,
            entityId.HasValue
                ? $"Navigate to {entityType} '{entityName}' ({action})"
                : $"Navigate to {entityType} list");
    }
}
