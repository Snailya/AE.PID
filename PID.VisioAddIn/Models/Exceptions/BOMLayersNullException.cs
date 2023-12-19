using System;

namespace AE.PID.Models.Exceptions;

public class BOMLayersNullException()
    : Exception("When exporting BOM, the layers set in ae-pid.json can not be empty.");