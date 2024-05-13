using ReactiveUI;

namespace AE.PID.ViewModels;

public class DocumentInfoViewModel : ViewModelBase
{
    private string _customerName = string.Empty;
    private string _documentNo = string.Empty;
    private string _projectNo = string.Empty;
    private string _versionNo = string.Empty;

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
}