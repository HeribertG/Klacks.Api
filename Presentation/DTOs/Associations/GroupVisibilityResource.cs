namespace Klacks.Api.Presentation.DTOs.Associations
{
    public class GroupVisibilityResource
    {
        public Guid Id { get; set; }

        public required string AppUserId { get; set; }

        public Guid GroupId { get; set; }
    }
}
