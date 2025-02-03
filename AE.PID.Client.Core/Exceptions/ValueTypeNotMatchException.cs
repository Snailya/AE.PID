using System;

namespace AE.PID.Client.Core;

public class ValueTypeNotMatchException(string propertyName, object value)
    : Exception($"{value} is not a valid value for {propertyName} property, please check and try again.")
{
}