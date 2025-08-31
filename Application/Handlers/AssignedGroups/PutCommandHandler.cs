using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AssignedGroupResource>, AssignedGroupResource?>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        
        public PutCommandHandler(
            IAssignedGroupRepository assignedGroupRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
        : base(logger)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            }

        public async Task<AssignedGroupResource?> Handle(PutCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
        {
            var existingAssignedGroup = await _assignedGroupRepository.Get(request.Resource.Id);
            if (existingAssignedGroup == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.Resource.Id} not found.");
            }

                _mapper.Map(request.Resource, existingAssignedGroup);
                await _assignedGroupRepository.Put(existingAssignedGroup);
                await _unitOfWork.CompleteAsync();
                return _mapper.Map<AssignedGroupResource>(existingAssignedGroup);
        }, 
        "updating group", 
        new { GroupId = request.Resource.Id });
    }
    }
}
