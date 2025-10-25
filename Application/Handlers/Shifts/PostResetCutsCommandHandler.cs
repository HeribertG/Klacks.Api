using AutoMapper;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostResetCutsCommandHandler : BaseHandler, IRequestHandler<PostResetCutsCommand, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftResetService _shiftResetService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostResetCutsCommandHandler(
        IShiftRepository shiftRepository,
        IShiftResetService shiftResetService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostResetCutsCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _shiftResetService = shiftResetService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ShiftResource>> Handle(PostResetCutsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== PostResetCutsCommand START ===");
        _logger.LogInformation("OriginalId: {OriginalId}, NewStartDate: {NewStartDate}",
            request.OriginalId, request.NewStartDate);

        var sealedOrder = await _shiftRepository.GetSealedOrder(request.OriginalId);

        if (sealedOrder == null)
        {
            throw new InvalidOperationException($"No SealedOrder found for OriginalId {request.OriginalId}");
        }

        _logger.LogInformation("Using SealedOrder as template: ID={Id}", sealedOrder.Id);

        var allShifts = await _shiftRepository.CutList(request.OriginalId, request.NewStartDate, tracked: true);

        _logger.LogInformation("Found {Count} shifts (filtered for cuts before {NewStartDate})",
            allShifts.Count, request.NewStartDate);

        if (!allShifts.Any())
        {
            _logger.LogInformation("No shifts to update, returning empty list");
            return new List<ShiftResource>();
        }

        var closeDateObj = request.NewStartDate.AddDays(-1);
        _shiftResetService.CloseExistingSplitShifts(allShifts, closeDateObj);

        await _shiftResetService.CreateNewOriginalShiftFromSealedOrderAsync(sealedOrder, request.NewStartDate);

        await _unitOfWork.CompleteAsync();

        var updatedShifts = await _shiftRepository.CutList(request.OriginalId);
        var result = updatedShifts.Select(s => _mapper.Map<ShiftResource>(s)).ToList();

        _logger.LogInformation("=== PostResetCutsCommand END - Returned {Count} shifts ===", result.Count);
        return result;
    }
}
