using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IMapper mapper;
    private readonly IShiftRepository repository;

    public GetQueryHandler(
                           IMapper mapper,
                           IShiftRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        var shift = await repository.Get(request.Id);
        try
        {
            var result = mapper.Map<Models.Schedules.Shift, ShiftResource>(shift!);
            return result;
        }
        catch (Exception ex)
        {

            throw;
        }
        
    }
}
