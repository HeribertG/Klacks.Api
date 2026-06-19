// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.ShiftSchedule;

namespace Klacks.Api.Application.Common;

/// <summary>
/// Resolves the visible-group hierarchy for the calling HTTP request.
/// Reads the X-Selected-Group header set by the frontend group-selection
/// interceptor and expands it into the full subtree via
/// <see cref="IShiftGroupFilterService"/>. Returns null when no header is
/// present (admin / unfiltered view) so the SP keeps backwards-compatible
/// is_group_restricted=false semantics.
/// </summary>
/// <param name="httpContextAccessor">Access to the current request headers.</param>
/// <param name="groupFilterService">Expands the selected group into the visible subtree.</param>
public class SelectedGroupContextResolver : ISelectedGroupContextResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IShiftGroupFilterService _groupFilterService;

    public SelectedGroupContextResolver(
        IHttpContextAccessor httpContextAccessor,
        IShiftGroupFilterService groupFilterService)
    {
        _httpContextAccessor = httpContextAccessor;
        _groupFilterService = groupFilterService;
    }

    public async Task<List<Guid>?> ResolveVisibleGroupIdsAsync()
    {
        var headerValue = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SelectedGroup]
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        if (!Guid.TryParse(headerValue, out var selectedGroupId))
        {
            return null;
        }

        var ids = await _groupFilterService.GetVisibleGroupIdsAsync(selectedGroupId);
        return ids.Count > 0 ? ids : null;
    }
}
