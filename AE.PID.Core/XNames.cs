using System.Xml.Linq;

namespace AE.PID.Core;

public abstract class XNames
{
    #region XElements

    public static readonly XName ShapesElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Shapes");

    public static readonly XName ShapeElement = XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Shape");

    public static readonly XName SectionElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Section");

    public static readonly XName RowElement = XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Row");
    public static readonly XName CellElement = XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Cell");
    public static readonly XName TextElement = XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Text");

    public static readonly XName VisioDocumentElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}VisioDocument");

    public static readonly XName StyleSheetsElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}StyleSheets");

    public static readonly XName StyleSheetElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}StyleSheet");

    public static readonly XName MasterElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Master");

    public static readonly XName MasterContentsElement =
        XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}MasterContents");

    public static readonly XName RelElement = XName.Get("{http://schemas.microsoft.com/office/visio/2012/main}Rel");

    #endregion

    #region XAttributes

    public static readonly XName IndexAttribute = XName.Get("IX");

    public static readonly XName NameAttribute = XName.Get("Name");
    public static readonly XName NameUAttribute = XName.Get("NameU");

    public static readonly XName IdAttribute = XName.Get("ID");
    public static readonly XName BaseIdAttribute = XName.Get("BaseID");
    public static readonly XName UniqueIdAttribute = XName.Get("UniqueID");

    public static readonly XName NAttribute = XName.Get("N");
    public static readonly XName VAttribute = XName.Get("V");
    public static readonly XName FAttribute = XName.Get("F");

    public static readonly XName DelAttribute = XName.Get("Del");

    public static readonly XName LineStyleAttribute = XName.Get("LineStyle");
    public static readonly XName FillStyleAttribute = XName.Get("FillStyle");
    public static readonly XName TextStyleAttribute = XName.Get("TextStyle");

    public static readonly XName TypeAttribute = XName.Get("Type");

    public static readonly XName MasterAttribute = XName.Get("Master");
    public static readonly XName MasterShapeAttribute = XName.Get("MasterShape");

    public static readonly XName RelIdAttribute =
        XName.Get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id");

    #endregion
}