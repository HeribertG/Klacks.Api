// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that creates a new client (employee, external employee or customer) with the mandatory
/// onboarding data: client master record + membership, plus optional address and communication
/// (email/phone). The id_number is left at its CLR default so the database sequence assigns it.
/// </summary>
/// <param name="clientRepository">Persists the client and its cascaded child entities</param>
/// <param name="unitOfWork">Commits the unit of work</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_employee")]
public class CreateEmployeeSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmployeeSkill(
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetRequiredString(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var genderStr = GetRequiredString(parameters, "gender");

        if (!Enum.TryParse<GenderEnum>(genderStr, true, out var gender))
        {
            return SkillResult.Error($"Invalid gender value: {genderStr}");
        }

        var entityType = ParseEntityType(GetParameter<string>(parameters, "entityType"));
        var birthdate = GetParameter<string>(parameters, "birthdate");
        var street = GetParameter<string>(parameters, "street");
        var zip = GetParameter<string>(parameters, "zip");
        var city = GetParameter<string>(parameters, "city");
        var state = GetParameter<string>(parameters, "state");
        var country = GetParameter<string>(parameters, "country", "Schweiz");
        var company = GetParameter<string>(parameters, "company");
        var email = GetParameter<string>(parameters, "email");
        var phone = GetParameter<string>(parameters, "phone");
        var proceedWithoutContact = GetParameter<bool>(parameters, "proceedWithoutContact", false);

        if (!proceedWithoutContact)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(zip) || string.IsNullOrWhiteSpace(city))
            {
                missing.Add("address (street, zip, city)");
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                missing.Add("email");
            }
            if (string.IsNullOrWhiteSpace(phone))
            {
                missing.Add("phone");
            }

            if (missing.Count > 0)
            {
                return SkillResult.Error(
                    "Cannot create the client yet. The following onboarding data is still missing: " +
                    string.Join(", ", missing) +
                    ". Ask the user for each missing item, then call create_employee again with all collected fields. " +
                    "Only if the user explicitly declines to provide them, call create_employee again with proceedWithoutContact set to true.");
            }
        }

        var now = DateTime.UtcNow;

        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            Name = lastName,
            Gender = gender,
            Company = company,
            Type = entityType,
            LegalEntity = gender == GenderEnum.LegalEntity,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        };

        if (!string.IsNullOrEmpty(birthdate) && DateTime.TryParse(birthdate, out var birthdateValue))
        {
            client.Birthdate = birthdateValue;
        }

        client.Membership = new Membership
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Type = 0,
            ValidFrom = now.Date,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        };

        if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(zip))
        {
            client.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = street ?? string.Empty,
                Zip = zip ?? string.Empty,
                City = city ?? string.Empty,
                State = state ?? string.Empty,
                Country = country ?? "Schweiz",
                Type = AddressTypeEnum.Employee,
                ValidFrom = now,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
        }

        if (!string.IsNullOrEmpty(email))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateMail,
                Value = email,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
        }

        if (!string.IsNullOrEmpty(phone))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateCellPhone,
                Value = phone,
                CreateTime = now,
                CurrentUserCreated = context.UserName
            });
        }

        await _clientRepository.Add(client);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            EmployeeId = client.Id,
            FirstName = firstName,
            LastName = lastName,
            Gender = gender.ToString(),
            EntityType = entityType.ToString(),
            HasMembership = true,
            HasEmail = !string.IsNullOrEmpty(email),
            HasPhone = !string.IsNullOrEmpty(phone),
            State = state,
            City = city,
            Country = country
        };

        var message = $"{entityType} {firstName} {lastName}" +
                      (!string.IsNullOrEmpty(state) ? $" from {state}" : string.Empty) +
                      " was successfully created (with membership" +
                      (!string.IsNullOrEmpty(email) ? ", email" : string.Empty) +
                      (!string.IsNullOrEmpty(phone) ? ", phone" : string.Empty) +
                      ").";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static EntityTypeEnum ParseEntityType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EntityTypeEnum.Employee;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "customer" or "kunde" or "kunden" or "client" or "2" => EntityTypeEnum.Customer,
            "externemp" or "extern" or "external" or "externer mitarbeiter" or "externer" or "1" => EntityTypeEnum.ExternEmp,
            _ => EntityTypeEnum.Employee
        };
    }
}
