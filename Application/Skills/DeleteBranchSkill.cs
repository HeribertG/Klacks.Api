// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_branch")]
public class DeleteBranchSkill : BaseSkillImplementation
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBranchSkill(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var branchIdStr = GetRequiredString(parameters, "branchId");
        var branchId = Guid.Parse(branchIdStr);

        var branch = await _branchRepository.Get(branchId);
        if (branch == null)
            return SkillResult.Error($"Branch with ID '{branchIdStr}' not found.");

        var branchName = branch.Name;
        await _branchRepository.Delete(branchId);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            BranchId = branchIdStr,
            DeletedBranchName = branchName
        };

        return SkillResult.SuccessResult(resultData,
            $"Branch '{branchName}' was successfully deleted.");
    }
}
