// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that lists existing customers (clients of type EntityTypeEnum.Customer) so the assistant can
/// ask the user which customer a new order is billed to, before creating it. Every order must be
/// billed to a customer; call this whenever a new order is requested and no customer is known yet.
/// </summary>
/// <param name="searchString">Optional text filter on company name, first name or last name.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_customer_candidates")]
public class FindCustomerCandidatesSkill : BaseSkillImplementation
{
    private const int MaxResults = 25;

    private readonly IClientRepository _clientRepository;

    public FindCustomerCandidatesSkill(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchString = (GetParameter<string>(parameters, "searchString") ?? string.Empty).Trim();

        var customers = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            EntityTypeEnum.Customer, cancellationToken);

        IEnumerable<Domain.Models.Staffs.Client> filtered = customers;
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            filtered = customers.Where(c =>
                Contains(c.Company, searchString) ||
                Contains(c.FirstName, searchString) ||
                Contains(c.Name, searchString));
        }

        var items = filtered
            .Select(c => new
            {
                CustomerId = c.Id,
                Name = DisplayName(c),
                City = c.Addresses?.FirstOrDefault()?.City ?? string.Empty
            })
            .OrderBy(c => c.Name)
            .Take(MaxResults)
            .ToList();

        string message;
        if (items.Count == 0)
        {
            message = "No customers found. Ask the user to confirm creating a NEW customer, then create it with " +
                      "create_employee using entityType=Customer, and bill the order to that customer.";
        }
        else if (items.Count == 1)
        {
            message = $"Exactly one customer matches: {items[0].Name} (clientId {items[0].CustomerId}). " +
                      "The customer is unambiguous, so do NOT ask the user to choose — bill the order to this customer and proceed.";
        }
        else
        {
            message = $"Found {items.Count} customer(s). ASK THE USER which customer the order is billed to, or whether to " +
                      "create a NEW customer (create_employee with entityType=Customer). Do not create the order before the user decides.";
        }

        return SkillResult.SuccessResult(new { Count = items.Count, Customers = items }, message);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string DisplayName(Domain.Models.Staffs.Client client)
    {
        if (!string.IsNullOrWhiteSpace(client.Company))
        {
            return client.Company;
        }

        var fullName = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "(unnamed customer)" : fullName;
    }
}
