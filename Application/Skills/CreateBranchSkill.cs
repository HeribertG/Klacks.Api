// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Skills;

public class CreateBranchSkill : BaseSkill
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "create_branch";

    public override string Description =>
        "Creates a new branch (Filiale) in the system. " +
        "A branch represents a physical office location with name, address, phone, and email.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "name",
            "Name of the branch (e.g. 'Filiale Zürich')",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "address",
            "Address of the branch",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "phone",
            "Phone number of the branch",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "email",
            "Email address of the branch",
            SkillParameterType.String,
            Required: false)
    ];

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
