// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one break placeholder. A placeholder owned by a client outside the caller's group visibility is
/// answered exactly like a placeholder that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see or write for the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<BreakPlaceholderResource>, BreakPlaceholderResource>
    {
        private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(
            IBreakPlaceholderRepository breakPlaceholderRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            ScheduleMapper scheduleMapper,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _breakPlaceholderRepository = breakPlaceholderRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<BreakPlaceholderResource> Handle(GetQuery<BreakPlaceholderResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var breakEntity = await _breakPlaceholderRepository.Get(request.Id);

                if (breakEntity == null
                    || !await _clientVisibilityGuard.IsVisibleAsync(breakEntity.ClientId, cancellationToken))
                {
                    throw new KeyNotFoundException($"Break with ID {request.Id} not found");
                }

                return _scheduleMapper.ToBreakPlaceholderResource(breakEntity);
            }, nameof(Handle), new { request.Id });
        }
    }
}
