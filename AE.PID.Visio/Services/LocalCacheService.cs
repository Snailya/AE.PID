using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;
using AE.PID.Visio.Helpers;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Services;

public class LocalCacheService : ILocalCacheService, IEnableLogger
{
    private readonly Document _document;
    private readonly SourceCache<Material, string> _materialCache = new(x => x.Code);
    private readonly SourceCache<Project, int> _projectCache = new(x => x.Id);


    public LocalCacheService(Document document)
    {
        _document = document;

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            _materialCache.AddOrUpdate(ReadFromSolutionXml<List<Material>>("materials"));
        }
        catch (FileNotFoundException e)
        {
        }
    }

    public void Dispose()
    {
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public Material? GetMaterialByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));
        
        var material = _materialCache.Lookup(code);
        return material.HasValue ? material.Value : null;
    }

    public void PersistAsSolutionXml<TObject, TKey>(string keyword, TObject[] items,
        Func<TObject, TKey> keySelector, bool overwrite = false)
        where TKey : notnull
    {
        List<TObject>? solutionItems;

        if (!overwrite)
            try
            {
                // replace the origin project xml
                solutionItems = ReadFromSolutionXml<List<TObject>>(keyword);

                foreach (var item in items)
                    solutionItems.ReplaceOrAdd(
                        solutionItems.SingleOrDefault(x => Equals(keySelector(x), keySelector(item))), item);
            }
            catch (FileNotFoundException e)
            {
                // or create a new one
                solutionItems = items.ToList();
            }
        else
            solutionItems = items.ToList();


        // persist
        var element = new SolutionXmlElement<List<TObject>>
        {
            Name = keyword,
            Data = solutionItems
        };
        SolutionXmlHelper.Store(_document, element);

        this.Log().Info($"{items.Length} items saved with keyword {keyword} as solution xml.");
    }

    public IEnumerable<Material> GetMaterials()
    {
        return _materialCache.Items;
    }

    public Project? GetProjectById(int id)
    {
        var project = _projectCache.Lookup(id);
        return project.HasValue ? project.Value : null;
    }

    private T ReadFromSolutionXml<T>(string name)
    {
        return SolutionXmlHelper.Get<T>(_document, name);
    }
}