// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_employee")]
public class CreateEmployeeSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmployeeSkill(
        IClientRepository clientRepository,
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _addressRepository = addressRepository;
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

        var birthdate = GetParameter<string>(parameters, "birthdate");
        var street = GetParameter<string>(parameters, "street");
        var zip = GetParameter<string>(parameters, "zip");
        var city = GetParameter<string>(parameters, "city");
        var state = GetParameter<string>(parameters, "state");
        var country = GetParameter<string>(parameters, "country", "Schweiz");
        var company = GetParameter<string>(parameters, "company");


        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            Name = lastName,
            Gender = gender,
            Company = company,
            LegalEntity = gender == GenderEnum.LegalEntity,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        if (!string.IsNullOrEmpty(birthdate) && DateTime.TryParse(birthdate, out var birthdateValue))
        {
            client.Birthdate = birthdateValue;
        }

        await _clientRepository.Add(client);

        if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city))
        {
            var address = new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = street ?? "",
                Zip = zip ?? "",
                City = city ?? "",
                State = state ?? "",
                Country = country ?? "Schweiz",
                Type = AddressTypeEnum.Employee,
                ValidFrom = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = context.UserName
            };

            await _addressRepository.Add(address);
        }

        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            EmployeeId = client.Id,
            FirstName = firstName,
            LastName = lastName,
            Gender = gender.ToString(),
            State = state,
            City = city,
            Country = country
        };

        var message = $"Employee {firstName} {lastName}" +
                      (!string.IsNullOrEmpty(state) ? $" from {state}" : "") +
                      " was successfully created.";

        return SkillResult.SuccessResult(resultData, message);
    }

}
