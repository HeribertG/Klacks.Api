// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies hourly availability entries for one or more clients: IsAvailable true/false upserts the slot, null
/// removes the stored row of the slot (back to "no entry"); a slot listed twice keeps its last item. When any
/// affected client, including one that only receives removals, is outside the caller's group visibility the whole
/// request is refused exactly like an unknown client; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for every affected client</param>

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
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
            var items = command.Request.Items;

            var clientIds = items.Select(i => i.ClientId).Distinct().ToList();
            var visibleClientIds = await _clientVisibilityGuard.FilterVisibleAsync(
                clientIds, id => id, cancellationToken);
            var hiddenClientId = clientIds.Except(visibleClientIds).Select(id => (Guid?)id).FirstOrDefault();
            if (hiddenClientId.HasValue)
            {
                throw new KeyNotFoundException($"Client with ID {hiddenClientId.Value} not found");
            }

            var lastItemPerSlot = items
                .GroupBy(i => new ClientAvailabilitySlot(i.ClientId, i.Date, i.Hour))
                .Select(g => (Slot: g.Key, Item: g.Last()))
                .ToList();

            var removals = lastItemPerSlot
                .Where(x => !x.Item.IsAvailable.HasValue)
                .Select(x => x.Slot)
                .ToList();
            var upserts = lastItemPerSlot
                .Where(x => x.Item.IsAvailable.HasValue)
                .Select(x => _mapper.ToEntity(x.Item))
                .ToList();

            if (removals.Count > 0)
            {
                await _repository.BulkRemove(removals);
            }

            if (upserts.Count > 0)
            {
                await _repository.BulkUpsert(upserts);
            }

            await _unitOfWork.CompleteAsync();

            return lastItemPerSlot.Count;
        }, "BulkUpdateClientAvailability", new { Count = command.Request.Items.Count });
    }
}
