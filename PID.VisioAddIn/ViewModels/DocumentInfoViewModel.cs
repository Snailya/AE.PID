using AE.PID.Models;
using ReactiveUI;
using System;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.ViewModels;

public class DocumentInfoViewModel : ViewModelBase
{
    private string _customerName;
    private string _documentNo;
    private string _projectNo;
    private string _versionNo;

    public DocumentInfoViewModel(IVPage page)
    {
        //_documentNo = GetValueIfExist(page.PageSheet, "User.DocumentNo") ?? Guid.NewGuid().ToString();
        //page.PageSheet.Cells["User.DocumentNo"].Formula = _documentNo;

        Load();
    }

    public string CustomerName
    {
        get => _customerName;
        private set => this.RaiseAndSetIfChanged(ref _customerName, value);
    }

    public string DocumentNo
    {
        get => _documentNo;
        private set => this.RaiseAndSetIfChanged(ref _documentNo, value);
    }

    public string ProjectNo
    {
        get => _projectNo;
        private set => this.RaiseAndSetIfChanged(ref _projectNo, value);
    }

    public string VersionNo
    {
        get => _versionNo;
        private set => this.RaiseAndSetIfChanged(ref _versionNo, value);
    }


    public void Load()
    {
        CustomerName = Globals.ThisAddIn.InputCache.CustomerName;
        DocumentNo = Globals.ThisAddIn.InputCache.DocumentNo;


        ProjectNo = Globals.ThisAddIn.InputCache.ProjectNo;
        VersionNo = Globals.ThisAddIn.InputCache.VersionNo;
    }

    // todo: remove, if the project is selected from database, no need to cache there in cache, but in page sheet
    public void Cache()
    {
        Globals.ThisAddIn.InputCache.CustomerName = _customerName;
        Globals.ThisAddIn.InputCache.DocumentNo = _documentNo;
        Globals.ThisAddIn.InputCache.ProjectNo = _projectNo;
        Globals.ThisAddIn.InputCache.VersionNo = _versionNo;
        InputCache.Save(Globals.ThisAddIn.InputCache);
    }

    private static string? GetValueIfExist(IVShape shape, string propName)
    {
        string? value = null;
        if (shape.CellExists[propName, (short)VisExistsFlags.visExistsLocally] == (short)VBABool.True) return value;

        var valueFromShape = shape.Cells[propName].ResultStr[VisUnitCodes.visUnitsString];
        if (!string.IsNullOrEmpty(valueFromShape)) value = valueFromShape;

        return value;
    }
}