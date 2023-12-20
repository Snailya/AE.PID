namespace AE.PID.Models;

public class PartItem
{
    /// <summary>
    /// A process zone is a group of functional group area in painting such as PT, ED
    /// </summary>
    public string ProcessZone { get; set; }

    /// <summary>
    /// A functional group is a combination of equipments that targets for the same propose, such as a pre-treatment group.
    /// </summary>
    public string FunctionalGroup { get; set; }

    /// <summary>
    /// A functional element is an indicator used in electric system for a part item.
    /// </summary>
    public string FunctionalElement { get; set; }

    /// <summary>
    /// The material number used in the system to get extra information about the part.
    /// </summary>
    public string MaterialNo { get; set; }

    /// <summary>
    /// The user friendly name of the part item.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// A merged string of specification and parameters and so on, which helps identify the part item.
    /// </summary>
    public string TechnicalData { get; set; }

    /// <summary>
    /// The number of the same part used in the source.
    /// </summary>
    public double Count { get; set; }
}