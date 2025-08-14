using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCommandHandler : IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public PutCommandHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var shift = _mapper.Map<Shift>(request.Resource);
        var updatedShift = await _shiftRepository.Put(shift);
        
        if (updatedShift == null)
        {
            return null;
        }
        
        return _mapper.Map<ShiftResource>(updatedShift);
    }
}
