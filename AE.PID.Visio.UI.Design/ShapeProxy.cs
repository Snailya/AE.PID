namespace AE.PID.Visio.UI.Design;

public class ShapeProxy
{
    public int Id { get; set; }
    public int ParentId { get; set; }

    public int PDMSFunctionId { get; set; }

    public string ShapeCategory { get; set; }

    public string Zone { get; set; }
    public string ZoneName { get; set; }
    public string ZoneNameEnglish { get; set; }

    public string Group { get; set; }
    public string GroupName { get; set; }
    public string GroupNameEnglish { get; set; }

    public string Element { get; set; }

    public string MaterialType { get; set; }
    public string MaterialCode { get; set; }

    public string Description { get; set; }
    public string Remarks { get; set; }
}