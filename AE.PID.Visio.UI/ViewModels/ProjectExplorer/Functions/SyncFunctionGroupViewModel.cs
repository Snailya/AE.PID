namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SyncFunctionGroupViewModel(FunctionViewModel? server, FunctionViewModel? client)
{
    public enum SyncStatus
    {
        Added,
        Removed,
        Updated
    }

    public FunctionViewModel? Client { get; } = client;
    public FunctionViewModel? Server { get; } = server;

    public SyncStatus Status =>
        Server is null ? SyncStatus.Added : Client is null ? SyncStatus.Removed : SyncStatus.Updated;
}