namespace AE.PID.Client.Core;

public static class CellDict
{
    public const string ShapeCategories = "User.msvShapeCategories";

    public const string Relationships = "Relationships";

    #region -- Projects --

    public const string ProjectId = "User.ProjectId";
    public const string ProjectCode = "User.ProjectCode";

    #endregion

    #region -- Material Locations --
    
    public const string Class = "Prop.Class";
    public const string SubClass = "Prop.SubClass";
    public const string KeyParameters = "User.KeyParameters";
    public const string UnitQuantity = "Prop.Quantity";
    public const string Quantity = "Prop.SubTotal";
    public const string MaterialCode = "Prop.D_BOM";

    #endregion

    #region -- Function Locations --

    public const string FunctionId = "User.FunctionId";

    public const string FunctionZone = "Prop.ProcessZone";
    public const string FunctionZoneName = "Prop.ProcessZoneName";
    public const string FunctionZoneEnglishName = "Prop.ProcessZoneEnglishName";

    public const string FunctionGroup = "Prop.FunctionalGroup";
    public const string FunctionGroupName = "Prop.FunctionalGroupName";
    public const string FunctionGroupEnglishName = "Prop.FunctionalGroupEnglishName";
    public const string FunctionGroupDescription = "Prop.FunctionalGroupDescription";

    public const string FunctionElement = "Prop.FunctionalElement";
    public const string ElementName = "Prop.Name";
    public const string Description = "Prop.Description";

    public const string Remarks = "Prop.Remarks";

    public const string Customer = "Prop.Customer";

    public const string RefEquipment = "Prop.RefEquipment";

    #endregion
}