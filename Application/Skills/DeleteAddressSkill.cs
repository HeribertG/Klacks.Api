// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client address via DeleteCommand&lt;AddressResource&gt;. Use get_client_details to
/// resolve the address id first; fails with a clear message if it does not exist. The delete is
/// verified by re-reading the address afterwards: only when it is no longer readable is the delete
/// reported as verified.
/// </summary>
/// <param name="addressId">Required. UUID of the address to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_address")]
public class DeleteAddressSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteAddressSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var addressId = GetRequiredGuid(parameters, "addressId");

        var deleted = await _mediator.Send(new DeleteCommand<AddressResource>(addressId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Address {addressId} not found.");
        }

        AddressResource? stillVisible;
        try
        {
            stillVisible = await _mediator.Send(new GetQuery<AddressResource>(addressId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            stillVisible = null;
        }

        if (stillVisible != null)
        {
            return SkillResult.Error(
                $"Address '{addressId}' was reported as deleted but is still readable from the database — " +
                "treat the delete as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, Verified = true },
            "Address deleted and confirmed removed from the database (verified).");
    }
}
