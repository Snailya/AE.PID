using System;

namespace AE.PID.Client.Core;

public class FunctionLocationNotValidException<T> : Exception
{
    public FunctionLocationNotValidException(T id) : base(
        $"Unable to get the function location with id {id}, it is either not exist or is not valid currently.")
    {
    }

    public FunctionLocationNotValidException(T id, string message) : base(message)
    {
    }
}