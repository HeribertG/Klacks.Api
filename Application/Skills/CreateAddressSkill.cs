// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new address for an existing client via PostCommand&lt;AddressResource&gt;. The client is
/// referenced by its UUID; use search_employees / get_client_details to resolve it first.
/// </summary>
/// <param name="clientId">Required. UUID of the client the address belongs to.</param>
/// <param name="street">Optional. Street and house number.</param>
/// <param name="zip">Optional. Postal/ZIP code.</param>
/// <param name="city">Optional. City/town.</param>
/// <param name="country">Optional. Country.</param>
/// <param name="state">Optional. State/canton/region.</param>
/// <param name="type">Optional. Numeric address type code (0 = employee, 1 = workplace, 2 = invoicing). Defaults to 0.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_address")]
public class CreateAddressSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateAddressSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");

        var address = new AddressResource
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Street = GetParameter<string>(parameters, "street") ?? string.Empty,
            Zip = GetParameter<string>(parameters, "zip") ?? string.Empty,
            City = GetParameter<string>(parameters, "city") ?? string.Empty,
            Country = GetParameter<string>(parameters, "country") ?? string.Empty,
            State = GetParameter<string>(parameters, "state") ?? string.Empty,
            Type = (AddressTypeEnum)GetParameter<int>(parameters, "type", (int)AddressTypeEnum.Employee)
        };

        var created = await _mediator.Send(new PostCommand<AddressResource>(address), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Creating address for client '{clientId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                created.ClientId,
                created.Street,
                created.Zip,
                created.City,
                created.Country,
                created.Type
            },
            "Address created.");
    }
}
