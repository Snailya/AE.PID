using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AE.PID.Client.Core;
using AE.PID.Core;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class VisioSolutionXmlCacheService : ILocalCacheService, IEnableLogger
{
    private readonly Document _document;
    private readonly SourceCache<Function, int> _functionCache = new(x => x.Id);
    private readonly SourceCache<Material, string> _materialCache = new(x => x.Code);
    private readonly SourceCache<Project, int> _projectCache = new(x => x.Id);

    public VisioSolutionXmlCacheService(Document document)
    {
        _document = document;

        Initialize();
    }

    public void Dispose()
    {
        Save();
    }

    public void Save()
    {
        if (_projectCache.Count > 0)
            PersistAsSolutionXml("projects", _projectCache.Items.ToArray(), x => x.Id, true);

        if (_materialCache.Count > 0)
            PersistAsSolutionXml<Material, string>("materials", _materialCache.Items.ToArray(), x => x.Code);
    }

    public Material? GetMaterialByCode(string? code)
    {
        if (code == null || string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));

        var material = _materialCache.Lookup(code);
        return material.HasValue ? material.Value : null;
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

    public void Add(Project project)
    {
        _projectCache.AddOrUpdate(project);
    }

    public void AddRange(Material[] materials)
    {
        _materialCache.AddOrUpdate(materials);
    }

    public Function? GetFunctionById(int id)
    {
        var function = _functionCache.Lookup(id);
        return function.HasValue ? function.Value : null;
    }

    private void PersistAsSolutionXml<TObject, TKey>(string keyword, TObject[] items,
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
                    if (solutionItems.SingleOrDefault(x => Equals(keySelector(x), keySelector(item))) is { } original)
                        solutionItems.Replace(original, item);
                    else
                        solutionItems.Add(item);
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

    private T ReadFromSolutionXml<T>(string name)
    {
        return SolutionXmlHelper.Get<T>(_document, name);
    }
}