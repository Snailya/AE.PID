using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using AE.PID.Models;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;

namespace AE.PID.Tools;

public static class BindingMixins
{
    public static IDisposable OneWayBind<TModel, TProperty>(this Shape shape, TModel model,
        Expression<Func<TModel, TProperty>> mProperty, string cellName,
        Func<string, TProperty?>? visioToModelConverterOverride = null)
    {
        var visioPropertyToPropertyConverter =
            visioToModelConverterOverride ?? (value => (TProperty)Convert.ChangeType(value, typeof(TProperty)));

        var vmExpression = Reflection.Rewrite(mProperty.Body);

        return (shape.CellExistsN(cellName, VisExistsFlags.visExistsAnywhere)
                ? Observable.Return(shape.Cells[cellName])
                : Observable.Empty<Cell>())
            .Merge(Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                    handler => shape.CellChanged += handler,
                    handler => shape.CellChanged -= handler)
                .Where(x => x.Name == cellName))
            .Select(x => x.TryGetFormatValue() ?? string.Empty)
            .Select(visioPropertyToPropertyConverter)
            .Subscribe(value =>
            {
                Reflection.TrySetValueToPropertyChain(model, vmExpression.GetExpressionChain(), value!);
            });
    }

    public static IDisposable Bind<TModel, TMProperty>(this Shape shape, TModel model,
        Expression<Func<TModel, TMProperty>> mProperty, string visioProp,
        Func<string, TMProperty?>? visioToModelConverterOverride = null,
        Func<TMProperty?, string>? modelToVisioConverterOverride = null) where TModel : INotifyPropertyChanged
    {
        EnsureVisioPropertyCreated(shape, visioProp);

        var visioToModelConverter =
            visioToModelConverterOverride ?? (value => (TMProperty)Convert.ChangeType(value, typeof(TMProperty)));
        var modelToVisioConverter =
            modelToVisioConverterOverride ?? (value => (string)Convert.ChangeType(value, typeof(string)));

        var vmExpression = Reflection.Rewrite(mProperty.Body);

        // setup initial value if exist
        if (shape.CellExistsN(visioProp, VisExistsFlags.visExistsAnywhere))
        {
            var rawValue = shape.Cells[visioProp].TryGetFormatValue() ?? string.Empty;
            var value = visioToModelConverter(rawValue);

            Reflection.TrySetValueToPropertyChain(model, vmExpression.GetExpressionChain(), value!);
        }

        var d = new CompositeDisposable();
        // observe to visio property change
        var visioObservable = Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                handler => shape.CellChanged += handler,
                handler => shape.CellChanged -= handler)
            .Where(x => x.Name == visioProp)
            .Select(x => x.TryGetFormatValue()) // get value
            .Select(visioToModelConverter) // convert to TMProperty
            .DistinctUntilChanged()
            .Select(value => new ChangeProxy<TMProperty>(ChangeSender.Visio, value));

        // observe the model property change to synchronize from model to visio
        var modelObservable = model.ObservableForProperty(mProperty, skipInitial: true)
            .DistinctUntilChanged()
            .Select(x => new ChangeProxy<TMProperty>(ChangeSender.Model, x.Value));

        visioObservable
            .Merge(modelObservable)
            .Scan(new Change<TMProperty>(),
                (acc, current) =>
                    new Change<TMProperty> { Previous = acc.Previous, Current = current }
            )
            .Where(tuple => !Equals(tuple.Previous, tuple.Current))
            .Select(change => change.Current)
            .WhereNotNull()
            .Subscribe(proxy =>
            {
                switch (proxy.Sender)
                {
                    case ChangeSender.Visio:
                        Reflection.TrySetValueToPropertyChain(model, vmExpression.GetExpressionChain(), proxy.Value!);
                        break;
                    case ChangeSender.Model:
                    {
                        AppScheduler.VisioScheduler.Schedule(() =>
                        {
                            var value = (modelToVisioConverter(proxy.Value) ?? string.Empty).ClearFormat(shape,
                                visioProp);
                            shape.Cells[visioProp].UpdateIfChanged(value);
                        });

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            })
            .DisposeWith(d);

        return d;
    }

    private static void EnsureVisioPropertyCreated(Shape shape, string visioProp)
    {
        // ensure the property exist
        if (shape.CellExistsN(visioProp, VisExistsFlags.visExistsAnywhere))
            return;

        if (visioProp == "Prop.D_BOM")
        {
            var shapeData = new ShapeData("D_BOM", "设计物料", "", "");
            shape.CreateOrUpdate(shapeData);
            return;
        }

        throw new NotImplementedException();
    }

    /// <summary>
    ///     Clear the format return the origin string.
    /// </summary>
    /// <param name="formatValue"></param>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    private static string ClearFormat(this string formatValue, IVShape shape, string propName)
    {
        var format = shape.Cells[propName].ContainingRow.CellU[VisCellIndices.visCustPropsFormat]
            .ResultStr[VisUnitCodes.visUnitsString];
        if (string.IsNullOrEmpty(format)) return formatValue;

        var pattern = Regex.Replace(format, @"(\\.)|(@)|(0\.[#0]+)|(#\\)", match =>
        {
            if (match.Groups[1].Success)
                return match.Groups[1].Value.Substring(1); // Replace \\char with char

            if (match.Groups[2].Success)
                return @"(\w+)"; // Replace @ with the \w+

            if (match.Groups[3].Success)
                return @"(\d+\.\d+)"; // Handle other numeric patterns

            if (match.Groups[4].Success)
                return @"(\d+)";

            return match.Value;
        });

        var result = Regex.Match(formatValue, pattern).Groups[1].Value;
        return result;
    }

    private enum ChangeSender
    {
        Visio,
        Model
    }

    private class ChangeProxy<TMProperty>(ChangeSender sender, TMProperty? value)
    {
        public ChangeSender Sender { get; } = sender;
        public TMProperty? Value { get; } = value;
    }

    private class Change<TMProperty>
    {
        public ChangeProxy<TMProperty>? Previous { get; set; }
        public ChangeProxy<TMProperty>? Current { get; set; }
    }
}