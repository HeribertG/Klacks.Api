// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: updates the birthdate of a client identified by name.
/// The write is self-verifying: it runs in a transaction and the new birthdate is re-read
/// fresh from the database; a mismatch rolls the update back.
/// </summary>
/// <param name="firstName">First name of the client to update.</param>
/// <param name="lastName">Last name of the client to update.</param>
/// <param name="birthdate">New birthdate in ISO format (YYYY-MM-DD).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateClientBirthdateSkill : BaseSkillImplementation
{
    private const string SkillName = "update_client_birthdate";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClientBirthdateSkill(
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
        var birthdateStr = GetRequiredString(parameters, "birthdate");

        if (!DateTime.TryParse(birthdateStr, out var birthdate))
        {
            return SkillResult.Error($"Invalid birthdate format: '{birthdateStr}'. Expected YYYY-MM-DD.");
        }

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var now = DateTime.UtcNow;
        client!.Birthdate = birthdate;
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
                    persisted => persisted.Birthdate.HasValue && persisted.Birthdate.Value.Date == birthdate.Date,
                    $"the new birthdate {birthdate:yyyy-MM-dd} of client '{client.FirstName} {client.Name}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, Birthdate = birthdate.ToString("yyyy-MM-dd") },
            $"Birthdate of {client.FirstName} {client.Name} updated to {birthdate:yyyy-MM-dd} " +
            "and confirmed in the database (verified).");
    }
}
