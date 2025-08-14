using AutoMapper;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCutsCommandHandler : IRequestHandler<PutCutsCommand, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public PutCutsCommandHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<List<ShiftResource>> Handle(PutCutsCommand request, CancellationToken cancellationToken)
    {
        var updatedShifts = new List<ShiftResource>();
        
        foreach (var cutResource in request.Cuts)
        {
            var shift = _mapper.Map<Shift>(cutResource);
            var updatedShift = await _shiftRepository.Put(shift);
            
            if (updatedShift != null)
            {
                updatedShifts.Add(_mapper.Map<ShiftResource>(updatedShift));
            }
        }
        
        return updatedShifts;
    }
}