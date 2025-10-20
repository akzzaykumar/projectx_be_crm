namespace ActivoosCRM.Domain.Common;

/// <summary>
/// Auditable entity with tracking of created/updated by users
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
