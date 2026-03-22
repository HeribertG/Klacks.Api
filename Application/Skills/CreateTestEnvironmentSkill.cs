// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Composite skill for creating a complete test environment for autofill testing.
/// Creates employees with addresses, contracts, group assignments and shifts in a single call.
/// </summary>
/// <param name="city">City for the employee addresses (e.g. "Bern")</param>
/// <param name="numberOfEmployees">Number of employees to create</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_test_environment")]
public class CreateTestEnvironmentSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTestEnvironmentSkill(
        IClientRepository clientRepository,
        IAddressRepository addressRepository,
        IContractRepository contractRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _addressRepository = addressRepository;
        _contractRepository = contractRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var city = GetParameter<string>(parameters, "city") ?? "Bern";
        var numberOfEmployees = GetParameter<int>(parameters, "numberOfEmployees", 5);
        var groupName = GetParameter<string>(parameters, "groupName") ?? city;
        var contractType = GetParameter<string>(parameters, "contractType") ?? "Vollzeit 180";
        var canton = GetParameter<string>(parameters, "canton") ?? "BE";
        var guaranteedHours = GetParameter<decimal>(parameters, "guaranteedHours", 172.2m);
        var shiftStartDate = GetParameter<string>(parameters, "shiftStartDate") ?? DateTime.Today.ToString("yyyy-MM-dd");
        var createShifts = GetParameter<bool>(parameters, "createShifts", true);
        var sumEmployees = GetParameter<int>(parameters, "sumEmployees", 1);

        if (!DateOnly.TryParse(shiftStartDate, out var fromDate))
        {
            fromDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var group = await FindGroupByName(groupName);
        if (group == null)
        {
            return SkillResult.Error($"Group '{groupName}' not found. Use list_groups to find available groups.");
        }

        var testData = GetTestData(city, canton);
        var existingClients = await _clientRepository.List();
        var existingNames = existingClients
            .Where(c => !c.IsDeleted)
            .Select(c => $"{c.FirstName}|{c.Name}")
            .ToHashSet();

        testData = testData.Where(t => !existingNames.Contains($"{t.FirstName}|{t.LastName}")).ToList();

        if (testData.Count == 0)
        {
            return SkillResult.Error("Test employees already exist. Delete them first before creating new ones.");
        }

        if (numberOfEmployees > testData.Count)
        {
            numberOfEmployees = testData.Count;
        }

        var (weeklyHours, contractName) = ParseContractType(contractType, canton);
        var monthlyGuaranteed = guaranteedHours > 0 ? guaranteedHours : weeklyHours * 4.33m;

        var contract = await CreateContractAsync(contractName, monthlyGuaranteed, weeklyHours, fromDate, context.UserName);
        var createdEmployees = await CreateEmployeesAsync(testData, numberOfEmployees, contract, group, fromDate, canton, context.UserName);

        var createdShifts = new List<object>();
        if (createShifts)
        {
            createdShifts = await CreateShiftsAsync(city, group, fromDate, sumEmployees, context.UserName);
        }

        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            Employees = createdEmployees,
            EmployeeCount = createdEmployees.Count,
            Contract = new { contract.Id, contract.Name, GuaranteedHours = monthlyGuaranteed },
            Group = new { group.Id, group.Name },
            Shifts = createdShifts,
            ShiftCount = createdShifts.Count
        };

        var message = $"Test environment created: {createdEmployees.Count} employees in group '{group.Name}' " +
                      $"with contract '{contract.Name}' ({monthlyGuaranteed}h/month)";

        if (createdShifts.Count > 0)
        {
            message += $" and {createdShifts.Count} shifts (24/7 from {fromDate:yyyy-MM-dd})";
        }

        return SkillResult.SuccessResult(resultData, message + ".");
    }

    private async Task<Contract> CreateContractAsync(string contractName, decimal monthlyGuaranteed, decimal weeklyHours, DateOnly fromDate, string userName)
    {
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            Name = contractName,
            GuaranteedHours = monthlyGuaranteed,
            MinimumHours = 0,
            MaximumHours = monthlyGuaranteed * 1.2m,
            FullTime = weeklyHours,
            ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue),
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = userName
        };

        await _contractRepository.Add(contract);

        return contract;
    }

    private async Task<List<object>> CreateEmployeesAsync(List<TestEmployee> testData, int count, Contract contract, Group group, DateOnly fromDate, string canton, string userName)
    {
        var createdEmployees = new List<object>();

        for (var i = 0; i < count; i++)
        {
            var data = testData[i];

            var clientContract = new ClientContract
            {
                Id = Guid.NewGuid(),
                ContractId = contract.Id,
                FromDate = fromDate,
                IsActive = true,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = userName
            };

            var client = new Client
            {
                Id = Guid.NewGuid(),
                FirstName = data.FirstName,
                Name = data.LastName,
                Gender = data.Gender,
                Birthdate = data.Birthdate,
                ClientContracts = [clientContract],
                Membership = new Membership
                {
                    Id = Guid.NewGuid(),
                    Type = 0,
                    ValidFrom = DateTime.UtcNow,
                    CreateTime = DateTime.UtcNow,
                    CurrentUserCreated = userName
                },
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = userName
            };

            clientContract.ClientId = client.Id;
            await _clientRepository.Add(client);

            var address = new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Street = data.Street,
                Zip = data.PostalCode,
                City = data.City,
                State = canton,
                Country = "CH",
                Type = AddressTypeEnum.Employee,
                ValidFrom = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = userName
            };

            await _addressRepository.Add(address);

            var groupItem = new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                GroupId = group.Id,
                ValidFrom = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = userName
            };

            await _groupItemRepository.Add(groupItem);

            createdEmployees.Add(new
            {
                Id = client.Id,
                Name = $"{data.FirstName} {data.LastName}",
                Address = $"{data.Street}, {data.PostalCode} {data.City}"
            });
        }

        return createdEmployees;
    }

    private async Task<List<object>> CreateShiftsAsync(string city, Group group, DateOnly fromDate, int sumEmployees, string userName)
    {
        var createdShifts = new List<object>();

        var shiftDefinitions = new[]
        {
            new { Name = $"Fruhdienst {city}", Abbr = "FD", Start = new TimeOnly(7, 0), End = new TimeOnly(15, 0) },
            new { Name = $"Spaetdienst {city}", Abbr = "SD", Start = new TimeOnly(15, 0), End = new TimeOnly(23, 0) },
            new { Name = $"Nachtdienst {city}", Abbr = "ND", Start = new TimeOnly(23, 0), End = new TimeOnly(7, 0) }
        };

        foreach (var def in shiftDefinitions)
        {
            var workTime = CalculateWorkTime(def.Start, def.End);

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                Name = def.Name,
                Abbreviation = def.Abbr,
                Status = ShiftStatus.SealedOrder,
                ShiftType = ShiftType.IsTask,
                StartShift = def.Start,
                EndShift = def.End,
                FromDate = fromDate,
                WorkTime = workTime,
                SumEmployees = sumEmployees,
                Quantity = 1,
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                IsSaturday = true,
                IsSunday = true,
                GroupItems =
                [
                    new GroupItem
                    {
                        GroupId = group.Id,
                        ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    }
                ],
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = userName
            };

            var resultShift = await _shiftRepository.AddWithSealedOrderHandling(shift);

            createdShifts.Add(new
            {
                Id = resultShift.Id,
                def.Name,
                Start = def.Start.ToString("HH:mm"),
                End = def.End.ToString("HH:mm"),
                WorkTime = workTime
            });
        }

        return createdShifts;
    }

    private async Task<Group?> FindGroupByName(string name)
    {
        var allGroups = await _groupRepository.List();
        return allGroups
            .Where(g => !g.IsDeleted)
            .FirstOrDefault(g => g.Name != null &&
                                 g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static (decimal weeklyHours, string name) ParseContractType(string contractType, string canton)
    {
        return contractType.ToLower() switch
        {
            "vollzeit 160" => (40m, $"{canton} Vollzeit 160 Std"),
            "vollzeit 180" => (45m, $"{canton} Vollzeit 180 Std"),
            "teilzeit 0 std" => (0m, $"{canton} Teilzeit 0 Std"),
            "teilzeit 80 std" => (20m, $"{canton} Teilzeit 80 Std"),
            "minijob" => (10m, $"{canton} Minijob"),
            _ => (40m, $"{canton} {contractType}")
        };
    }

    private static List<TestEmployee> GetTestData(string city, string canton)
    {
        var bernAddresses = new List<TestEmployee>
        {
            new("Anna", "Meier", GenderEnum.Female, "Kramgasse 12", "3011", "Bern", new DateTime(1988, 4, 15)),
            new("Lukas", "Brunner", GenderEnum.Male, "Marktgasse 45", "3011", "Bern", new DateTime(1992, 8, 22)),
            new("Sarah", "Gerber", GenderEnum.Female, "Bundesplatz 3", "3003", "Bern", new DateTime(1985, 11, 3)),
            new("Marco", "Bianchi", GenderEnum.Male, "Spitalgasse 28", "3011", "Bern", new DateTime(1990, 6, 17)),
            new("Nina", "Mueller", GenderEnum.Female, "Gerechtigkeitsgasse 7", "3011", "Bern", new DateTime(1995, 1, 29)),
            new("David", "Schneider", GenderEnum.Male, "Laenggassstrasse 51", "3012", "Bern", new DateTime(1987, 3, 8)),
            new("Laura", "Weber", GenderEnum.Female, "Effingerstrasse 16", "3008", "Bern", new DateTime(1993, 9, 12)),
            new("Thomas", "Fischer", GenderEnum.Male, "Monbijoustrasse 22", "3011", "Bern", new DateTime(1982, 7, 25)),
            new("Elena", "Huber", GenderEnum.Female, "Bollwerk 15", "3011", "Bern", new DateTime(1991, 12, 5)),
            new("Patrick", "Keller", GenderEnum.Male, "Aarbergergasse 30", "3011", "Bern", new DateTime(1989, 2, 18))
        };

        if (city.Equals("Bern", StringComparison.OrdinalIgnoreCase))
        {
            return bernAddresses;
        }

        return bernAddresses.Select(e => e with { City = city }).ToList();
    }

    private record TestEmployee(
        string FirstName,
        string LastName,
        GenderEnum Gender,
        string Street,
        string PostalCode,
        string City,
        DateTime Birthdate);
}
