// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_branch")]
public class CreateBranchSkill : BaseSkillImplementation
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchSkill(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        var address = GetRequiredString(parameters, "address");

        if (string.IsNullOrWhiteSpace(name))
            return SkillResult.Error("Branch name cannot be empty.");

        if (string.IsNullOrWhiteSpace(address))
            return SkillResult.Error("Branch address cannot be empty.");

        var exists = await _branchRepository.ExistsByNameAsync(name);
        if (exists)
            return SkillResult.Error($"A branch with the name '{name}' already exists.");

        var phone = GetParameter<string>(parameters, "phone") ?? string.Empty;
        var email = GetParameter<string>(parameters, "email") ?? string.Empty;

        var branch = new Branch
        {
            Name = name,
            Address = address,
            Phone = phone,
            Email = email
        };

        await _branchRepository.Add(branch);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            BranchId = branch.Id,
            branch.Name,
            branch.Address,
            branch.Phone,
            branch.Email
        };

        return SkillResult.SuccessResult(resultData,
            $"Branch '{name}' was successfully created.");
    }
}
