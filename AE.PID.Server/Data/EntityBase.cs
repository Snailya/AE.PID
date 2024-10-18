using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

public abstract class EntityBase
{
    [Key] public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ModifiedAt { get; set; }
}