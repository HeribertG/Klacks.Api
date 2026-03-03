// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class BulkGroupVisibilitiesCommandHandler : BaseHandler, IRequestHandler<BulkGroupVisibilitiesCommand>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public BulkGroupVisibilitiesCommandHandler(
        ILogger<BulkGroupVisibilitiesCommandHandler> logger,
        IGroupVisibilityRepository groupVisibilityRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(BulkGroupVisibilitiesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk update of GroupVisibility list with {Count} items.", request.List.Count);

        await ExecuteAsync(async () =>
        {
            var groupVisibilities = request.List.Select(v => _groupMapper.ToGroupVisibilityEntity(v)).ToList();
            await _groupVisibilityRepository.SetGroupVisibilityList(groupVisibilities);
            await _unitOfWork.CompleteAsync();
        },
        "operation",
        new { });

        return Unit.Value;
    }
}
