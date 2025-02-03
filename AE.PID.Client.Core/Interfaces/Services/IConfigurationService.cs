using System;
using System.Linq.Expressions;

namespace AE.PID.Client.Core;

public interface IConfigurationService : IDisposable
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