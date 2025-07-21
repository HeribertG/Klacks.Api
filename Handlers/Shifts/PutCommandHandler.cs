using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Datas;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PutCommandHandler : IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IShiftRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                            IMapper mapper,
                            IShiftRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();

        try
        {
            var dbShift = await repository.Get(request.Resource.Id);
            if (dbShift == null)
            {
                logger.LogWarning("Shift with ID {ShiftId} not found.", request.Resource.Id);
                return null;
            }

            var idList = request.Resource.Groups.Select(x => x.Id).ToList();

            request.Resource.Groups.Clear();

            var updatedShift = mapper.Map(request.Resource, dbShift);

            await StoreInRepostitory(updatedShift, idList);

            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("Shift with ID {ShiftId} updated successfully.", request.Resource.Id);

            var getShift = await repository.Get(request.Resource.Id);

            return mapper.Map<Shift, ShiftResource>(getShift!);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(transaction);
            logger.LogError(ex, "Error occurred while updating shift with ID {ShiftId}.", request.Resource.Id);
            throw new InvalidRequestException("Error occurred while updating shift with ID {ShiftId}. " + ex.Message);
        }
    }

    private async Task StoreInRepostitory(Shift shift, List<Guid> groupIdList)
    {
        var shiftStatus = shift.Status;

        switch (shiftStatus)
        {
            case ShiftStatus.IsCut:
            case ShiftStatus.Original:
                await Store(shift, groupIdList);
                break;
            
            case ShiftStatus.IsCutOriginal:
                throw new InvalidRequestException("This service may not be saved due to its status!");
            case ShiftStatus.ReadyToCut:
                shift.Status = ShiftStatus.IsCutOriginal;
                await Store(shift, groupIdList);
                await unitOfWork.CompleteAsync();
                shift.OriginalId = shift.Id;
                shift.Id = Guid.Empty;
                shift.Status = ShiftStatus.IsCut;
                await StorePost(shift, groupIdList);
                await unitOfWork.CompleteAsync();
                break;
        }
    }

    private async Task Store(Shift shift, List<Guid> groupIdList)
    {
        await repository.Put(shift);

        await repository.UpdateGroupItems(shift.Id, groupIdList);
    }

    private async Task StorePost(Shift shift, List<Guid> groupIdList)
    {
        await repository.Add(shift);

        await repository.UpdateGroupItems(shift.Id, groupIdList);
    }
}
