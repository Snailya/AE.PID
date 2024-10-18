using System.IO.Packaging;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Server.Data;
using AE.PID.Server.Exceptions;
using AE.PID.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class JobController(ILogger<JobController> logger, AppDbContext dbContext) : ControllerBase
{
    private static string TmpFolder => "/opt/pid/data/tmp";

    [HttpGet("")]
    public IActionResult CheckUpdate([FromQuery] int versionId, [FromQuery] bool involvePrerelease = false)
    {
        var needUpdate = false;

        var record = dbContext.RepositorySnapshots.SingleOrDefault(x => x.Id == versionId);
        // if the hash is not recognized by the server, need do an update
        if (record == null)
        {
            needUpdate = true;
        }
        else // if it is a valid hash, compared with the latest version
        {
            var latestRecord = GetLatestVersion(involvePrerelease);

            if (latestRecord != null && latestRecord.Id > record.Id)
                needUpdate = true;
        }

        return Ok(needUpdate);
    }

    private RepositorySnapshot? GetLatestVersion(bool involvePrerelease)
    {
        var latestRecord = involvePrerelease
            ? dbContext.RepositorySnapshots.Where(x => x.Versions.All(i => i.IsReleased)).MaxBy(x => x.Id)
            : dbContext.RepositorySnapshots.MaxBy(x => x.Id);
        return latestRecord;
    }

    [HttpPost("update-masters")]
    public async Task<IActionResult> Update()
    {
        logger.LogInformation("Server side update");

        // save the byte array as a local file
        byte[] buffer;
        using (var memoryStream = new MemoryStream())
        {
            await Request.Body.CopyToAsync(memoryStream);
            buffer = memoryStream.ToArray();
        }

        var fileName = Path.Combine(TmpFolder, Path.GetRandomFileName());
        await System.IO.File.WriteAllBytesAsync(fileName, buffer);

        logger.LogInformation("File cached at {Path}.", fileName);

        // do the update
        try
        {
            Update(fileName);
            return new PhysicalFileResult(fileName, "application/octet-stream");
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    private void Update(string fileName, bool involvePrerelease = false)
    {
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

            // get the latest version's masters from the database
            var version = GetLatestVersion(involvePrerelease)!;
            var dbMasters = version.Versions.SelectMany(x => x.LibraryVersionItems);

            // filter out masters need to be updated
            var mastersNeedUpdate = masterElements
                .Join(dbMasters, element => element.Attribute("BaseID")!.Value, item => item.BaseId,
                    (element, libraryItem) => new { Element = element, LibraryItem = libraryItem }).Where(x =>
                    x.Element.Attribute("UniqueID")!.Value != x.LibraryItem.UniqueId).ToArray();
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
                var newMasterElement = XElement.Parse(item.LibraryItem.LibraryVersionItemXML.MasterElement);
                newMasterElement.Attribute("ID")!.SetValue(oldId);

                // build up the new master node's Rel node
                var relElement = mastersDocument.XPathSelectElement(
                    $"//main:Master[@BaseID='{item.LibraryItem.BaseId}']/main:Rel",
                    VisioXmlWrapper.NamespaceManager)!;
                newMasterElement.Descendants(VisioXmlWrapper.MainNs + "Rel").First().ReplaceWith(relElement);

                // replace the original master
                item.Element.ReplaceWith(newMasterElement);

                // Next step is to replace the master part with the database one
                // we should replace the LineStyle, FillStyle, TextStyle of the Shape node with correct id in style tables.
                var oldMasterPart = VisioXmlWrapper.GetMasterPartByMasterId(visioPackage, int.Parse(oldId))!;
                var newMasterDocument = XDocument.Parse(item.LibraryItem.LibraryVersionItemXML.MasterDocument);
                foreach (var shapeElement in newMasterDocument.XPathSelectElements("//main:Shape",
                             VisioXmlWrapper.NamespaceManager))
                {
                    var lineStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.LibraryItem.LibraryVersionItemXML.LineStyleName)?.Id;
                    if (lineStyleId != null)
                        shapeElement.Attribute("LineStyle")?.SetValue(lineStyleId);
                    var fillStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.LibraryItem.LibraryVersionItemXML.FillStyleName)?.Id;
                    if (fillStyleId != null)
                        shapeElement.Attribute("FillStyle")?.SetValue(fillStyleId);
                    var textStyleId = currentStyleTables
                        .SingleOrDefault(x => x.Name == item.LibraryItem.LibraryVersionItemXML.TextStyleName)?.Id;
                    if (textStyleId != null)
                        shapeElement.Attribute("TextStyle")?.SetValue(textStyleId);
                }

                // save the new master part
                XmlHelper.SaveXDocumentToPart(oldMasterPart, newMasterDocument);
            }

            // save the new masters part
            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            // update version xml
            SolutionXmlHelper.UpdateVersion(visioPackage, version.Id);

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

    #region -- API Version 3.0 --

    #endregion
}