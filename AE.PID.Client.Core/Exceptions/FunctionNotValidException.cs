using System;

namespace AE.PID.Client.Core;

public class FunctionNotValidException : Exception
{
    public FunctionNotValidException(int id) : base(
        $"Unable to get the function with id {id}, it is either not exist or is not valid currently.")
    {
    }

    public FunctionNotValidException(int id, string message) : base(message)
    {
    }
}