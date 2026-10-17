// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stores "advanced" as the caller's explanation-depth override through the same service the set_explain_depth skill
/// uses. A caller id that is not a GUID yields NoUser and writes nothing.
/// </summary>
/// <param name="overrideService">Writes the override and drops the cached depth</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetAdvancedExplainDepthCommandHandler : IRequestHandler<SetAdvancedExplainDepthCommand, ExplainDepthOverrideOutcome>
{
    private readonly IExplainDepthOverrideService _overrideService;

    public SetAdvancedExplainDepthCommandHandler(IExplainDepthOverrideService overrideService)
    {
        _overrideService = overrideService;
    }

    public async Task<ExplainDepthOverrideOutcome> Handle(SetAdvancedExplainDepthCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return ExplainDepthOverrideOutcome.NoUser;
        }

        return await _overrideService.SetAsync(userId, ExplainDepthDefaults.OverrideValueAdvanced, cancellationToken);
    }
}
