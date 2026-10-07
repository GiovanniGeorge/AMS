namespace AMS.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>User who created the record. FK to User.Id (configured in ApplicationDbContext).</summary>
    public int? CreatedById { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    /// <summary>User who last modified the record. FK to User.Id.</summary>
    public int? ModifiedById { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
