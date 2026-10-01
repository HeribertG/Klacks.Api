// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Upserts hourly availability entries for one or more clients. When any affected client is outside the
/// caller's group visibility the whole request is refused exactly like an unknown client; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for every affected client</param>

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class BulkUpdateCommandHandler : BaseHandler, IRequestHandler<BulkUpdateClientAvailabilityCommand, int>
{
    private readonly IClientAvailabilityRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ClientAvailabilityMapper _mapper;

    public BulkUpdateCommandHandler(
        IClientAvailabilityRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        ClientAvailabilityMapper mapper,
        ILogger<BulkUpdateCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<int> Handle(
        BulkUpdateClientAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = command.Request.Items
                .Select(_mapper.ToEntity)
                .ToList();

            var clientIds = entities.Select(e => e.ClientId).Distinct().ToList();
            var visibleClientIds = await _clientVisibilityGuard.FilterVisibleAsync(
                clientIds, id => id, cancellationToken);
            var hiddenClientId = clientIds.Except(visibleClientIds).Select(id => (Guid?)id).FirstOrDefault();
            if (hiddenClientId.HasValue)
            {
                throw new KeyNotFoundException($"Client with ID {hiddenClientId.Value} not found");
            }

            await _repository.BulkUpsert(entities);
            await _unitOfWork.CompleteAsync();

            return entities.Count;
        }, "BulkUpdateClientAvailability", new { Count = command.Request.Items.Count });
    }
}
