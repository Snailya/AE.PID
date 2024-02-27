using System.Collections.Generic;

namespace AE.PID.ViewModels;

public class DesignMaterialViewModel(string id, string name)
{
    public string Id { get; private set; } = id;

    public string Name { get; private set; } = name;

    public List<MaterialProperty> Properties { get; set; } = [];
}

public class MaterialProperty(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}