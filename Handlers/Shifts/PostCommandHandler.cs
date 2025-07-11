using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

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
        try
        {
            var idList= request.Resource.Groups.Select(x=> x.Id).ToList();

            request.Resource.Groups.Clear();    

            var shift = mapper.Map<ShiftResource, Models.Schedules.Shift>(request.Resource);

            await repository.Add(shift);

            await repository.UpdateGroupItems(shift.Id, idList);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New shift added successfully. ID: {ShiftId}", shift.Id);

            return mapper.Map<Models.Schedules.Shift, ShiftResource>(shift);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new shift.");
            throw;
        }
    }
}
