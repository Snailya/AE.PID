namespace AE.PID.Client.Core.VisioExt.Control;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ElectricalControlSpecificationItem : Attribute
{
    public ElectricalControlSpecificationItem()
    {
    }

    public ElectricalControlSpecificationItem(string[] baseIDs)
    {
        BaseIDs = baseIDs;
    }

    public ElectricalControlSpecificationItem(string sheetName, string sectionName, string[] baseIDs)
    {
        SheetName = sheetName;
        SectionName = sectionName;
        BaseIDs = baseIDs;
    }

    public ElectricalControlSpecificationItem(string sheetName, string[] baseIDs)
    {
        SheetName = sheetName;
        BaseIDs = baseIDs;
    }

    public string? SheetName { get; set; }
    public string? SectionName { get; set; }
    public string[]? BaseIDs { get; }
}