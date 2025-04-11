using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AE.PID.Core;
using AE.PID.Core.DTOs;
using Refit;

namespace AE.PID.Client.Infrastructure.VisioExt;

public interface IDocumentApi
{
#if DEBUG
    [Get("/api/v3/masters/snapshots?status=0")]
    Task<IEnumerable<MasterSnapshotDto>> GetCurrentSnapshot();
#else
        [Get("/api/v3/masters/snapshots?status=1")]
    Task<IEnumerable<MasterSnapshotDto>> GetCurrentSnapshot();
#endif
    [Multipart]
    [Post("/api/v3/documents/update")]
    // 2025.02.03: IFormFile在Refit中可以对应StreamPart,ByteArrayPart,FileInfoPart，无论是哪个都可以。
    Task<Stream> Update([AliasAs("file")] ByteArrayPart file, [AliasAs("data")] string? data = null,
        [Query] int status = 1);
}