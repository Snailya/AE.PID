using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.Core.Exceptions;

public class InvalidShapeSheetPropertyValueException(string propertyName, object value)
    : Exception($"{value} is not a valid value for {propertyName} property, please check and try again.")
{
}

public class FunctionLocationNotValidException : Exception
{
    public FunctionLocationNotValidException(CompositeId id) : base(
        $"Unable to get the function location with id {id}, it is either not exist or is not valid currently.")
    {
    }

    public FunctionLocationNotValidException(CompositeId id, string message) : base(message)
    {
    }
}