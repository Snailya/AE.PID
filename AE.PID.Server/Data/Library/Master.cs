using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

public class Master : EntityBase
{
    [Required] public string BaseId { get; set; }

    [Required] public string Name { get; set; }

    #region -- Navigation Properties --

    public ICollection<MasterContentSnapshot> MasterContentSnapshots { get; set; } = [];

    #endregion
}