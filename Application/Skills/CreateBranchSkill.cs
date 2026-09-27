// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new branch (a physical office location) and verifies the write by re-reading the
/// row straight from the database; a mismatch rolls the write back.
/// </summary>
/// <param name="name">Required. Unique display name of the branch.</param>
/// <param name="address">Required. Postal address of the branch.</param>
/// <param name="phone">Optional. Phone number of the branch.</param>
/// <param name="email">Optional. Email address of the branch.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CreateBranchSkill : BaseSkillImplementation
{
    private const string SkillName = "create_branch";

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
        var name = GetRequiredString(parameters, "name").Trim();
        var address = GetRequiredString(parameters, "address").Trim();

        if (name.Length == 0)
            return SkillResult.Error("Branch name cannot be empty.");

        if (address.Length == 0)
            return SkillResult.Error("Branch address cannot be empty.");

        var exists = await _branchRepository.ExistsByNameAsync(name);
        if (exists)
            return SkillResult.Error($"A branch with the name '{name}' already exists.");

        var phone = GetParameter<string>(parameters, "phone")?.Trim() ?? string.Empty;
        var email = GetParameter<string>(parameters, "email")?.Trim() ?? string.Empty;

        var branch = new Branch
        {
            Name = name,
            Address = address,
            Phone = phone,
            Email = email
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _branchRepository.Add(branch);
                await _unitOfWork.CompleteAsync();

                var createdId = branch.Id;
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _branchRepository.GetNoTracking(createdId),
                    persisted => !persisted.IsDeleted
                        && persisted.Name == name
                        && persisted.Address == address
                        && persisted.Phone == phone
                        && persisted.Email == email,
                    $"the new branch '{name}'");

                return createdId;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var resultData = new
        {
            BranchId = branch.Id,
            branch.Name,
            branch.Address,
            branch.Phone,
            branch.Email
        };

        return SkillResult.SuccessResult(resultData,
            $"Branch '{name}' was created and confirmed in the database (verified).");
    }
}
