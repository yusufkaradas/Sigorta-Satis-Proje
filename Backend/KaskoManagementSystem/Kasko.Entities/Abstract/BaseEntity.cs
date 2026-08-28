namespace Kasko.Entities.Abstract;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public Guid? DeletedBy { get; set; }
}