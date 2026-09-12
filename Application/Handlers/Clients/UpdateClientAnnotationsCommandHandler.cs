// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes the notes of one client and leaves every other field of the stored client untouched. This is
/// the path a caller who holds CanEditClientNotes but not CanEditClients takes through PUT
/// api/backend/Clients: the note card saves the whole client resource, and only the notes out of it are
/// applied. Nothing is compared against the sent resource, so there is no window between reading the
/// stored client and writing it, and no false refusal when a field the caller never touched merely
/// looks different.
/// </summary>
/// <param name="request">Carries the client id and the complete note list as it should be afterwards</param>

using Klacks.Api.Application.Commands.Clients;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients;

public class UpdateClientAnnotationsCommandHandler
    : BaseHandler, IRequestHandler<UpdateClientAnnotationsCommand, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClientAnnotationsCommandHandler(
        IClientRepository clientRepository,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        ILogger<UpdateClientAnnotationsCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientResource?> Handle(
        UpdateClientAnnotationsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var annotations = request.Annotations
                .Select(_clientMapper.ToAnnotationEntity)
                .ToList();

            var client = await _clientRepository.PutAnnotations(request.ClientId, annotations);
            if (client == null)
            {
                return null;
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Client notes updated: {ClientId}", request.ClientId);

            // Answered from a fresh read rather than from the entity just written, for two reasons: the
            // note card reloads the whole client from this response, and the tracked entity carries a
            // freshly added note twice — once added to the navigation collection by the synchronisation,
            // once by EF's own fixup. What comes back is therefore what is actually stored.
            return _clientMapper.ToResource(await _clientRepository.GetNoTracking(request.ClientId));
        },
        "updating client notes",
        new { request.ClientId });
    }
}
