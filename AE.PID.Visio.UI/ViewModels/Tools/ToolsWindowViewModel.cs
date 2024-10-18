using AE.PID.Visio.Core.Interfaces;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ToolsWindowViewModel : ViewModelBase
{
    public SelectToolViewModel SelectTool { get; }

    #region -- Constructors --

    internal ToolsWindowViewModel()
    {
    }

    public ToolsWindowViewModel(IToolService toolService)
    {
        SelectTool = new SelectToolViewModel(toolService);
    }

    #endregion
}