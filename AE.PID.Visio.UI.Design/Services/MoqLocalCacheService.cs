using System;
using System.Collections.Generic;
using AE.PID.Client.Core;

namespace AE.PID.Visio.UI.Design;

internal sealed class MoqLocalCacheService : ILocalCacheService
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public Material? GetMaterialByCode(string code)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Material> GetMaterials()
    {
        throw new NotImplementedException();
    }

    public Project? GetProjectById(int id)
    {
        return null;
    }

    public void Add(Project project)
    {
        throw new NotImplementedException();
    }

    public void AddRange(Material[] material)
    {
        throw new NotImplementedException();
    }

    public Function? GetFunctionById(int id)
    {
        throw new NotImplementedException();
    }
}