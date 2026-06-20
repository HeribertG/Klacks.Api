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
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_employee")]
public class CreateEmployeeSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICountryResolver _countryResolver;

    public CreateEmployeeSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IUnitOfWork unitOfWork,
        ICountryResolver countryResolver)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _unitOfWork = unitOfWork;
        _countryResolver = countryResolver;
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

        var entityTypeStr = GetParameter<string>(parameters, "entityType");
        var entityType = EntityTypeEnum.Employee;
        if (!string.IsNullOrWhiteSpace(entityTypeStr)
            && !Enum.TryParse(entityTypeStr, true, out entityType))
        {
            return SkillResult.Error(
                $"Invalid entityType value: {entityTypeStr}. Valid values: Employee, ExternEmp, Customer.");
        }
        var birthdate = GetParameter<string>(parameters, "birthdate");
        var street = GetParameter<string>(parameters, "street");
        var zip = GetParameter<string>(parameters, "zip");
        var city = GetParameter<string>(parameters, "city");
        var state = GetParameter<string>(parameters, "state");
        var countryInput = GetParameter<string>(parameters, "country");
        var company = GetParameter<string>(parameters, "company");
        var email = GetParameter<string>(parameters, "email");
        var phone = GetParameter<string>(parameters, "phone");
        var memberSince = GetParameter<string>(parameters, "memberSince");
        var proceedWithoutContact = GetParameter<bool>(parameters, "proceedWithoutContact", false);

        if (string.IsNullOrWhiteSpace(memberSince))
        {
            return SkillResult.Error(
                "Cannot create the client yet. Please ask the user when the employee starts working " +
                "(memberSince / Eintrittsdatum / start date), then call create_employee again with that value in YYYY-MM-DD format. " +
                "When you ask for this date, append a date-picker block so the user can pick it instead of typing: [REPLIES:date \"Ab wann?\"]. " +
                "Resolve casual phrases the user already gave you: \"heute\"/\"sofort\"/\"jetzt\"/\"direkt\"/\"today\"/\"now\" → today's date; " +
                "\"morgen\"/\"tomorrow\" → tomorrow's date; \"1. Juli\"/\"July 1st\" → the matching YYYY-MM-DD.");
        }

        if (!DateTime.TryParse(memberSince, out var memberSinceDate))
        {
            return SkillResult.Error(
                $"Invalid memberSince value: {memberSince}. Expected format YYYY-MM-DD (e.g. 2026-06-01).");
        }

        // An address must ALWAYS be complete — a partial address (e.g. a missing zip / postal code) is
        // invalid even when contact data is skipped. proceedWithoutContact only waives email/phone.
        var hasAnyAddress = !string.IsNullOrWhiteSpace(street)
                            || !string.IsNullOrWhiteSpace(zip)
                            || !string.IsNullOrWhiteSpace(city);
        if (hasAnyAddress
            && (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(zip) || string.IsNullOrWhiteSpace(city)))
        {
            return SkillResult.Error(
                "Cannot create the client yet. The address is incomplete: street, zip (postal code) and city are all " +
                "required when an address is given. The zip/postal code is missing — ask the user for it (e.g. 2500 for Biel) " +
                "or supply the one you already know, then call create_employee again with the complete address.");
        }

        if (!proceedWithoutContact)
        {
            var missing = new List<string>();
            if (!hasAnyAddress)
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

        var resolvedCountry = await _countryResolver.ResolveAsync(countryInput, cancellationToken)
            ?? await _countryResolver.GetDefaultAsync(cancellationToken);

        var countryCode = resolvedCountry?.Abbreviation ?? string.Empty;

        if (string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(zip))
        {
            state = await _searchRepository.FindStatePostCode(zip!);
        }

        var (phonePrefix, phoneNumber) = SplitPhone(phone, resolvedCountry);

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
            ValidFrom = memberSinceDate.Date,
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
                Country = countryCode,
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

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            client.Communications.Add(new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = CommunicationTypeEnum.PrivateCellPhone,
                Prefix = phonePrefix,
                Value = phoneNumber,
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
            Country = countryCode
        };

        var message = $"{entityType} {firstName} {lastName}" +
                      (!string.IsNullOrEmpty(state) ? $" from {state}" : string.Empty) +
                      " was successfully created (with membership" +
                      (!string.IsNullOrEmpty(email) ? ", email" : string.Empty) +
                      (!string.IsNullOrEmpty(phone) ? ", phone" : string.Empty) +
                      ").";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static (string Prefix, string Number) SplitPhone(string? phone, Countries? country)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return (string.Empty, string.Empty);
        }

        var cleaned = new string(phone.Where(ch => char.IsDigit(ch) || ch == '+').ToArray());
        if (cleaned.StartsWith("00", StringComparison.Ordinal))
        {
            cleaned = "+" + cleaned[2..];
        }

        var prefix = country?.Prefix ?? string.Empty;

        if (!string.IsNullOrEmpty(prefix) && cleaned.StartsWith(prefix, StringComparison.Ordinal))
        {
            return (prefix, cleaned[prefix.Length..].TrimStart('0'));
        }

        if (cleaned.StartsWith('+'))
        {
            return (string.Empty, cleaned);
        }

        if (!string.IsNullOrEmpty(prefix))
        {
            return (prefix, cleaned.TrimStart('0'));
        }

        return (string.Empty, cleaned);
    }

}
