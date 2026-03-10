// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_contract")]
public class CreateContractSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContractSkill(
        IClientRepository clientRepository,
        IContractRepository contractRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _contractRepository = contractRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var employeeIdStr = GetRequiredString(parameters, "employeeId");
        var contractType = GetRequiredString(parameters, "contractType");
        var canton = GetRequiredString(parameters, "canton");
        var startDateStr = GetParameter<string>(parameters, "startDate");
        var endDateStr = GetParameter<string>(parameters, "endDate");
        var weeklyHours = GetParameter<decimal?>(parameters, "weeklyHours");
        var guaranteedHours = GetParameter<decimal?>(parameters, "guaranteedHours");

        if (!Guid.TryParse(employeeIdStr, out var employeeId))
        {
            return SkillResult.Error($"Invalid employee ID format: {employeeIdStr}");
        }

        var employee = await _clientRepository.Get(employeeId);
        if (employee == null)
        {
            return SkillResult.Error($"Employee with ID {employeeId} not found.");
        }

        var startDate = DateTime.Today;
        if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, out var parsedStart))
        {
            startDate = parsedStart;
        }

        DateTime? endDate = null;
        if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, out var parsedEnd))
        {
            endDate = parsedEnd;
        }

        var (hours, contractName) = ParseContractType(contractType, canton);
        guaranteedHours ??= hours * 4.33m;

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            Name = contractName,
            GuaranteedHours = guaranteedHours,
            MinimumHours = 0,
            MaximumHours = hours * 4.33m * 1.2m,
            FullTime = weeklyHours ?? hours,
            ValidFrom = startDate,
            ValidUntil = endDate,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _contractRepository.Add(contract);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            ContractId = contract.Id,
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.Name}",
            ContractType = contractType,
            ContractName = contractName,
            Canton = canton,
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate?.ToString("yyyy-MM-dd"),
            GuaranteedHours = guaranteedHours
        };

        var message = $"Contract '{contractName}' for {employee.FirstName} {employee.Name} in {canton} was created successfully.";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static (decimal hours, string name) ParseContractType(string contractType, string canton)
    {
        var baseName = contractType.ToLower() switch
        {
            "vollzeit 160" => (40m, $"{canton} Vollzeit 160 Std"),
            "vollzeit 180" => (45m, $"{canton} Vollzeit 180 Std"),
            "teilzeit 0 std" => (0m, $"{canton} Teilzeit 0 Std"),
            "teilzeit 80 std" => (20m, $"{canton} Teilzeit 80 Std"),
            "minijob" => (10m, $"{canton} Minijob"),
            _ => (40m, $"{canton} {contractType}")
        };

        return baseName;
    }
}
