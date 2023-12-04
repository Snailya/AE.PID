using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AE.PID.Converters;

public class ConcurrentBagConverter<T>(JsonConverter<IEnumerable<T>> enumerableConverter)
    : JsonConverter<ConcurrentBag<T>>
{
    public override ConcurrentBag<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = enumerableConverter.Read(ref reader, typeof(IEnumerable<T>), options);
        return result is null ? null : new ConcurrentBag<T>(result);
    }

    public override void Write(Utf8JsonWriter writer, ConcurrentBag<T> value, JsonSerializerOptions options)
    {
        enumerableConverter.Write(writer, value, options);
    }
}

public class ConcurrentBagConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ConcurrentBag<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(CanConvert(typeToConvert));
        var elementType = typeToConvert.GetGenericArguments()[0];
        var ienumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var converterType = typeof(ConcurrentBagConverter<>).MakeGenericType(elementType);
        var ienumerableConverter = options.GetConverter(ienumerableType);
        return (JsonConverter)Activator.CreateInstance(converterType, ienumerableConverter)!;
    }
}