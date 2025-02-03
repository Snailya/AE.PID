using AE.PID.Client.Core.VisioExt;
using AE.PID.UI.Shared;

namespace AE.PID.UI.Avalonia.VisioExt;

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