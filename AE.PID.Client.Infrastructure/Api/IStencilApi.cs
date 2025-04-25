using System.Collections.Generic;
using System.Threading.Tasks;
using AE.PID.Core;
using Refit;

namespace AE.PID.Client.Infrastructure;

public interface IStencilApi
{
#if DEBUG
    [Get("/api/v3/stencils/snapshots?status=0")]
    Task<IEnumerable<StencilSnapshotSyncDto>> GetCurrentSnapshot();
#else
        [Get("/api/v3/stencils/snapshots?status=1")]
    Task<IEnumerable<StencilSnapshotDto>> GetCurrentSnapshot();
#endif
}