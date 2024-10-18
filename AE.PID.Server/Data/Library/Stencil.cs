using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

public class Stencil : EntityBase
{
    [Required] public string Name { get; set; }

    #region -- Navigation Properties --

    public ICollection<StencilSnapshot> StencilSnapshots { get; set; } = [];

    #endregion
}