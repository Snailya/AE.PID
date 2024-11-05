namespace AE.PID.Visio.Core.Exceptions;

public class ShapeSheetPropertyValueNotInvalidException(string propertyName, object value)
    : Exception($"{value} is not a valid value for {propertyName} property, please check and try again.")
{
}