using System.Linq.Expressions;
using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.Core.Interfaces;

public interface IConfigurationService
{
    RuntimeConfiguration RuntimeConfiguration { get; }

    /// <summary>
    ///     The observable for configuration for the app.
    /// </summary>
    IObservable<Configuration> Configuration { get; }

    /// <summary>
    ///     The current configuration.
    /// </summary>
    /// <returns></returns>
    Configuration GetCurrentConfiguration();

    /// <summary>
    ///     Update the configuration by expression tree.
    /// </summary>
    /// <param name="propertyExpression"></param>
    /// <param name="newValue"></param>
    void UpdateProperty(Expression<Func<Configuration, object>> propertyExpression, object newValue);
}