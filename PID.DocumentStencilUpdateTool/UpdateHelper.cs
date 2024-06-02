using System.IO.Packaging;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core.DTOs;
using AE.PID.Core.Tools;

namespace PID.DocumentStencilUpdateTool;

public static class UpdateHelper
{
    public const string DefaultReferencePath = @".cheatsheet";

    private static readonly Regex SubClassFormulaRegex =
        new(@"INDEX\((\d+),Prop\.SubClass\.Format\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    ///     Create a backup file for current file with .bak extension
    /// </summary>
    /// <param name="file"></param>
    public static FileInfo CreateBackup(FileInfo file)
    {
        var backup = file.CopyTo(Path.ChangeExtension(file.FullName, "bak"), true);
        Console.WriteLine($"Backup file created at {backup.FullName}.");

        return backup;
    }

    /// <summary>
    ///     If there's more than one masters of the same baseId, replace the shapes in the pages to a single master.
    ///     If not, the masters after update will have the same unique id but different name, which leads to unexpected end
    ///     file when using copy and paste shapes.
    /// </summary>
    public static void SupplementSubClassFormula(Package package)
    {
        try
        {
            var count = 0;

            var mastersPart = package.GetPart(VisioXmlWrapper.MastersPartUri);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);

            foreach (var masterElement in mastersDocument.Descendants(VisioXmlWrapper.MainNs + "Master"))
            {
                var masterId = masterElement.Attribute("ID")!.Value;

                var masterRelId = VisioXmlWrapper.GetRelId(masterElement)!;
                var masterPart = VisioXmlWrapper.GetRelPart(mastersPart, masterRelId);

                var masterDocument = XmlHelper.GetDocumentFromPart(masterPart);

                // get the subclass options;
                var subClassElement = masterDocument.XPathSelectElement(
                    "//main:Section[@N='Property']/main:Row[@N='SubClass']/main:Cell[@N='Type' and @V='1']/preceding-sibling::main:Cell[@N='Format']",
                    VisioXmlWrapper.NamespaceManager);
                if (subClassElement == null) continue;

                var options = subClassElement.Attribute("F")!.Value.Replace("GUARD(\"", "")
                    .Replace("\")", "").Split(';').ToList();

                // get all shapes from page
                var pagesPart = package.GetPart(VisioXmlWrapper.PagesPartUri);
                var pagesDocument = XmlHelper.GetDocumentFromPart(pagesPart);

                foreach (var pageElement in pagesDocument.Descendants(VisioXmlWrapper.MainNs + "Page"))
                {
                    var pageRelId = VisioXmlWrapper.GetRelId(pageElement)!;
                    var pagePart = VisioXmlWrapper.GetRelPart(pagesPart, pageRelId);
                    var pageDocument = XmlHelper.GetDocumentFromPart(pagePart);

                    var cells = pageDocument.XPathSelectElements(
                        $"//main:Shape[@Master='{masterId}']/main:Section[@N='Property']/main:Row[@N='SubClass']/main:Cell[@N='Value']",
                        VisioXmlWrapper.NamespaceManager);
                    foreach (var cellElement in cells)
                    {
                        // skip if is already using index
                        if (cellElement.Attribute("F")?.Value is { } formula &&
                            SubClassFormulaRegex.IsMatch(formula)) continue;

                        var value = cellElement.Attribute("V")!.Value;
                        var index = options.IndexOf(value);
                        cellElement.SetAttributeValue("F", $"INDEX({index},Prop.SubClass.Format)");

                        count++;
                    }

                    XmlHelper.SaveXDocumentToPart(pagePart, pageDocument);
                }
            }

            Console.WriteLine($"Supplement {count} subclass formulas.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message, "Failed to supplement subclass formula.");
            throw;
        }
    }

    /// <summary>
    ///     If there's more than one masters of the same baseId, replace the shapes in the pages to a single master.
    ///     If not, the masters after update will have the same unique id but different name, which leads to unexpected end
    ///     file when using copy and paste shapes.
    /// </summary>
    public static void ReplaceDuplicateMasters(Package package)
    {
        try
        {
            var count = 0;

            var mastersPart = VisioXmlWrapper.GetMastersPart(package);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);

            var pagesPart = VisioXmlWrapper.GetPagesPart(package);
            var pages = pagesPart.GetRelationships()
                .Select(x => package.GetPart(PackUriHelper.ResolvePartUri(x.SourceUri, x.TargetUri)))
                .Select(x => new { Part = x, Document = XmlHelper.GetDocumentFromPart(x) }).ToList();

            foreach (var group in mastersDocument.XPathSelectElements("//main:Master", VisioXmlWrapper.NamespaceManager)
                         .GroupBy(x => x.Attribute("BaseID")!.Value).Where(x => x.Count() > 1))
            {
                // treat the first XElement as the correct one
                var prototype = group.First();
                var prototypeId = prototype.Attribute("ID")!.Value;

                // treat the others as duplicates
                foreach (var duplicate in group.Skip(1).ToList())
                {
                    var duplicateId = duplicate.Attribute("ID")!.Value;

                    // to safely delete the master, we should find find out all related shapes in page
                    foreach (var shapeElement in pages.SelectMany(x =>
                                 x.Document.XPathSelectElements($"//main:Shape[@Master='{duplicateId}']",
                                     VisioXmlWrapper.NamespaceManager)))
                        shapeElement.Attribute("Master")!.SetValue(prototypeId);

                    // after replace the related shape's ID with prototypeId, it is disconnect from the duplicate master, so we could delete this duplicate master
                    // first is to delete the relationship, then remove the element
                    var relId = VisioXmlWrapper.GetRelId(duplicate)!;
                    mastersPart.DeleteRelationship(relId);

                    duplicate.Remove();

                    count++;
                }
            }

            // persist page change
            foreach (var page in pages)
                XmlHelper.SaveXDocumentToPart(page.Part, page.Document);

            // persist master change
            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            Console.WriteLine($"Removed {count} duplicated masters.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message, "Failed to remove duplicate masters.");
            throw;
        }
    }

    /// <summary>
    ///     Load masters from cheat sheet, this cheat sheet is updated along with library updates.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<DetailedLibraryItemDto> LoadReferenceFromPath(FileInfo file)
    {
        try
        {
            var fileContents = File.ReadAllText(file.FullName);
            var masters = JsonSerializer.Deserialize<IEnumerable<DetailedLibraryItemDto>>(fileContents);
            return masters ?? throw new Exception("No contents found in cheat sheet.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task<IEnumerable<DetailedLibraryItemDto>> LoadReferenceFromServer()
    {
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://172.18.128.104:32768");

            var responseString = await client.GetStringAsync("/libraries/cheatsheet");

            if (string.IsNullOrEmpty(responseString)) return [];

            using var writer = new FileInfo(DefaultReferencePath).CreateText();
            await writer.WriteLineAsync(responseString);

            Console.WriteLine($"Reference saved at {DefaultReferencePath}");


            return JsonSerializer.Deserialize<IEnumerable<DetailedLibraryItemDto>>(responseString) ?? [];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static IEnumerable<DetailedLibraryItemDto> LoadReferenceFromDocument(FileInfo file)
    {
        try
        {
            using var package = Package.Open(file.FullName, FileMode.Open, FileAccess.Read);
            var mastersPart = VisioXmlWrapper.GetMastersPart(package);

            var styles = VisioXmlWrapper.GetStyles(package).ToList();

            // Loop through masters part to get
            using var partXmlReader = XmlReader.Create(mastersPart.GetStream());
            var root = XElement.Load(partXmlReader);

            return (from masterElement in root.Elements()
                let masterDocument =
                    XmlHelper.GetDocumentFromPart(
                        VisioXmlWrapper.GetMasterPartById(package, int.Parse(masterElement.Attribute("ID")!.Value))!)
                let shapeElement = masterDocument.XPathSelectElement("/main:MasterContents/main:Shapes/main:Shape",
                    VisioXmlWrapper.NamespaceManager)
                select new DetailedLibraryItemDto
                {
                    BaseId = masterElement.Attribute("BaseID")!.Value,
                    UniqueId = masterElement.Attribute("UniqueID")!.Value,
                    Name = masterElement.Attribute("Name")!.Value,
                    LineStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("LineStyle")!.Value))
                        .Name,
                    FillStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("FillStyle")!.Value))
                        .Name,
                    TextStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("TextStyle")!.Value))
                        .Name,
                    MasterElement = masterElement.ToString(SaveOptions.DisableFormatting),
                    MasterDocument = masterDocument.ToString(SaveOptions.DisableFormatting)
                }).ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    ///     Replace the master and report progress.
    /// </summary>
    /// <param name="package"></param>
    /// <param name="refMasters"></param>
    public static void ReplaceMasterElementAndMasterContent(Package package,
        IEnumerable<DetailedLibraryItemDto> refMasters)
    {
        try
        {
            var count = 0;

            // get style sheets from current document
            var styleTable = VisioXmlWrapper.GetStyles(package).ToList();

            var mastersPart = VisioXmlWrapper.GetMastersPart(package);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);

            foreach (var refMaster in refMasters)
            {
                // skip the master if there's error in cheatsheet
                if (string.IsNullOrEmpty(refMaster.MasterElement) ||
                    string.IsNullOrEmpty(refMaster.MasterDocument)) continue;

                // skip the master if not used by current document
                var oldMasterElement = mastersDocument.XPathSelectElement(
                    $"//main:Master[@BaseID='{refMaster.BaseId}']",
                    VisioXmlWrapper.NamespaceManager);
                if (oldMasterElement == null) continue;

                // skip the master if the same
                var masterElement = XElement.Parse(refMaster.MasterElement);
                if (oldMasterElement.Attribute("UniqueID")!.Value ==
                    masterElement.Attribute("UniqueID")!.Value) continue;

                var masterDocument = XDocument.Parse(refMaster.MasterDocument);

                // to build new master element,
                // we should replace the ID attribute of Master node with the origin one,
                // and the Rel node with the origin one
                var id = oldMasterElement.Attribute("ID")!.Value;
                masterElement.Attribute("ID")!.SetValue(id);

                var relElement = mastersDocument.XPathSelectElement(
                    $"//main:Master[@BaseID='{refMaster.BaseId}']/main:Rel",
                    VisioXmlWrapper.NamespaceManager)!;
                masterElement.Descendants(VisioXmlWrapper.MainNs + "Rel").First().ReplaceWith(relElement);

                oldMasterElement.ReplaceWith(masterElement);

                // then handle master{i} part
                // we should replace the LineStyle, FillStyle, TextStyle of the Shape node with correct id in style tables.
                var oldMasterPart = VisioXmlWrapper.GetMasterPartById(package, int.Parse(id))!;
                foreach (var shapeElement in masterDocument.XPathSelectElements("//main:Shape",
                             VisioXmlWrapper.NamespaceManager))
                {
                    var lineStyleId = styleTable.SingleOrDefault(x => x.Name == refMaster.LineStyleName)?.Id;
                    if (lineStyleId != null)
                        shapeElement.Attribute("LineStyle")?.SetValue(lineStyleId);
                    var fillStyleId = styleTable.SingleOrDefault(x => x.Name == refMaster.FillStyleName)?.Id;
                    if (fillStyleId != null)
                        shapeElement.Attribute("FillStyle")?.SetValue(fillStyleId);
                    var textStyleId = styleTable.SingleOrDefault(x => x.Name == refMaster.TextStyleName)?.Id;
                    if (textStyleId != null)
                        shapeElement.Attribute("TextStyle")?.SetValue(textStyleId);
                }

                XmlHelper.SaveXDocumentToPart(oldMasterPart, masterDocument);

                count++;
            }

            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            // recalculate formula in shape sheet
            XmlHelper.RecalculateDocument(package);

            Console.WriteLine($"Replaced {count} masters by reference.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message, "Failed to replace masters.");
            throw;
        }
    }
}