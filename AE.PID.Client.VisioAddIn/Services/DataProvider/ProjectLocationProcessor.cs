using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public class ProjectLocationProcessor
{
    private readonly Subject<ProjectLocation> _updater = new();

    public ProjectLocationProcessor(VisioDocumentService docService)
    {
        var document = docService.GetDocument();

        var currentProject = new ProjectLocation(new VisioDocumentId(document.ID),
            document.DocumentSheet.TryGetValue<int>(CellDict.ProjectId));

        // 2025.02.07:必须保存 DocumentSheet 的引用，否则过程中UI可能会丢失更新
        // 原因：Observable.FromEvent 依赖 documentSheet 的存在，如果 documentSheet 被回收，事件触发会导致异常。
        // 解决：通过 _documentSheet 字段保持强引用。
        var documentSheet = document.DocumentSheet;
        ProjectLocation = Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                handler => documentSheet.CellChanged += handler,
                handler => documentSheet.CellChanged -= handler)
            .Where(x => x.LocalName == CellDict.ProjectId)
            .Select(x => new ProjectLocation(new VisioDocumentId(document.ID),
                document.DocumentSheet.TryGetValue<int>(CellDict.ProjectId)))
            .StartWith(currentProject);

        _updater
            .Subscribe(location =>
            {
                docService.UpdateProperties([
                    new PropertyPatch(location.Id, CellDict.ProjectId, location.ProjectId ?? 0, true)
                ]);
            });
    }

    public IObservable<ProjectLocation> ProjectLocation { get; }

    public void Update(ProjectLocation location)
    {
        _updater.OnNext(location);
    }
}