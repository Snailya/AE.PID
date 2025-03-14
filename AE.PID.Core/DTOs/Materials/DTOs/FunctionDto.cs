namespace AE.PID.Core.DTOs;

public class FunctionDto
{
    /// <summary>
    ///     The id of the function, please note this is might be overlap if for different function type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     As the function zone, a function group is saved as different table in PDMS, so the id might overlap between
    ///     different
    ///     function and the Type property matters.
    /// </summary>
    public FunctionType FunctionType { get; set; }

    /// <summary>
    ///     The code for the function, e.g. P1PT1P01
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     The name for the function.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The English name for the function.
    /// </summary>
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>
    ///     The description for the function.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}