using System;

namespace AE.PID.Models.Exceptions;

public class MasterNotFoundException
    : Exception
{
    public MasterNotFoundException(string baseId) : base($"Masters of BaseID: {baseId} not found in document stencil.")
    {
    }

    public MasterNotFoundException(string baseId, string filepath) : base(
        $"Masters of BaseID: {baseId} not found in {filepath}")
    {
    }
}