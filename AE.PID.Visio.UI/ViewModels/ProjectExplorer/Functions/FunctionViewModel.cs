using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionViewModel(Function source)
{
    public int Id { get; set; } = source.Id;
    public string Name { get; set; } = source.Name;
    public string Code { get; set; } = source.Code;
    public string EnglishName { get; set; } = source.EnglishName;
    public string Description { get; set; } = source.Description;
    public Function Source { get; set; } = source;
}