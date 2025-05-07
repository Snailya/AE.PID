using Microsoft.Extensions.DependencyInjection;

namespace AE.PID.Client.VisioAddIn;

public interface IServiceBridge
{
    /// <summary>
    /// Get the required service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T GetRequiredService<T>() where T : class;
    
    /// <summary>
    /// Create a scope
    /// </summary>
    /// <returns></returns>
    IServiceScope CreateScope();
    
    /// <summary>
    /// Get the scope for 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    IServiceScope GetScope(object obj);

    void ReleaseScope(object obj);
}