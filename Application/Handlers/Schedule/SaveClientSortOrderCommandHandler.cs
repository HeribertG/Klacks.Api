// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replaces the client sort order for the user+group by soft-deleting old entries and inserting the new set.
/// </summary>

using Klacks.Api.Application.Commands.Schedule;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Schedule;

public class SaveClientSortOrderCommandHandler
    : BaseHandler, IRequestHandler<SaveClientSortOrderCommand, bool>
{
    private readonly IClientSortPreferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveClientSortOrderCommandHandler(
        IClientSortPreferenceRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<SaveClientSortOrderCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SaveClientSortOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var newEntries = request.Entries.Select(dto => new ClientSortPreference
            {
                UserId = request.UserId,
                GroupId = request.GroupId,
                ClientId = dto.ClientId,
                SortOrder = dto.SortOrder
            });

            await _repository.ReplaceAllAsync(request.UserId, request.GroupId, newEntries);
            await _unitOfWork.CompleteAsync();
            return true;
        }, "save client sort order", new { request.UserId, request.GroupId, Count = request.Entries.Count });
    }
}
