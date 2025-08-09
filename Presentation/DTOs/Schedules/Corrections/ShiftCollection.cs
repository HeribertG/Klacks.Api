using Klacks.Api.Domain.Enums;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Presentation.DTOs.Schedules.Corrections;

public class ShiftCollection
{
    private readonly List<PlaceholderEnum> _placeholders;

    public Shift Shift { get; init; } = null!;

    public IReadOnlyList<PlaceholderEnum> Placeholders => _placeholders;

    public ShiftCollection(Shift shift, List<PlaceholderEnum> placeholders)
    {
        Shift = shift ?? throw new ArgumentNullException(nameof(shift));
        _placeholders = placeholders ?? throw new ArgumentNullException(nameof(placeholders));
    }

    public PlaceholderEnum this[int dayIndex]
    {
        get => dayIndex >= 0 && dayIndex < _placeholders.Count ? _placeholders[dayIndex] : PlaceholderEnum.Empty;
        set
        {
            if (dayIndex >= 0 && dayIndex < _placeholders.Count)
            {
                _placeholders[dayIndex] = value;
            }
        }
    }

    public int TotalDays => _placeholders.Count;

    public int EmptyDays => _placeholders.Count(p => p == PlaceholderEnum.Empty);

    public int ExistingDays => _placeholders.Count(p => p == PlaceholderEnum.Exist);

    public int PlannedDays => _placeholders.Count(p => p == PlaceholderEnum.Planned);
}