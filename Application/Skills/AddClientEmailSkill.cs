// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: adds an email address to an existing client identified by name.
/// Kept tiny so the LLM reliably emits the call (large multi-field update_client is not).
/// The write is self-verifying: it runs in a transaction and the new entry must read back fresh
/// from the database before success is reported; otherwise the change is rolled back.
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="email">Email address to add.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddClientEmailSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_email";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientEmailSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var email = GetRequiredString(parameters, "email");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var now = DateTime.UtcNow;
        var communicationId = Guid.NewGuid();
        client!.Communications.Add(new Communication
        {
            Id = communicationId,
            ClientId = client.Id,
            Type = CommunicationTypeEnum.PrivateMail,
            Value = email,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        });
        client.UpdateTime = now;
        client.CurrentUserUpdated = context.UserName;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _clientRepository.Put(client);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _clientRepository.GetNoTracking(client.Id),
                    persisted => persisted.Communications.Any(c =>
                        c.Id == communicationId && c.Value == email && !c.IsDeleted),
                    $"the new email {email} for client '{client.FirstName} {client.Name}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, Email = email, CommunicationId = communicationId },
            $"Email {email} added to {client.FirstName} {client.Name} and confirmed in the database (verified).");
    }
}
