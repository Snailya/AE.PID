using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core.DTOs;
using AE.PID.Core.Tools;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Newtonsoft.Json;
using NLog;
using Path = System.IO.Path;

namespace AE.PID.Controllers.Services;

/// <summary>
///     Compare document stencil with library ones and do updates for the document to keep stencil in time.
/// </summary>
public class DocumentUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _filePath;

    /// <summary>
    ///     Compare document stencil with library ones and do updates for the document to keep stencil in time.
    /// </summary>
    public DocumentUpdater(string filePath)
    {
        _filePath = filePath;
    }

    private static Subject<IVDocument> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="document"></param>
    public static void Invoke(IVDocument document)
    {
        ManuallyInvokeTrigger.OnNext(document);
    }

    /// <summary>
    ///     Listen to both document open event and user click event to monitor if a document master update is needed.
    ///     The update process is done on a background thread using OpenXML, so it is extremely fast.
    ///     However, a progress bar still provided in case a long time run needed in the future.
    /// </summary>
    public static IDisposable Listen()
    {
        Logger.Info("Document Update Service started.");

        return Observable
            .FromEvent<EApplication_DocumentOpenedEventHandler, Document>(
                handler => Globals.ThisAddIn.Application.DocumentOpened += handler,
                handler => Globals.ThisAddIn.Application.DocumentOpened -= handler)
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing)
            .Do(_ => Logger.Info("Document Update started. {Initiated by: Document Open Event}"))
            // manually invoke from ribbon
            .Merge(
                ManuallyInvokeTrigger
                    .Throttle(TimeSpan.FromMilliseconds(300))
                    .Do(_ => Logger.Info("Document Update started. {Initiated by: User}"))
            ).Subscribe(
                document =>
                {
                    Observable.Return(document)
                        .SubscribeOn(TaskPoolScheduler.Default)
                        .SelectMany(data => Task.Run(() => NeedUpdate(data)),
                            (data, needUpdate) => new { Document = data, NeedUpdate = needUpdate })
                        .Where(data => data.NeedUpdate)
                        // prompt user decision
                        .Select(result => new
                            { Info = result, DialogResult = ThisAddIn.AskForUpdate("检测到文档模具与库中模具不一致，是否立即更新文档模具？") })
                        .Where(x => x.DialogResult == DialogResult.Yes)
                        .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
                        // close all document stencils to avoid occupied
                        .Select(x => new { FilePath = CreateCopy(x.Info.Document) })
                        // display a progress bar to do time-consuming operation
                        .Select(data =>
                        {
                            var progress = new Progress<int>();
                            var token = new CancellationTokenSource().Token;

                            var evidence = new Evidence();
                            evidence.AddHostEvidence(new Zone(SecurityZone.MyComputer));

                            var updater = new DocumentUpdater(data.FilePath);
                            updater.DoUpdatesByOpenXml(progress, token);

                            // todo: for large file, there is issue with evidence

                            // var domain = AppDomain.CreateDomain("Test", new Evidence());
                            // var updater =  (IOpenXmlService)domain.CreateInstanceAndUnwrap(typeof(OpenXmlService).Assembly.FullName, typeof(OpenXmlService).FullName, new object[]{data.FilePath});
                            // updater.DoUpdates(progress, token);
                            return data.FilePath;
                        })
                        .Subscribe(
                            OpenCopy,
                            ex => { ThisAddIn.Alert(ex.Message); }
                        );
                },
                ex => { Logger.Error(ex, "Document Update Service ternimated accidently."); },
                () => { Logger.Error("Document Update Service should never complete."); });
    }

    /// <summary>
    ///     Compare the document stencil with library stuffs.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static bool NeedUpdate(IVDocument document)
    {
        var configuration = Globals.ThisAddIn.Configuration;

        foreach (var source in document.Masters.OfType<IVMaster>().ToList())
            if (configuration.LibraryConfiguration.GetItems()
                    .SingleOrDefault(x => x.BaseId == source.BaseID) is { } item
                && item.UniqueId != source.UniqueID)
                return true;

        return false;
    }

    /// <summary>
    ///     Close all opened document in docked window to prevent document busy, copy the source file to a temporary path.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static string CreateCopy(IVDocument document)
    {
        // create a copy of source file
        var copied = Path.Combine(ThisAddIn.TmpFolder, Path.ChangeExtension(Path.GetRandomFileName(), "vsdx"));
        File.Copy(document.FullName, copied);

        return copied;
    }

    /// <summary>
    ///     Update a document's document stencil by overwrite the masters.xml and related master{i}.xml file in background
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException"></exception>
    private void DoUpdatesByOpenXml(IProgress<int> progress, CancellationToken token)
    {
        try
        {
            using var package = Package.Open(_filePath, FileMode.Open, FileAccess.ReadWrite);

            // when user using context menu to setup the subclass property, the subclass property value is a string, which will lost if the subclass format changed
            // therefore, replace this string value with a formula basing the index
            SupplementSubClassFormula(package);

            // though the masters is set to match name on drop, it still could not restrict user to use the unique master.
            // by checking the BaseID in the masters, replace the shapes to point to one single master
            ReplaceDuplicateMasters(package);

            ReplaceMasterElementAndMasterContentWithReportProgress(package, progress, token);

            Logger.Info("Document masters updated successfully.");
        }
        catch (Exception e)
        {
            Logger.Info("Failed to updates masters");
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="filePath"></param>
    private static void OpenCopy(string filePath)
    {
        ThisAddIn.Alert("更新成功，请在新文件打开后手动另存。");
        Globals.ThisAddIn.Application.Documents.OpenEx(filePath,
            (short)VisOpenSaveArgs.visOpenRW);
    }

    /// <summary>
    ///     Replace the master and report progress.
    /// </summary>
    /// <param name="package"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    private static void ReplaceMasterElementAndMasterContentWithReportProgress(Package package, IProgress<int> progress,
        CancellationToken token)
    {
        try
        {
            var masters = LoadMastersFromCheatSheet().ToList();

            // get style sheets from current document
            var styleTable = VisioXmlWrapper.GetStyles(package).ToList();

            var mastersPart = VisioXmlWrapper.GetMastersPart(package);
            var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);

            var processCount = masters.Count;
            var processIndicator = 0;

            foreach (var master in masters)
            {
                if (token.IsCancellationRequested) return;

                // skip the master if there's error in cheatsheet
                if (string.IsNullOrEmpty(master.MasterElement) || string.IsNullOrEmpty(master.MasterDocument)) continue;

                // skip the master if not used by current document
                var oldMasterElement = mastersDocument.XPathSelectElement($"//main:Master[@BaseID='{master.BaseId}']",
                    VisioXmlWrapper.NamespaceManager);
                if (oldMasterElement == null) continue;

                var masterElement = XElement.Parse(master.MasterElement);
                var masterDocument = XDocument.Parse(master.MasterDocument);

                // to build new master element,
                // we should replace the ID attribute of Master node with the origin one,
                // and the Rel node with the origin one
                var id = oldMasterElement.Attribute("ID")!.Value;
                masterElement.Attribute("ID")!.SetValue(id);

                var relElement = mastersDocument.XPathSelectElement(
                    $"//main:Master[@BaseID='{master.BaseId}']/main:Rel",
                    VisioXmlWrapper.NamespaceManager)!;
                masterElement.Descendants(VisioXmlWrapper.MainNs + "Rel").First().ReplaceWith(relElement);

                oldMasterElement.ReplaceWith(masterElement);

                // then handle master{i} part
                // we should replace the LineStyle, FillStyle, TextStyle of the Shape node with correct id in style tables.
                var oldMasterPart = VisioXmlWrapper.GetMasterPartById(package, int.Parse(id))!;
                foreach (var shapeElement in masterDocument.XPathSelectElements("//main:Shape",
                             VisioXmlWrapper.NamespaceManager))
                {
                    var lineStyleId = styleTable.SingleOrDefault(x => x.Name == master.LineStyleName)?.Id;
                    if (lineStyleId != null)
                        shapeElement.Attribute("LineStyle")?.SetValue(lineStyleId);
                    var fillStyleId = styleTable.SingleOrDefault(x => x.Name == master.FillStyleName)?.Id;
                    if (fillStyleId != null)
                        shapeElement.Attribute("FillStyle")?.SetValue(fillStyleId);
                    var textStyleId = styleTable.SingleOrDefault(x => x.Name == master.TextStyleName)?.Id;
                    if (textStyleId != null)
                        shapeElement.Attribute("TextStyle")?.SetValue(textStyleId);
                }

                XmlHelper.SaveXDocumentToPart(oldMasterPart, masterDocument);

                var current = 100 * ++processIndicator / processCount;
                progress.Report(current);
            }

            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

            // recalculate formula in shape sheet
            XmlHelper.RecalculateDocument(package);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to replace masters.");
            throw;
        }
    }

    /// <summary>
    ///     Load masters from cheat sheet, this cheat sheet is updated along with library updates.
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<DetailedLibraryItemDto> LoadMastersFromCheatSheet()
    {
        var fileContents = File.ReadAllText(ThisAddIn.LibraryCheatSheet);
        var masters = JsonConvert.DeserializeObject<IEnumerable<DetailedLibraryItemDto>>(fileContents);
        if (masters == null) throw new Exception("No contents found in cheat sheet.");
        return masters;
    }


    /// <summary>
    ///     If there's more than one masters of the same baseId, replace the shapes in the pages to a single master.
    ///     If not, the masters after update will have the same unique id but different name, which leads to unexpected end
    ///     file when using copy and paste shapes.
    /// </summary>
    private static void ReplaceDuplicateMasters(Package package)
    {
        try
        {
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
                }
            }

            // persist page change
            foreach (var page in pages)
                XmlHelper.SaveXDocumentToPart(page.Part, page.Document);

            // persist master change
            XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to remove duplicate masters.");
            throw;
        }
    }


    /// <summary>
    ///     If there's more than one masters of the same baseId, replace the shapes in the pages to a single master.
    ///     If not, the masters after update will have the same unique id but different name, which leads to unexpected end
    ///     file when using copy and paste shapes.
    /// </summary>
    private static void SupplementSubClassFormula(Package package)
    {
        try
        {
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
                    .Replace("\")", "").Split(';');

                // get all shapes from page
                var pagesPart = package.GetPart(VisioXmlWrapper.PagesPartUri);
                var pagesDocument = XmlHelper.GetDocumentFromPart(pagesPart);

                foreach (var pageElement in pagesDocument.Descendants(VisioXmlWrapper.MainNs + "Page"))
                {
                    var pageRelId = VisioXmlWrapper.GetRelId(pageElement)!;
                    var pagePart = VisioXmlWrapper.GetRelPart(pagesPart, pageRelId);
                    var pageDocument = XmlHelper.GetDocumentFromPart(pagePart);


                    var cells = pageDocument.XPathSelectElements(
                        $"//main:Shape[@Master='{masterId}']/main:Section[@N='Property']/main:Row[@N='SubClass']/main:Cell",
                        VisioXmlWrapper.NamespaceManager);
                    foreach (var cellElement in cells)
                    {
                        var value = cellElement.Attribute("V")!.Value;
                        var index = options.IndexOf(value);
                        cellElement.SetAttributeValue("F", $"INDEX({index},Prop.SubClass.Format)");
                    }

                    XmlHelper.SaveXDocumentToPart(pagePart, pageDocument);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to supplement subclass formula.");
            throw;
        }
    }
}