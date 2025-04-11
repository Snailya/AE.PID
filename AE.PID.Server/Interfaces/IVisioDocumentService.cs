using AE.PID.Core;
using AE.PID.Server.Data;

namespace AE.PID.Server;

public interface IVisioDocumentService
{
    Task<string> UpdateDocumentStencils(string? clientIp, IFormFile file, MasterDto[]? items, SnapshotStatus status);
}