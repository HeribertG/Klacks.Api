// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new address for an existing client via PostCommand&lt;AddressResource&gt;. The client is
/// referenced by its UUID; use search_employees / get_client_details to resolve it first. The write
/// is verified by re-reading the address via GetQuery and comparing the persisted fields; a failed
/// verification is reported as an error instead of a fabricated success.
/// </summary>
/// <param name="clientId">Required. UUID of the client the address belongs to.</param>
/// <param name="street">Optional. Street and house number.</param>
/// <param name="zip">Optional. Postal/ZIP code.</param>
/// <param name="city">Optional. City/town.</param>
/// <param name="country">Optional. Country.</param>
/// <param name="state">Optional. State/canton/region.</param>
/// <param name="type">Optional. Numeric address type code (0 = employee, 1 = workplace, 2 = invoicing). Defaults to 0.</param>
/// <param name="validFrom">Optional. Date (yyyy-MM-dd) from which the address is valid — used for relocations, where a future date schedules the move.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
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

        var validFromRaw = GetParameter<string>(parameters, "validFrom");
        if (!string.IsNullOrWhiteSpace(validFromRaw))
        {
            if (!DateTime.TryParse(validFromRaw, out var validFrom))
            {
                return SkillResult.Error($"Invalid validFrom value: {validFromRaw}. Expected format yyyy-MM-dd.");
            }

            address.ValidFrom = validFrom;
        }

        var created = await _mediator.Send(new PostCommand<AddressResource>(address), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Creating address for client '{clientId}' failed.");
        }

        AddressResource? persisted;
        try
        {
            persisted = await _mediator.Send(new GetQuery<AddressResource>(created.Id), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            persisted = null;
        }

        if (persisted == null)
        {
            return SkillResult.Error(
                $"Address '{created.Id}' was reported as created but could not be re-read from the database — " +
                "treat the creation as not persisted and do not use this address id.");
        }

        var mismatches = CollectFieldMismatches(address, persisted);
        if (mismatches.Count > 0)
        {
            return SkillResult.Error(
                $"Address '{created.Id}' was created but database verification found mismatching fields: " +
                $"{string.Join(", ", mismatches)}. Re-read the address before relying on the stored values.");
        }

        return SkillResult.SuccessResult(
            new
            {
                persisted.Id,
                persisted.ClientId,
                persisted.Street,
                persisted.Zip,
                persisted.City,
                persisted.Country,
                persisted.Type,
                persisted.ValidFrom,
                Verified = true
            },
            "Address created and confirmed in the database (verified).");
    }

    private static List<string> CollectFieldMismatches(AddressResource expected, AddressResource persisted)
    {
        var mismatches = new List<string>();

        if (persisted.ClientId != expected.ClientId)
        {
            mismatches.Add("clientId");
        }

        if (persisted.Street != expected.Street)
        {
            mismatches.Add("street");
        }

        if (persisted.Zip != expected.Zip)
        {
            mismatches.Add("zip");
        }

        if (persisted.City != expected.City)
        {
            mismatches.Add("city");
        }

        if (persisted.Country != expected.Country)
        {
            mismatches.Add("country");
        }

        if (persisted.State != expected.State)
        {
            mismatches.Add("state");
        }

        if (persisted.Type != expected.Type)
        {
            mismatches.Add("type");
        }

        if (expected.ValidFrom.HasValue && persisted.ValidFrom?.Date != expected.ValidFrom.Value.Date)
        {
            mismatches.Add("validFrom");
        }

        return mismatches;
    }
}
