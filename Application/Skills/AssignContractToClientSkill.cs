using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class AssignContractToClientSkill : BaseSkill
{
    private readonly IClientRepository _clientRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "assign_contract_to_client";

    public override string Description =>
        "Assigns an existing contract to a client/employee. This creates a client-contract association with validity dates. " +
        "Only one contract can be active at a time per client. Use this to set up employment contracts.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditClients" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "clientId",
            "The unique ID (GUID) of the client",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "contractId",
            "The unique ID (GUID) of the contract to assign",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "fromDate",
            "Start date of the contract assignment (format: YYYY-MM-DD)",
            SkillParameterType.Date,
            Required: true),
        new SkillParameter(
            "untilDate",
            "End date of the contract assignment (format: YYYY-MM-DD). Leave empty for indefinite.",
            SkillParameterType.Date,
            Required: false),
        new SkillParameter(
            "setAsActive",
            "Whether to set this as the active contract (will deactivate other contracts)",
            SkillParameterType.Boolean,
            Required: false,
            DefaultValue: "true")
    };

    public AssignContractToClientSkill(
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
        var clientIdStr = GetRequiredString(parameters, "clientId");
        var contractIdStr = GetRequiredString(parameters, "contractId");
        var fromDateStr = GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var setAsActive = GetParameter<bool>(parameters, "setAsActive", true);

        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
        }

        if (!Guid.TryParse(contractIdStr, out var contractId))
        {
            return SkillResult.Error($"Invalid contract ID format: {contractIdStr}");
        }

        if (!DateOnly.TryParse(fromDateStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid from date format: {fromDateStr}. Expected format: YYYY-MM-DD");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr))
        {
            if (!DateOnly.TryParse(untilDateStr, out var parsedUntil))
            {
                return SkillResult.Error($"Invalid until date format: {untilDateStr}. Expected format: YYYY-MM-DD");
            }
            untilDate = parsedUntil;
        }

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client with ID {clientId} not found.");
        }

        var contract = await _contractRepository.Get(contractId);
        if (contract == null)
        {
            return SkillResult.Error($"Contract with ID {contractId} not found.");
        }

        var existingAssignment = client.ClientContracts
            .FirstOrDefault(cc => cc.ContractId == contractId && !cc.IsDeleted);

        if (existingAssignment != null)
        {
            return SkillResult.Error($"Contract '{contract.Name}' is already assigned to this client.");
        }

        if (setAsActive)
        {
            foreach (var cc in client.ClientContracts.Where(c => c.IsActive && !c.IsDeleted))
            {
                cc.IsActive = false;
                cc.UpdateTime = DateTime.UtcNow;
                cc.CurrentUserUpdated = context.UserName;
            }
        }

        var clientContract = new ClientContract
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ContractId = contractId,
            FromDate = fromDate,
            UntilDate = untilDate,
            IsActive = setAsActive,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        client.ClientContracts.Add(clientContract);
        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            ClientContractId = clientContract.Id,
            ClientId = clientId,
            ClientName = $"{client.FirstName} {client.Name}",
            ContractId = contractId,
            ContractName = contract.Name,
            FromDate = fromDate,
            UntilDate = untilDate,
            IsActive = setAsActive,
            GuaranteedHours = contract.GuaranteedHours,
            MaximumHours = contract.MaximumHours
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Contract '{contract.Name}' successfully assigned to '{client.FirstName} {client.Name}'.");
    }
}
