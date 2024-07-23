using System.IO.Packaging;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core.Tools;
using AE.PID.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class JobController(ILogger<JobController> logger, AppDbContext dbContext) : ControllerBase
{
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

        var fileName = Path.Combine("/opt/pid/data/tmp", Path.GetRandomFileName());
        await System.IO.File.WriteAllBytesAsync(fileName, buffer);

        logger.LogInformation("File cached at {Path}.", fileName);

        // open the local file as package
        using var package = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
        try
        {
            // get style sheets from current document
            var styleTable = VisioXmlWrapper.GetStyles(package).ToList();
            logger.LogInformation("Style tables got");

            // take a look at the masters part to see how many masters are there
            var mastersPart = VisioXmlWrapper.GetMastersPart(package);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);
            var masterElements = mastersDocument.XPathSelectElements("//main:Master",VisioXmlWrapper.NamespaceManager);
            logger.LogInformation(
                "There are {Count} in the document", masterElements.Count());
            
            // get the latest masters in the database
            var libraryItems = Helper.PopulatesCheatSheetItems(dbContext);

            // filter out masters need to be updated
            var mastersNeedUpdate = masterElements
                .Join(libraryItems, element => element.Attribute("BaseID")!.Value, item => item.BaseId,
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
                var newMasterElement = XElement.Parse(item.LibraryItem.MasterElement);
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
                var oldMasterPart = VisioXmlWrapper.GetMasterPartById(package, int.Parse(oldId))!;
                var newMasterDocument = XDocument.Parse(item.LibraryItem.MasterDocument);
                foreach (var shapeElement in newMasterDocument.XPathSelectElements("//main:Shape",
                             VisioXmlWrapper.NamespaceManager))
                {
                    var lineStyleId = styleTable.SingleOrDefault(x => x.Name == item.LibraryItem.LineStyleName)?.Id;
                    if (lineStyleId != null)
                        shapeElement.Attribute("LineStyle")?.SetValue(lineStyleId);
                    var fillStyleId = styleTable.SingleOrDefault(x => x.Name == item.LibraryItem.FillStyleName)?.Id;
                    if (fillStyleId != null)
                        shapeElement.Attribute("FillStyle")?.SetValue(fillStyleId);
                    var textStyleId = styleTable.SingleOrDefault(x => x.Name == item.LibraryItem.TextStyleName)?.Id;
                    if (textStyleId != null)
                        shapeElement.Attribute("TextStyle")?.SetValue(textStyleId);
                }

                // save the new master part
                XmlHelper.SaveXDocumentToPart(oldMasterPart, newMasterDocument);
            }

            // save the new masters part
            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            // recalculate formula in shape sheet
            XmlHelper.RecalculateDocument(package);

            logger.LogInformation("Update done.");

            return new PhysicalFileResult(fileName, "application/octet-stream");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to update document: {Reason}", e.Message);
        }

        return BadRequest();
    }
}