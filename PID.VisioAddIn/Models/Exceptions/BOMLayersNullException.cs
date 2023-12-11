using System;

namespace AE.PID.Models.Exceptions;

public class BOMLayersNullException
    : Exception
{
    public BOMLayersNullException() : base("When exporting BOM, the layers set in ae-pid.json can not be empty.")
    {
    }
}