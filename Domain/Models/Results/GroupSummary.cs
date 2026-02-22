namespace Klacks.Api.Domain.Models.Results;

public class GroupSummary
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public DateOnly? ValidFrom { get; set; }
    
    public DateOnly? ValidTo { get; set; }
    
    public bool IsActive { get; set; }
    
    public bool IsFormer { get; set; }
    
    public bool IsFuture { get; set; }
    
    public int? ParentId { get; set; }
    
    public string ParentName { get; set; } = string.Empty;
    
    public int Depth { get; set; }
    
    public string Path { get; set; } = string.Empty;
    
    public int MemberCount { get; set; }
    
    public int DirectMemberCount { get; set; }
    
    public int SubGroupCount { get; set; }
    
    public int LeftValue { get; set; }
    
    public int RightValue { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
}