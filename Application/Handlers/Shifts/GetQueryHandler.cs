using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public GetQueryHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.Get(request.Id);
        
        if (shift == null)
        {
            return new ShiftResource();
        }
        
        return _mapper.Map<ShiftResource>(shift);
    }
}
