// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing client address. Only fields supplied as parameters are changed; the address
/// is loaded via GetQuery&lt;AddressResource&gt; and saved via PutCommand&lt;AddressResource&gt;.
/// </summary>
/// <param name="addressId">Required. UUID of the address to update.</param>
/// <param name="street">Optional. New street and house number.</param>
/// <param name="zip">Optional. New postal/ZIP code.</param>
/// <param name="city">Optional. New city/town.</param>
/// <param name="country">Optional. New country.</param>
/// <param name="state">Optional. New state/canton/region.</param>
/// <param name="type">Optional. New numeric address type code (0 = employee, 1 = workplace, 2 = invoicing).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_address")]
public class UpdateAddressSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateAddressSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var addressId = GetRequiredGuid(parameters, "addressId");

        AddressResource address;
        try
        {
            address = await _mediator.Send(new GetQuery<AddressResource>(addressId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Address '{addressId}' not found.");
        }

        var changed = new List<string>();

        var stringFields = new (string Key, Func<string> Get, Action<string> Set)[]
        {
            ("street", () => address.Street, v => address.Street = v),
            ("zip", () => address.Zip, v => address.Zip = v),
            ("city", () => address.City, v => address.City = v),
            ("country", () => address.Country, v => address.Country = v),
            ("state", () => address.State, v => address.State = v),
        };

        foreach (var field in stringFields)
        {
            var value = GetParameter<string>(parameters, field.Key);
            if (value == null || value == field.Get())
            {
                continue;
            }

            field.Set(value);
            changed.Add(field.Key);
        }

        var type = GetParameter<int?>(parameters, "type");
        if (type.HasValue && (int)address.Type != type.Value)
        {
            address.Type = (AddressTypeEnum)type.Value;
            changed.Add("type");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { AddressId = addressId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — address left unchanged.");
        }

        var updated = await _mediator.Send(new PutCommand<AddressResource>(address), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating address '{addressId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                AddressId = addressId,
                ChangedFields = changed,
                updated.ClientId,
                updated.Street,
                updated.Zip,
                updated.City,
                updated.Country,
                updated.Type
            },
            $"Address updated ({string.Join(", ", changed)}).");
    }
}
