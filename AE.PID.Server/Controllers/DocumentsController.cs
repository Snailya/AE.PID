using System.IO.Packaging;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Server.Data;
using AE.PID.Server.Exceptions;
using AE.PID.Server.Interfaces;
using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class DocumentsController(
    ILogger<DocumentsController> logger,
    AppDbContext dbContext,
    IDocumentService documentService)
    : ControllerBase
{
    /// <summary>
    ///     更新文档模具。
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromQuery] SnapshotStatus status = SnapshotStatus.Published)
    {
        logger.LogInformation("Server side update v2");

        // save the byte array as a local file
        byte[] buffer;
        using (var memoryStream = new MemoryStream())
        {
            await Request.Body.CopyToAsync(memoryStream);
            buffer = memoryStream.ToArray();
        }

        var fileName = Path.Combine(Constants.TmpPath, Path.GetRandomFileName());
        await System.IO.File.WriteAllBytesAsync(fileName, buffer);

        logger.LogInformation("File cached at {Path}.", fileName);

        // do the update
        try
        {
            // first validate the document
            using var visioPackage = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
            var snapshots = GetMasterSnapshot(status);
            documentService.ValidateMasterBaseIdUnique(visioPackage, snapshots.Select(x=>x.BaseId));

            // do update
            foreach (var snapshot in snapshots) documentService.Update(visioPackage, snapshot);

            logger.LogInformation("File updated without error.");

            // return physical file
            return new PhysicalFileResult(fileName, "application/octet-stream");
        }
        catch (Exception e)
        {
            logger.LogError(e,"File updated failed with error.");
            
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    ///     更新文档模具。
    /// </summary>
    /// <param name="request"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPost("update/file")]
    public async Task<IActionResult> Update(FileUploadRequest request,
        [FromQuery] SnapshotStatus status = SnapshotStatus.Published)
    {
        // Save the uploaded file to a folder
        var fileName = Path.Combine(Constants.TmpPath, Path.GetFileName(request.File.FileName));

        using (var stream = new FileStream(fileName, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        try
        {
            // first validate the document
            using var visioPackage = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
            var snapshots = GetMasterSnapshot(status);
            documentService.ValidateMasterBaseIdUnique(visioPackage,snapshots.Select(x=>x.BaseId));

            // do update
            foreach (var snapshot in snapshots) documentService.Update(visioPackage, snapshot);

            // return physical file
            return PhysicalFile(fileName, "application/octet-stream", request.File.FileName, true);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private List<MasterContentSnapshot> GetMasterSnapshot(
        SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshots = dbContext.Masters.Include(x => x.MasterContentSnapshots).Select(x =>
            x.MasterContentSnapshots.Where(i => i.Status >= status).OrderByDescending(i => i.CreatedAt)
                .FirstOrDefault()).Where(x => x != null).Cast<MasterContentSnapshot>().ToList();
        return snapshots;
    }


    private void Update(string fileName, IEnumerable<MasterContentSnapshot> snapshots)
    {
        /* 使用OpenXML更新文档模具可能会涉及到以下几个文件内容的更新
         * --- /visio/masters.xml ---
         *      masters.xml中每个<Master/>节点对应文档模具（Document Stencil）中的主控形状（Master）。
         *      <Master />中包含了右键菜单项编辑模具（Edit Master）可以定义的全部属性，常用属性有页面的宽度、形状的关键词、图标等。
         *      此外<Rel/>节点记录了与此<Master/>节点关联的master{i}.xml的关系Id。
         *      更新模具时，需要将Master节点的内容替换为最新的MasterElement，并主要保证ID不变。
         * --------------------------
         * - /visio/master{i}.xml
         *      master{i}.xml中记录了与模具相关的形状的定义，包括形状携带的属性，例如User Section Data, Shape Data等。
         *      需要注意的是<Shape LineStyle='' FillStyle='' TextStyle=''/>对于样式的定义使用的是ID，但是不同文档中由于样式的定义顺序不同，即使同一个样式ID也会不同，所以更新时除了将此文件替换为最新的文件外，还需要更新样式的值。
         *      样式ID与样式名称的对应关系位于/visio/document.xml中的<StyleSheet />节点
         * --------------------------
         * - /visio/pages/page{i}.xml
         *      page{i}.xml中的<Shapes />的每个子节点<Shape>对应绘图页上的一个形状，如果该形状是对于主控形状的引用，则被引用的主控形状体现为<Shape Master=''/>。由于对于masters.xml的更新可能会使<Master />的ID发生变化，在更新page{i}.xml时，应保证该值的正确性。
         *      如果主控形状只有属性与之前版本不同，通常无需修改除<Shape Master=''/>以外的内容，因为这里存储的都是被用户自定义的值。 （此处不太确定新增自动计算的属性时，再更新后是否会自动计算该值）。
         *      如果主控形状内的子形状发生了新增或删除，必须在对应的<Shape><Shapes /></Shape>中对应新增或删除子形状，否则会报错。例如
         * 		<Shape ID='1' NameU='泵.22251' IsCustomNameU='1' Name='泵.22251' IsCustomName='1' Type='Group' Master='2'>
         *          <Shapes>
         *              +++ <Shape ID='7' NameU='VSD' IsCustomNameU='1' Name='VSD' IsCustomName='1' Type='Shape' MasterShape='514'>
         *              +++     <Cell N='LayerMember' V='0;1'/>
         *              +++ </Shape>
         *          </Shapes>
         *      </Shape>
         *      但是这种做法只能保证不报错，当字形状中包含自动计算的属性值时，由于没有添加该值，这个新的属性不会被显示。暂时没找到可以用于快速生成需要被添加节点的方法，最好不要修改子形状。
         * --------------------------
         * - /docProps/custom.xml
         *      添加重新计算文档公式，但是好像对字形状的公式不起作用。
         *
         */

        // open the local file as package
        using var visioPackage = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite);

        try
        {
            // get style sheets from current document
            var currentStyleTables = VisioXmlWrapper.GetStyles(visioPackage).ToList();
            logger.LogInformation("Style tables got");

            // take a look at the masters part to see how many masters are there
            var mastersPart = VisioXmlWrapper.GetMastersPart(visioPackage);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);
            var masterElements = mastersDocument.XPathSelectElements("//main:Master", VisioXmlWrapper.NamespaceManager);
            logger.LogInformation(
                "There are {Count} in the document", masterElements.Count());

            // filter out masters need to be updated
            var mastersNeedUpdate = masterElements
                .Join(snapshots,
                    xElement => xElement.Attribute("BaseID")!.Value,
                    snapshot => snapshot.BaseId,
                    (xElement, masterContentSnapshot) =>
                        new { Element = xElement, ContentSnapshot = masterContentSnapshot })
                .Where(x => x.Element.Attribute("UniqueID")!.Value != x.ContentSnapshot.UniqueId)
                .ToArray();
            logger.LogInformation(
                "There are {Count} out of {Total} masters need to be update.", mastersNeedUpdate.Length,
                masterElements.Count());

            foreach (var item in mastersNeedUpdate)
            {
                // to build a new master node,
                // we should replace the ID attribute of Master node with the origin one,
                // and the Rel node with the origin one

                // build the new master node by replace the ID attribute as the old one
                var oldId = item.Element.Attribute("ID")!.Value;
                var newMasterElement = XElement.Parse(item.ContentSnapshot.MasterElement);
                newMasterElement.Attribute("ID")!.SetValue(oldId);

                // build up the new master node's Rel node
                var relElement = mastersDocument.XPathSelectElement(
                    $"//main:Master[@BaseID='{item.ContentSnapshot.BaseId}']/main:Rel",
                    VisioXmlWrapper.NamespaceManager)!;
                newMasterElement.Descendants(VisioXmlWrapper.MainNs + "Rel").First().ReplaceWith(relElement);

                // replace the original master
                item.Element.ReplaceWith(newMasterElement);

                // Next step is to replace the master part with the database one
                // we should replace the LineStyle, FillStyle, TextStyle of the Shape node with correct id in style tables.
                var oldMasterPart = VisioXmlWrapper.GetMasterPartByMasterId(visioPackage, int.Parse(oldId))!;
                var newMasterDocument = XDocument.Parse(item.ContentSnapshot.MasterDocument);
                foreach (var shapeElement in newMasterDocument.XPathSelectElements("//main:Shape",
                             VisioXmlWrapper.NamespaceManager))
                {
                    var lineStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.ContentSnapshot.LineStyleName)?.Id;
                    if (lineStyleId != null)
                        shapeElement.Attribute("LineStyle")?.SetValue(lineStyleId);
                    var fillStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.ContentSnapshot.FillStyleName)?.Id;
                    if (fillStyleId != null)
                        shapeElement.Attribute("FillStyle")?.SetValue(fillStyleId);
                    var textStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.ContentSnapshot.TextStyleName)?.Id;
                    if (textStyleId != null)
                        shapeElement.Attribute("TextStyle")?.SetValue(textStyleId);
                }

                // save the new master part
                XmlHelper.SaveXDocumentToPart(oldMasterPart, newMasterDocument);
            }

            // update page


            // save the new masters part
            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            // update version xml
            // SolutionXmlHelper.UpdateVersion(visioPackage, version.Id);

            // recalculate formula in shape sheet
            XmlHelper.RecalculateDocument(visioPackage);

            logger.LogInformation("Update done.");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to update document: {Reason}", e.Message);
            throw new DocumentUpdateFailedException();
        }
    }
}

public class FileUploadRequest
{
    public IFormFile File { get; set; }
}