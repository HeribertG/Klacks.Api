// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AssignedGroupResource>, AssignedGroupResource?>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly IUnitOfWork _unitOfWork;
        
        public PutCommandHandler(
            IAssignedGroupRepository assignedGroupRepository,
            GroupMapper groupMapper,
            IUnitOfWork unitOfWork,
            ILogger<PutCommandHandler> logger)
        : base(logger)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _groupMapper = groupMapper;
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

                var updatedAssignedGroup = _groupMapper.ToAssignedGroupEntity(request.Resource);
                updatedAssignedGroup.CreateTime = existingAssignedGroup.CreateTime;
                updatedAssignedGroup.CurrentUserCreated = existingAssignedGroup.CurrentUserCreated;
                existingAssignedGroup = updatedAssignedGroup;
                await _assignedGroupRepository.Put(existingAssignedGroup);
                await _unitOfWork.CompleteAsync();
                return _groupMapper.ToAssignedGroupResource(existingAssignedGroup);
        }, 
        "updating group", 
        new { GroupId = request.Resource.Id });
    }
    }
}
