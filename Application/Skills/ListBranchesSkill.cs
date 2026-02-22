// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListBranchesSkill : BaseSkill
{
    private readonly IBranchRepository _branchRepository;

    public override string Name => "list_branches";

    public override string Description =>
        "Lists all branches (Filialen) in the system. " +
        "Returns branch IDs, names, addresses, phone numbers, and emails.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "searchTerm",
            "Optional search term to filter branches by name",
            SkillParameterType.String,
            Required: false)
    ];

    public ListBranchesSkill(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var allBranches = await _branchRepository.List();

        var branches = allBranches
            .Where(b => !b.IsDeleted)
            .Where(b => string.IsNullOrEmpty(searchTerm) ||
                       b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Address,
                b.Phone,
                b.Email
            })
            .ToList();

        var resultData = new
        {
            Branches = branches,
            TotalCount = branches.Count
        };

        var message = $"Found {branches.Count} branch(es)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
