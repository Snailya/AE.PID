using System.Linq;
using System.Reflection;

namespace AE.PID.Visio.Shared.Extensions;

public static class CompareExt
{
    public static PropertyInfo[] DiffWith<T>(this T current, T? compareTo)
    {
        if (compareTo == null)
            return current!.GetType().GetProperties();

        return (from propertyInfo in typeof(T).GetProperties()
            let thisValue = propertyInfo.GetValue(current)
            let otherValue = propertyInfo.GetValue(compareTo)
            where thisValue != otherValue
            select propertyInfo).ToArray();
    }
}