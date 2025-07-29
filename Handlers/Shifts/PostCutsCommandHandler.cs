using AutoMapper;
using Klacks.Api.Commands.Shifts;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PostCutsCommandHandler(
                        IMapper mapper,
                        IShiftRepository repository,
                        IUnitOfWork unitOfWork,
                        ILogger<PostCutsCommandHandler> logger) : IRequestHandler<PostCutsCommand, List<ShiftResource>>
{
    private readonly ILogger<PostCutsCommandHandler> logger = logger;
    private readonly IMapper mapper = mapper;
    private readonly IShiftRepository repository = repository;
    private readonly IUnitOfWork unitOfWork = unitOfWork;

    public async Task<List<ShiftResource>> Handle(PostCutsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            logger.LogInformation("Starting bulk creation of {Count} cut shifts.", request.Cuts.Count);

            var createdShifts = new List<Shift>();

            foreach (var cutResource in request.Cuts)     
            {
                if (cutResource.Status != ShiftStatus.IsCut)
                {
                    throw new InvalidRequestException($"Shift {cutResource.Id} must have status IsCut. Current status: {cutResource.Status}");
                }

                if (cutResource.OriginalId == null)
                {
                    throw new InvalidRequestException($"Cut shift {cutResource.Id} must have an OriginalId.");
                }

                var groupIdList = cutResource.Groups.Select(x => x.Id).ToList();
                cutResource.Groups.Clear();

                var shift = mapper.Map<ShiftResource, Shift>(cutResource);

                if (shift.OriginalId == null)
                {
                    throw new InvalidRequestException($"Mapped shift must have an OriginalId for cut operations.");
                }

                await repository.Add(shift);
                await repository.UpdateGroupItems(shift.Id, groupIdList);

                createdShifts.Add(shift);
                logger.LogInformation("Created cut shift with ID: {ShiftId}", shift.Id);
            }

            await unitOfWork.CompleteAsync();
            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("Successfully created {Count} cut shifts.", createdShifts.Count);

            return mapper.Map<List<Shift>, List<ShiftResource>>(createdShifts);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(transaction);
            logger.LogError(ex, "Error occurred while creating cut shifts.");
            throw new InvalidRequestException("Error occurred while creating cut shifts. " + ex.Message);
        }
    }
}