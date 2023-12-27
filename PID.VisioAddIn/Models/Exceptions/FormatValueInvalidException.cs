using System;

namespace AE.PID.Models.Exceptions;

public class FormatValueInvalidException(int shapeId, string rowName)
    : Exception($"Unable to get format result. Please check {shapeId}!{rowName}");