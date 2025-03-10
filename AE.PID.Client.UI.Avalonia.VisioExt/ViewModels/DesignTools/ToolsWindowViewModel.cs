using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.UI.Avalonia.Shared;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

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