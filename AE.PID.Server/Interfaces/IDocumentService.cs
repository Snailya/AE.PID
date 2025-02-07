using System.IO.Packaging;
using AE.PID.Server.Data;
using AE.PID.Server.Models;

namespace AE.PID.Server;

public interface IDocumentService
{
    /// <summary>
    ///     更新文档样式
    /// </summary>
    /// <param name="package"></param>
    void UpdateStyles(Package package);

    /// <summary>
    ///     更新模具
    /// </summary>
    /// <param name="package"></param>
    /// <param name="uniqueId"></param>
    /// <param name="snapshot"></param>
    void UpdateMaster(Package package, string uniqueId, MasterContentSnapshot snapshot);

    /// <summary>
    ///     获取文档模具列表
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    VisioMaster[] GetDocumentMasters(Package package);
}