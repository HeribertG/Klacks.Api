using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class DeleteBranchSkill : BaseSkill
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "delete_branch";

    public override string Description =>
        "Deletes a branch (Filiale) by its ID. " +
        "Requires admin permissions.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "branchId",
            "The ID of the branch to delete",
            SkillParameterType.String,
            Required: true)
    ];

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
