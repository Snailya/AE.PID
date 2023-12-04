using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using AE.PID.Controllers.Services;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class VersionUpdatePromptViewModel : ReactiveObject
{
    private readonly UpdateChecker _updateChekcer;
    private string _description;


    public VersionUpdatePromptViewModel(IEnumerable<JsonElement> stencilObjects)
    {
        //_updateChekcer = Globals.ThisAddIn.UpdateChecker;

        _description = string.Join(Environment.NewLine,
            stencilObjects.Select(x => $"{x.GetProperty("name")} {x.GetProperty("version")}"));

        // Update = ReactiveCommand.CreateFromTask(_updateChekcer.DoLibraryUpdate);
        NotNow = ReactiveCommand.Create(_updateChekcer.CloseVersionUpdatePromptWindow);
    }

    public ReactiveCommand<Unit, Unit> Update { get; }
    public ReactiveCommand<Unit, Unit> NotNow { get; }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }
}