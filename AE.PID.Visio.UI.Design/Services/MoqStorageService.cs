using System.Diagnostics;
using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Visio.UI.Design;

internal sealed class MoqStorageService : IStorageService
{
    public Task SaveAsWorkbookAsync(string fileName, object data)
    {
        Debug.WriteLine("Exported");

        return Task.CompletedTask;
    }

    public void SaveAsJson<T>(string fileName, T data)
    {
        Debug.WriteLine("Saved");
    }
}