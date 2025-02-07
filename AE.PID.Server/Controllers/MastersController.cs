using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class MastersController(ILogger<MastersController> logger, AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    ///     获取所有的模具。用于客户端比较本地的文档中引用的Master版本是否为最新的。
    /// </summary>
    /// <param name="status"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    [HttpGet("snapshots")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetSnapshots([FromQuery] SnapshotStatus status = SnapshotStatus.Published,
        [FromQuery] int? mode = 0)
    {
        var tmp1 = dbContext.Masters.Include(x => x.MasterContentSnapshots).ToList();
        var tmp2 = dbContext.Masters.Include(x => x.MasterContentSnapshots).Select(x =>
            x.MasterContentSnapshots.Where(i => i.Status >= status)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefault()).ToList();

        var snapshots = dbContext.Masters.Include(x => x.MasterContentSnapshots)
            .Select(x =>
                x.MasterContentSnapshots.Where(i => i.Status >= status)
                    .OrderByDescending(i => i.CreatedAt)
                    .FirstOrDefault())
            .Where(x => x != null)
            .Cast<MasterContentSnapshot>().ToList();

        return mode switch
        {
            0 => Ok(snapshots.Select(x => new MasterSnapshotDto
            {
                Name = x.Master.Name, BaseId = x.BaseId, UniqueId = x.UniqueId,
                UniqueIdHistory = x.Master.MasterContentSnapshots.Select(m => m.UniqueId).ToArray()
            })),
            1 => Ok(snapshots),
            _ => BadRequest()
        };
    }


    /// <summary>
    ///     批量更新属于某一个stencil snapshot的master状态。
    /// </summary>
    /// <param name="stencilSnapshotId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPatch("snapshots")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PatchMasterSnapshots([FromQuery] int stencilSnapshotId,
        [FromBody] SnapshotStatus status = SnapshotStatus.Published)
    {
        var stencilSnapshot = dbContext.StencilSnapshots.Find(stencilSnapshotId);
        if (stencilSnapshot == null) return BadRequest();

        dbContext.Entry(stencilSnapshot).Collection(x => x.MasterContentSnapshots).Load();
        var count = stencilSnapshot.MasterContentSnapshots.Count(x => x.Status != status);
        if (count == 0) return Ok();

        foreach (var snapshot in stencilSnapshot.MasterContentSnapshots)
        {
            snapshot.Status = status;
            snapshot.ModifiedAt = DateTime.Now;
        }

        dbContext.Update(stencilSnapshot);
        dbContext.SaveChanges();

        dbContext.Entry(stencilSnapshot).Reference(x => x.Stencil).Load();
        logger.LogInformation(
            "Status updated. Target: {StencilName}.{SnapshotId}, Count: {Count}, Current: {CurrentValue}",
            stencilSnapshot.Stencil.Name, stencilSnapshotId, count, status);

        return Ok(stencilSnapshot.MasterContentSnapshots);
    }

    /// <summary>
    ///     更新Master的状态。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPatch("snapshots/{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PatchMasterSnapshot([FromRoute] int id,
        [FromBody] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshot = dbContext.MasterContentSnapshots.Find(id);
        if (snapshot == null) return BadRequest();

        if (snapshot.Status == status) return Ok(snapshot);

        var previousStatus = snapshot.Status;
        snapshot.Status = status;
        snapshot.ModifiedAt = DateTime.Now;
        dbContext.Update(snapshot);
        dbContext.SaveChanges();

        dbContext.Entry(snapshot).Reference(x => x.Master).Load();
        logger.LogInformation(
            "Status updated. Target: {MasterName}.{MasterSnapshotId}, Previous: {PreviousValue}, Current: {CurrentValue}",
            snapshot.Master.Name, id, previousStatus,
            snapshot.Status);

        return Ok(snapshot);
    }
}