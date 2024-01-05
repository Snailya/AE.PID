using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Models.BOM;
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
        var lineItems = DocumentExporter.GetLineItems().ToList();
        LineItems = new ObservableCollection<LineItemBase>(lineItems);

        Submit = ReactiveCommand.Create(() =>
        {
            DocumentExporter.SaveAsBom(lineItems, _customerName, _documentNo, _projectNo,
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

    public ObservableCollection<LineItemBase> LineItems { get; set; }
    public ReactiveCommand<Unit, Unit> Submit { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }
}