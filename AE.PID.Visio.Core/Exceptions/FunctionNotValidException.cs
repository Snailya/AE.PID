namespace AE.PID.Visio.Core.Exceptions;

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