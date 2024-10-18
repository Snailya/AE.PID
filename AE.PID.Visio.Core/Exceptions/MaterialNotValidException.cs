namespace AE.PID.Visio.Core.Exceptions;

public class MaterialNotValidException : Exception
{
    public MaterialNotValidException(string code) : base(
        $"Unable to get the material with code {code}, it is either not exist or is not valid currently.")
    {
    }

    public MaterialNotValidException(string code, string message) : base(message)
    {
    }
}