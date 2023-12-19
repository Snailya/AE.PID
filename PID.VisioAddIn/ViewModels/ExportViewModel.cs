using System.Reactive;
using AE.PID.Controllers.Services;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ExportViewModel : ReactiveObject
{
    private string _customerName = string.Empty;
    private string _documentNo = string.Empty;
    private string _projectNo = string.Empty;
    private string _versionNo = string.Empty;

    public ExportViewModel()
    {
        Submit = ReactiveCommand.Create(() =>
        {
            Exporter.SaveAsBom(Globals.ThisAddIn.Application.ActivePage, _customerName, _documentNo, _projectNo,
                _versionNo);
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