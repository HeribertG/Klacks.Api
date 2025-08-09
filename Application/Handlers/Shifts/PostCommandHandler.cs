using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler(
                        IMapper mapper,
                        IShiftRepository repository,
                        IUnitOfWork unitOfWork,
                        ILogger<PostCommandHandler> logger) : IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly ILogger<PostCommandHandler> logger = logger;
    private readonly IMapper mapper = mapper;
    private readonly IShiftRepository repository = repository;
    private readonly IUnitOfWork unitOfWork = unitOfWork;

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            var idList = request.Resource.Groups.Select(x => x.Id).ToList();

            request.Resource.Groups.Clear();

            var shift = mapper.Map<ShiftResource, Shift>(request.Resource);

            await StoreInRepostitory(shift, idList);
            
            await unitOfWork.CompleteAsync();

            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("New shift added successfully. ID: {ShiftId}", shift.Id);

            return mapper.Map<Shift, ShiftResource>(shift);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(transaction);
            logger.LogError(ex, "Error occurred while adding a new shift.");
            throw new InvalidRequestException("Error occurred while adding a new shift. "  + ex.Message);
        }
    }

    private async Task StoreInRepostitory(Shift shift, List<Guid> groupIdList)
    {
        var shiftStatus = shift.Status;

        switch (shiftStatus)
        {
            case ShiftStatus.Original:
                await Store(shift, groupIdList);
                break;

            case ShiftStatus.IsCut:
            case ShiftStatus.IsCutOriginal:
                throw new InvalidRequestException("This service may not be saved due to its status!");
            case ShiftStatus.ReadyToCut:
                shift.Status = ShiftStatus.IsCutOriginal;
                await Store(shift, groupIdList);
                shift.OriginalId = shift.Id;
                shift.Id = Guid.Empty;
                shift.Status = ShiftStatus.IsCut;
                await Store(shift, groupIdList);
                break;
        }
     }

    private async Task Store(Shift shift, List<Guid> groupIdList)
    {
        await repository.Add(shift);

        await repository.UpdateGroupItems(shift.Id, groupIdList);
    }
}
