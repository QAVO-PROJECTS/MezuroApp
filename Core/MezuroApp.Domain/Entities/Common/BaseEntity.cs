namespace MezuroApp.Domain.Entities.Common;

public class BaseEntity
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime DeletedDate { get; set; }
    
}