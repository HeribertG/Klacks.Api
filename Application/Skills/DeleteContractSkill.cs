// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a contract template after verifying that it exists. Existing client-contract
/// assignments are not touched by this skill; use list_contracts / get_contract_details to
/// inspect the contract before removal.
/// </summary>
/// <param name="contractId">Required. UUID of the contract to delete; a name is deliberately not accepted for this destructive skill.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_contract")]
public class DeleteContractSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteContractSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (parsedId, idError) = ContractReferenceResolver.ParseStrictId(
            GetParameter<string>(parameters, ContractFieldNames.ContractId), ContractFieldNames.ContractId);
        if (parsedId == null)
        {
            return SkillResult.Error(idError!);
        }

        var contractId = parsedId.Value;

        ContractResource contract;
        try
        {
            contract = await _mediator.Send(new GetQuery<ContractResource>(contractId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Contract '{contractId}' not found.");
        }

        var deleted = await _mediator.Send(new DeleteCommand<ContractResource>(contractId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Deleting contract '{contractId}' failed.");
        }

        return SkillResult.SuccessResult(
            new { ContractId = contractId, DeletedName = contract.Name },
            $"Contract '{contract.Name}' was soft-deleted.");
    }
}
