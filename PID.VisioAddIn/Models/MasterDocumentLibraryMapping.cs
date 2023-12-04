namespace AE.PID.Models;

public class MasterDocumentLibraryMapping
{
    /// <summary>
    ///     The name of the master that used as ui identifier.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The base id of the master, used to locate the master in document stencil and library document
    /// </summary>
    public string BaseId { get; set; }

    /// <summary>
    ///     The path of the library that used to find the target master.
    /// </summary>
    public string LibraryPath { get; set; }
}