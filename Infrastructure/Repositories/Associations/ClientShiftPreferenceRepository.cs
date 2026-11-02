// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for managing client shift preferences with shift include support. The list of a client and its bulk delete
/// see the real rows only: the preference clones of a scenario keep their token and stay with that scenario. The list also
/// leaves out a real row on a shift with a token, which an older save could leave behind; a save removes such a row.
/// </summary>
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class ClientShiftPreferenceRepository : BaseRepository<ClientShiftPreference>, IClientShiftPreferenceRepository
{
    public ClientShiftPreferenceRepository(DataBaseContext context, ILogger<ClientShiftPreference> logger)
        : base(context, logger)
    {
    }

    public async Task<List<ClientShiftPreference>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default)
    {
        return await context.ClientShiftPreference
            .Include(csp => csp.Shift)
            .Where(csp => csp.ClientId == clientId && csp.AnalyseToken == null && csp.Shift!.AnalyseToken == null)
            .OrderBy(csp => csp.PreferenceType)
            .ThenBy(csp => csp.Shift!.Name)
            .ToListAsync(ct);
    }

    public async Task<List<ClientShiftPreference>> GetByShiftIdAsync(Guid shiftId, CancellationToken ct = default)
    {
        return await context.ClientShiftPreference
            .Where(csp => csp.ShiftId == shiftId && csp.AnalyseToken == null)
            .ToListAsync(ct);
    }

    public async Task<ClientShiftPreference?> GetByClientAndShiftAsync(
        Guid clientId, Guid shiftId, CancellationToken ct = default)
    {
        return await context.ClientShiftPreference
            .FirstOrDefaultAsync(
                csp => csp.ClientId == clientId && csp.ShiftId == shiftId && csp.AnalyseToken == null, ct);
    }

    public Task<IReadOnlyList<ScopedShiftPreference>> GetScopedByClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds, Guid? analyseToken, CancellationToken ct = default)
        => ShiftPreferenceScopeQuery.LoadAsync(context, clientIds, analyseToken, ct);

    public async Task DeleteAllByClientIdAsync(Guid clientId, CancellationToken ct = default)
    {
        var existing = await context.ClientShiftPreference
            .Where(csp => csp.ClientId == clientId && csp.AnalyseToken == null)
            .ToListAsync(ct);

        context.ClientShiftPreference.RemoveRange(existing);
    }
}
