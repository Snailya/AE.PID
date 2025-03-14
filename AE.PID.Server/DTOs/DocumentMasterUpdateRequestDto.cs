using System.ComponentModel.DataAnnotations;
using AE.PID.Core;

namespace AE.PID.Server.DTOs;

public class DocumentMasterUpdateRequestDto
{
    [Required] public IFormFile File { get; set; }

    public MasterDto[]? Items { get; set; } = null;
}