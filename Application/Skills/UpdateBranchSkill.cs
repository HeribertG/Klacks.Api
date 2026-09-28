// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates an existing branch server-side. The branch is identified by its id or resolved by its
/// current name (exact match first, then a single partial match); an ambiguous or unknown name is
/// answered with the real branch names instead of guessing. Only provided values change, and the
/// write is verified by re-reading the row; a mismatch rolls it back.
/// </summary>
/// <param name="branchId">Optional. Id of the branch to update; takes precedence over branchName.</param>
/// <param name="branchName">Optional. Current name of the branch to update when no id is known.</param>
/// <param name="name">Optional. New branch name (must stay unique).</param>
/// <param name="address">Optional. New postal address.</param>
/// <param name="phone">Optional. New phone number.</param>
/// <param name="email">Optional. New email address.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateBranchSkill : BaseSkillImplementation
{
    private const string SkillName = "update_branch";
    private const string BranchIdParameter = "branchId";
    private const string BranchNameParameter = "branchName";
    private const string NameParameter = "name";
    private const string AddressParameter = "address";
    private const string PhoneParameter = "phone";
    private const string EmailParameter = "email";

    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBranchSkill(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var branches = await _branchRepository.List();
        var (target, resolveError) = Resolve(parameters, branches);
        if (target == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var newName = TrimmedOrNull(parameters, NameParameter);
        var newAddress = TrimmedOrNull(parameters, AddressParameter);
        var newPhone = TrimmedOrNull(parameters, PhoneParameter);
        var newEmail = TrimmedOrNull(parameters, EmailParameter);

        if (newName == null && newAddress == null && newPhone == null && newEmail == null)
        {
            return SkillResult.Error(
                $"Nothing to change for branch '{target.Name}'. Provide at least one of name, address, phone or email.");
        }

        if (newName != null && newName.Length == 0)
            return SkillResult.Error("Branch name cannot be empty.");

        if (newAddress != null && newAddress.Length == 0)
            return SkillResult.Error("Branch address cannot be empty.");

        if (newName != null && await _branchRepository.ExistsByNameAsync(newName, target.Id))
            return SkillResult.Error($"A branch with the name '{newName}' already exists.");

        var branch = await _branchRepository.Get(target.Id);
        if (branch == null)
            return SkillResult.Error($"Branch '{target.Name}' was not found.");

        var previousName = branch.Name;
        branch.Name = newName ?? branch.Name;
        branch.Address = newAddress ?? branch.Address;
        branch.Phone = newPhone ?? branch.Phone;
        branch.Email = newEmail ?? branch.Email;

        var expectedName = branch.Name;
        var expectedAddress = branch.Address;
        var expectedPhone = branch.Phone;
        var expectedEmail = branch.Email;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _branchRepository.Put(branch);
                await _unitOfWork.CompleteAsync();

                await ConfirmPersistedAsync(
                    SkillName,
                    () => _branchRepository.GetNoTracking(branch.Id),
                    persisted => !persisted.IsDeleted
                        && persisted.Name == expectedName
                        && persisted.Address == expectedAddress
                        && persisted.Phone == expectedPhone
                        && persisted.Email == expectedEmail,
                    $"the changes to branch '{previousName}'");

                return branch.Id;
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
            $"Branch '{previousName}' was updated and confirmed in the database (verified).");
    }

    private (Branch? Branch, string? Error) Resolve(Dictionary<string, object> parameters, List<Branch> branches)
    {
        var active = branches.Where(b => !b.IsDeleted).ToList();
        var idText = TrimmedOrNull(parameters, BranchIdParameter);
        if (!string.IsNullOrEmpty(idText))
        {
            if (!Guid.TryParse(idText, out var id))
                return (null, $"'{idText}' is not a valid branch id.");

            var byId = active.FirstOrDefault(b => b.Id == id);
            return byId != null
                ? (byId, null)
                : (null, $"Branch with ID '{idText}' was not found. {DescribeAvailable(active)}");
        }

        var nameText = TrimmedOrNull(parameters, BranchNameParameter);
        if (string.IsNullOrEmpty(nameText))
            return (null, $"Provide branchId or branchName to identify the branch. {DescribeAvailable(active)}");

        var exact = active.Where(b => string.Equals(b.Name, nameText, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1)
            return (exact[0], null);

        var partial = active.Where(b => b.Name.Contains(nameText, StringComparison.OrdinalIgnoreCase)).ToList();
        if (partial.Count == 1)
            return (partial[0], null);

        if (partial.Count > 1)
        {
            var candidates = string.Join(", ", partial.Select(b => $"'{b.Name}'"));
            return (null, $"'{nameText}' matches several branches: {candidates}. Ask which one is meant.");
        }

        return (null, $"No branch matches '{nameText}'. {DescribeAvailable(active)}");
    }

    private static string DescribeAvailable(List<Branch> branches) =>
        branches.Count == 0
            ? "There are no branches yet."
            : $"Available branches: {string.Join(", ", branches.Select(b => $"'{b.Name}'"))}.";

    private string? TrimmedOrNull(Dictionary<string, object> parameters, string key) =>
        GetParameter<string>(parameters, key)?.Trim();
}
