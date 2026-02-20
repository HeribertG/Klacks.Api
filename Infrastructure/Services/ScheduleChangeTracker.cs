using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleChangeTracker : IScheduleChangeTracker
{
    private readonly DataBaseContext _context;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScheduleChangeTracker(
        DataBaseContext context,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task TrackChangeAsync(Guid clientId, DateOnly changeDate)
    {
        var existing = await _context.ScheduleChange
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(sc => sc.ClientId == clientId && sc.ChangeDate == changeDate);

        if (existing != null)
        {
            existing.IsDeleted = false;
            existing.UpdateTime = DateTime.UtcNow;
            _context.ScheduleChange.Update(existing);
        }
        else
        {
            var entry = new ScheduleChange
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ChangeDate = changeDate,
                CreateTime = DateTime.UtcNow
            };
            await _context.ScheduleChange.AddAsync(entry);
        }

        await _context.SaveChangesAsync();

        var connectionId = _httpContextAccessor.HttpContext?.Request.Query["connectionId"].FirstOrDefault() ?? string.Empty;
        var notification = new ScheduleChangeNotificationDto
        {
            ClientId = clientId,
            ChangeDate = changeDate,
            SourceConnectionId = connectionId
        };
        await _notificationService.NotifyScheduleChangeTracked(notification);
    }

    public async Task<List<ScheduleChange>> GetChangesAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.ScheduleChange
            .Where(sc => sc.ChangeDate >= startDate && sc.ChangeDate <= endDate)
            .ToListAsync();
    }
}
