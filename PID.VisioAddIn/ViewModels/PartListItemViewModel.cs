using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using AE.PID.Models.BOM;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class PartListItemViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();

    public  Element Source { get; set; }
    private string _functionalElement;

    private string _functionalGroup;
    private int _index;

    public PartListItemViewModel(PartItem element)
    {
        Contract.Assert(element is Equipment or Models.BOM.FunctionalElement,
            "Only Equipment and FunctionalElement can be convert into PartListItem");

        Source = element;
        _functionalElement = Source.Designation;

        if (Source is Equipment equipment)
            this.WhenAnyValue(x => x.Source.ParentId)
                .Subscribe(value => { FunctionalGroup = equipment.GetFunctionalGroup(); }).DisposeWith(_cleanUp);
        else if (Source is FunctionalElement fe)
            this.WhenAnyValue(x => x.Source.ParentId)
                .Subscribe(value =>
                {
                    FunctionalGroup = fe.GetFunctionalGroup();

                    FunctionalElement = fe.GetFullFunctionalElement();
                }).DisposeWith(_cleanUp);

    }

    public int Index
    {
        get => _index;
        set => this.RaiseAndSetIfChanged(ref _index, value);
    }

    public string FunctionalGroup
    {
        get => _functionalGroup;
        set => this.RaiseAndSetIfChanged(ref _functionalGroup, value);
    }

    public string FunctionalElement
    {
        get => _functionalElement;
        set => this.RaiseAndSetIfChanged(ref _functionalElement, value);
    }
    
    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}