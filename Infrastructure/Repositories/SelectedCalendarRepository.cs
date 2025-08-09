using Klacks.Api.Datas;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class SelectedCalendarRepository : BaseRepository<SelectedCalendar>, ISelectedCalendarRepository
{
    private readonly DataBaseContext context;

    public SelectedCalendarRepository(DataBaseContext context, ILogger<SelectedCalendar> logger)
      : base(context, logger)
    {
        this.context = context;
    }

    public async Task AddPulk(SelectedCalendar[] models)
    {
        Logger.LogInformation("Adding {Count} SelectedCalendar entities in bulk.", models.Length);
        try
        {
            this.context.Set<SelectedCalendar>().AddRange(models);
            await this.context.SaveChangesAsync();
            Logger.LogInformation("{Count} SelectedCalendar entities added successfully.", models.Length);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to add {Count} SelectedCalendar entities in bulk. Database update error.", models.Length);
            throw new InvalidRequestException($"Failed to add {models.Length} SelectedCalendar entities due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while adding {Count} SelectedCalendar entities.", models.Length);
            throw;
        }
    }

    public async Task DeleteFromParend(Guid id)
    {
        Logger.LogInformation("Deleting SelectedCalendar entries for parent ID: {ParentId}", id);
        try
        {
            var entriesToDelete = await this.context.Set<SelectedCalendar>().Where(sc => sc.CalendarSelection != null && sc.CalendarSelection.Id == id).ToListAsync();
            if (entriesToDelete.Any())
            {
                this.context.Set<SelectedCalendar>().RemoveRange(entriesToDelete);
                await this.context.SaveChangesAsync();
                Logger.LogInformation("{Count} SelectedCalendar entries deleted for parent ID: {ParentId}.", entriesToDelete.Count, id);
            }
            else
            {
                Logger.LogInformation("No SelectedCalendar entries found for parent ID: {ParentId} to delete.", id);
            }
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to delete SelectedCalendar entries for parent ID: {ParentId}. Database update error.", id);
            throw new InvalidRequestException($"Failed to delete SelectedCalendar entries for parent ID {id} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while deleting SelectedCalendar entries for parent ID: {ParentId}.", id);
            throw;
        }
    }
}
