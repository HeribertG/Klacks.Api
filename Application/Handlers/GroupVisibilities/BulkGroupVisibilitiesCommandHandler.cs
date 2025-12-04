using AutoMapper;
using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class BulkGroupVisibilitiesCommandHandler : BaseHandler, IRequestHandler<BulkGroupVisibilitiesCommand>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public BulkGroupVisibilitiesCommandHandler(
        ILogger<BulkGroupVisibilitiesCommandHandler> logger,
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(BulkGroupVisibilitiesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk update of GroupVisibility list with {Count} items.", request.List.Count);

        await ExecuteAsync(async () =>
        {
            var groupVisibilities = _mapper.Map<List<GroupVisibility>>(request.List);
            await _groupVisibilityRepository.SetGroupVisibilityList(groupVisibilities);
            await _unitOfWork.CompleteAsync();
        },
        "operation",
        new { });

        return Unit.Value;
    }
}
