using AE.PID.Core.Models;

namespace AE.PID.Client.Core;

public class Function
{
    /// <summary>
    ///     The id of the function
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     As the function zone, function group is saved as different table in PDMS, so the id might overlap between different
    ///     function and the Type property matters.
    /// </summary>
    public FunctionType Type { get; set; }

    /// <summary>
    ///     The code for the function, e.g. P1PT1P01
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     The name for the function
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The English name for the function
    /// </summary>
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>
    ///     The description for the function
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Is this function in use
    /// </summary>
    public bool IsEnabled { get; set; }
}