// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client address via DeleteCommand&lt;AddressResource&gt;. Use get_client_details to
/// resolve the address id first; fails with a clear message if it does not exist.
/// </summary>
/// <param name="addressId">Required. UUID of the address to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
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

        return SkillResult.SuccessResult(
            new { deleted.Id },
            "Address deleted.");
    }
}
