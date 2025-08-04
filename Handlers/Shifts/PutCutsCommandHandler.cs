using AutoMapper;
using Klacks.Api.Commands.Shifts;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PutCutsCommandHandler(
                        IMapper mapper,
                        IShiftRepository repository,
                        IUnitOfWork unitOfWork,
                        ILogger<PutCutsCommandHandler> logger) : IRequestHandler<PutCutsCommand, List<ShiftResource>>
{
    private readonly ILogger<PutCutsCommandHandler> logger = logger;
    private readonly IMapper mapper = mapper;
    private readonly IShiftRepository repository = repository;
    private readonly IUnitOfWork unitOfWork = unitOfWork;

    public async Task<List<ShiftResource>> Handle(PutCutsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            logger.LogInformation("Starting bulk update of {Count} cut shifts.", request.Cuts.Count);

            var updatedShifts = new List<Shift>();

            foreach (var cutResource in request.Cuts)
            {
                if (cutResource.Status != ShiftStatus.IsCut)
                {
                    throw new InvalidRequestException($"Shift {cutResource.Id} must have status IsCut. Current status: {cutResource.Status}");
                }

                var existingShift = await repository.Get(cutResource.Id);
                if (existingShift == null)
                {
                    throw new InvalidRequestException($"Shift with ID {cutResource.Id} not found.");
                }

                var groupIdList = cutResource.Groups.Select(x => x.Id).ToList();
                cutResource.Groups.Clear();
                
                // Preserve nested set values before mapping
                var preservedLft = existingShift.Lft;
                var preservedRgt = existingShift.Rgt;
                var preservedParentId = existingShift.ParentId;
                var preservedRootId = existingShift.RootId;
                var preservedOriginalId = existingShift.OriginalId;
               
                mapper.Map(cutResource, existingShift);
                
                // Restore preserved nested set values after mapping
                existingShift.Lft = preservedLft;
                existingShift.Rgt = preservedRgt;
                existingShift.ParentId = preservedParentId;
                existingShift.RootId = preservedRootId;
                existingShift.OriginalId = preservedOriginalId;
                existingShift.Status = ShiftStatus.IsCut;

                await repository.Put(existingShift);
                await repository.UpdateGroupItems(existingShift.Id, groupIdList);

                updatedShifts.Add(existingShift);
                logger.LogInformation("Updated cut shift with ID: {ShiftId}", existingShift.Id);
            }

            await unitOfWork.CompleteAsync();
            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("Successfully updated {Count} cut shifts.", updatedShifts.Count);

            return mapper.Map<List<Shift>, List<ShiftResource>>(updatedShifts);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(transaction);
            logger.LogError(ex, "Error occurred while updating cut shifts.");
            throw new InvalidRequestException("Error occurred while updating cut shifts. " + ex.Message);
        }
    }
}