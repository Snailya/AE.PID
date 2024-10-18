using System;
using System.Collections.Generic;
using System.Diagnostics;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.UI.Design.Design;
using DynamicData;

namespace AE.PID.Visio.UI.Design.Services;

public class MoqVisioService : IVisioService
{
    public MoqVisioService()
    {
        FunctionLocations = new Lazy<IObservableCache<FunctionLocation, CompositeId>>(() =>
            DesignData.FunctionLocations.AsObservableChangeSet(t => t.Id).AsObservableCache());
        MaterialLocations = new Lazy<IObservableCache<MaterialLocation, CompositeId>>(() =>
            DesignData.MaterialLocations.AsObservableChangeSet(t => t.LocationId).AsObservableCache());
    }

    public Lazy<IObservableCache<FunctionLocation, CompositeId>> FunctionLocations { get; }
    public Lazy<IObservableCache<MaterialLocation, CompositeId>> MaterialLocations { get; }
    public Lazy<IObservableCache<Symbol, string>> Symbols { get; }
    public void SelectAndCenterView(CompositeId id)
    {
        Debug.WriteLine($"Located {id}");
    }

    public string? GetDocumentProperty(string propName)
    {
        throw new NotImplementedException();
    }

    public string? GetPageProperty(int id, string propName)
    {
        throw new NotImplementedException();
    }

    public string? GetShapeProperty(CompositeId id, string propName)
    {
        throw new NotImplementedException();
    }

    public void UpdateDocumentProperties(IEnumerable<ValuePatch> patches)
    {
        throw new NotImplementedException();
    }

    public void UpdatePageProperties(int id, IEnumerable<ValuePatch> patches)
    {
        throw new NotImplementedException();
    }

    public void UpdateShapeProperties(CompositeId id, IEnumerable<ValuePatch> patches)
    {
        Debug.WriteLine("Updated");
    }

    public void PersistAsSolutionXml<TObject, TKey>(string keyword, TObject[] items, Func<TObject, TKey> keySelector,
        bool overwrite = false) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public T ReadFromSolutionXml<T>(string name)
    {
        throw new NotImplementedException();
    }

    public void InsertAsExcelSheet(string[,] toDataArray)
    {
        Debug.WriteLine("Inserted");
    }


    public void SaveAsSolutionXml<T>(string name, T data)
    {
        throw new NotImplementedException();
    }
}