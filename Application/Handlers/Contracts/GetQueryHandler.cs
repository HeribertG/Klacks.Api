// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ContractResource>, ContractResource>
    {
        private readonly IContractRepository _contractRepository;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(IContractRepository contractRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _contractRepository = contractRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<ContractResource> Handle(GetQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var contract = await _contractRepository.Get(request.Id);

                if (contract == null)
                {
                    throw new KeyNotFoundException($"Contract with ID {request.Id} not found");
                }

                return _scheduleMapper.ToContractResource(contract);
            }, nameof(Handle), new { request.Id });
        }
    }
}
