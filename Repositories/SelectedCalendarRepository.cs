using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.CalendarSelections;

namespace Klacks.Api.Repositories
{
    public class SelectedCalendarRepository : BaseRepository<SelectedCalendar>, ISelectedCalendarRepository
    {
        private readonly DataBaseContext context;

        public SelectedCalendarRepository(DataBaseContext context, ILogger<SelectedCalendar> logger)
          : base(context, logger)
        {
            this.context = context;
        }

        public void AddPulk(SelectedCalendar[] models)
        {
            this.context.Set<SelectedCalendar>().AddRange(models);
        }

        public void DeleteFromParend(Guid id)
        {
            var entriesToDelete = this.context.Set<SelectedCalendar>().Where(sc => sc.CalendarSelection != null && sc.CalendarSelection.Id == id).ToList();
            this.context.Set<SelectedCalendar>().RemoveRange(entriesToDelete);
        }
    }
}
