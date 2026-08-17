// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class UserAbsencePeriodRepository : IUserAbsencePeriodRepository
{
    private readonly DataBaseContext _context;

    public UserAbsencePeriodRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserAbsencePeriod>> GetByUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        return await _context.UserAbsencePeriod
            .Where(p => p.AppUserId == appUserId)
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAbsencePeriod> AddAsync(UserAbsencePeriod period, CancellationToken cancellationToken = default)
    {
        _context.UserAbsencePeriod.Add(period);
        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _context.UserAbsencePeriod.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (period is null)
        {
            return false;
        }

        _context.UserAbsencePeriod.Remove(period);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
