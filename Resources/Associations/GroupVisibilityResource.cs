namespace Klacks.Api.Resources.Associations
{
    public class GroupVisibilityResource
    {
        public Guid Id { get; set; }

        public required string AppUserId { get; set; }

        public Guid GroupId { get; set; }
    }
}
