namespace ActivoosCRM.Domain.Common;

/// <summary>
/// Interface for entities that support soft delete
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
