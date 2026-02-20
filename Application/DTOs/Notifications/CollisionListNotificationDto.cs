namespace Klacks.Api.Application.DTOs.Notifications;

public record CollisionListNotificationDto
{
    public List<CollisionNotificationDto> Collisions { get; init; } = [];
    public bool IsFullRefresh { get; init; }
    public Guid? CheckedClientId { get; init; }
    public DateOnly? CheckedDate { get; init; }
}
