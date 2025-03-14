using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using AE.PID.Client.Core;

namespace AE.PID.Visio.UI.Design.Services;

public class MoqConfigurationService : IConfigurationService
{
    private readonly BehaviorSubject<Configuration> _configurationSubject = new(new Configuration
    {
        Server = "http://localhost:32768",
        UserId = "6470",
        Stencils = []
    });

    private readonly Subject<(Expression<Func<Configuration, object>> PropertyExpression, object NewValue)>
        _updateSubject = new();

    public MoqConfigurationService()
    {
        var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        RuntimeConfiguration = new RuntimeConfiguration
        {
            ProductName = fvi?.ProductName ?? string.Empty,
            Version = fvi?.FileVersion ?? string.Empty,
            UUID = ""
        };
    }

    public RuntimeConfiguration RuntimeConfiguration { get; }
    public IObservable<Configuration> Configuration => _configurationSubject.AsObservable();

    public Configuration GetCurrentConfiguration()
    {
        return _configurationSubject.Value;
    }

    public void UpdateProperty(Expression<Func<Configuration, object>> propertyExpression, object newValue)
    {
        var configuration = (Configuration)_configurationSubject.Value.Clone();

        var propertyInfo = (PropertyInfo)((MemberExpression)propertyExpression.Body).Member;
        propertyInfo.SetValue(configuration, newValue);
    }

    public void Dispose()
    {
        _configurationSubject.Dispose();
        _updateSubject.Dispose();
    }
}