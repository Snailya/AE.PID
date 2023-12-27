using System.Reactive;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ExportViewModel : ReactiveObject
{
    private string _customerName = Globals.ThisAddIn.InputCache.CustomerName;
    private string _documentNo = Globals.ThisAddIn.InputCache.DocumentNo;
    private string _projectNo = Globals.ThisAddIn.InputCache.ProjectNo;
    private string _versionNo = Globals.ThisAddIn.InputCache.VersionNo;

    public ExportViewModel()
    {
        Submit = ReactiveCommand.Create(() =>
        {
            DocumentExporter.SaveAsBom(Globals.ThisAddIn.Application.ActivePage, _customerName, _documentNo, _projectNo,
                _versionNo);

            Globals.ThisAddIn.InputCache.CustomerName = _customerName;
            Globals.ThisAddIn.InputCache.DocumentNo = _documentNo;
            Globals.ThisAddIn.InputCache.ProjectNo = _projectNo;
            Globals.ThisAddIn.InputCache.VersionNo = _versionNo;
            InputCache.Save(Globals.ThisAddIn.InputCache);
        });
        Cancel = ReactiveCommand.Create(() => { });
    }

    public string CustomerName
    {
        get => _customerName;
        set => this.RaiseAndSetIfChanged(ref _customerName, value);
    }

    public string DocumentNo
    {
        get => _documentNo;
        set => this.RaiseAndSetIfChanged(ref _documentNo, value);
    }

    public string ProjectNo
    {
        get => _projectNo;
        set => this.RaiseAndSetIfChanged(ref _projectNo, value);
    }

    public string VersionNo
    {
        get => _versionNo;
        set => this.RaiseAndSetIfChanged(ref _versionNo, value);
    }

    public ReactiveCommand<Unit, Unit> Submit { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }
}