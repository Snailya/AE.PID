using Splat;

namespace AE.PID.Client.UI.Avalonia.Shared;

public abstract class ViewModelLocator
{
    /// <summary>
    ///     Resolve the parameters for the constructor with reflection to reduce the code when creating instance.
    /// </summary>
    /// <param name="parameters"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static TViewModel Create<TViewModel>(params object[] parameters)
        where TViewModel : ViewModelBase
    {
        var viewModelType = typeof(TViewModel);
        var constructor = viewModelType.GetConstructors().FirstOrDefault(x => x.IsPublic);
        if (constructor == null)
            throw new InvalidOperationException($"No suitable constructor found for {viewModelType}");

        var constructorParameters = constructor.GetParameters();
        var resolvedParameters = constructorParameters.Select(param =>
            {
                // first try to solve it from service collection
                var service = Locator.Current.GetService(param.ParameterType);
                if (service != null)
                    return service;

                // if it is not in the service collection, consider it as a plain parameter passed in
                var matchingParam = parameters.FirstOrDefault(p => p.GetType() == param.ParameterType);
                if (matchingParam == null) return null;

                parameters = parameters.Where(p => p != matchingParam).ToArray();
                return matchingParam;
            }
        ).ToArray();

        return (TViewModel)constructor.Invoke(resolvedParameters);
    }
}