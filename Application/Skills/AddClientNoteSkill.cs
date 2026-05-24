// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: adds a note (annotation) to an existing client identified by name.
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="note">The note text to add.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_client_note")]
public class AddClientNoteSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientNoteSkill(
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
        var note = GetRequiredString(parameters, "note");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var now = DateTime.UtcNow;
        client!.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Note = note,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        });
        client.UpdateTime = now;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name },
            $"Note added to {client.FirstName} {client.Name}.");
    }
}
