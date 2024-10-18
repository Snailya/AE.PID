using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using Refit;

namespace AE.PID.Visio.Shared;

public interface IDocumentApi
{
#if DEBUG
    [Get("/api/v3/masters/snapshots?status=0")]
    Task<IEnumerable<MasterSnapshotDto>> GetCurrentSnapshot();
#else
        [Get("/api/v3/masters/snapshots?status=1")]
    Task<IEnumerable<MasterSnapshotDto>> GetCurrentSnapshot();
#endif
    [Post("/api/v3/documents/update")]
    Task<Stream> Update(ByteArrayContent content);
}