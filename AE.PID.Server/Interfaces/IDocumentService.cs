using System.IO.Packaging;
using System.Xml.Linq;
using AE.PID.Server.Data;
using AE.PID.Server.Exceptions;

namespace AE.PID.Server.Interfaces;

public interface IDocumentService
{
    /// <summary>
    ///     构建更新/visio/masters/masters.xml文件。
    /// </summary>
    /// <param name="source">原文件</param>
    /// <param name="snapshot">想要变更的新内容</param>
    /// <param name="baseId">变更项的ID</param>
    /// <returns></returns>
    XDocument BuildMastersDocument(XDocument source, XElement snapshot, string baseId);

    /// <summary>
    ///     构建用来更新/visio/masters/master{i}.xml的文件。
    /// </summary>
    /// <param name="snapshot">想要变更的新内容</param>
    /// <param name="lineStyle"></param>
    /// <param name="fillStyle"></param>
    /// <param name="textStyle"></param>
    /// <returns></returns>
    XDocument BuildMasterDocument(XDocument snapshot, int? lineStyle, int? fillStyle, int? textStyle);

    /// <summary>
    ///     构建用来更新/visio/pages/page{i}.xml的文件。
    /// </summary>
    /// <param name="source">原文件</param>
    /// <param name="snapshot">想要变更的新内容</param>
    /// <param name="masterId">变更项的ID</param>
    /// <returns></returns>
    XDocument BuildPageDocument(XDocument source, XDocument snapshot, string masterId);

    /// <summary>
    ///     更新文档模具
    /// </summary>
    /// <param name="package"></param>
    /// <param name="snapshot"></param>
    void UpdateMaster(Package package, MasterContentSnapshot snapshot);
    
    /// <summary>
    /// 更新文档样式
    /// </summary>
    /// <param name="package"></param>
    void UpdateStyles(Package package);

    /// <summary>
    /// Validate if there is no duplicated master base id.
    /// </summary>
    /// <param name="visioPackage"></param>
    /// <param name="baseIds"></param>
    /// <exception cref="MasterBaseIdNotUniqueException"></exception>
    void ValidateMasterBaseIdUnique(Package visioPackage, IEnumerable<string> baseIds);
}