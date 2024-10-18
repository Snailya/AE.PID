namespace AE.PID.Visio.Core.Exceptions;

public class ProjectNotValidException : Exception
{
    public ProjectNotValidException(int id) : base(
        $"Unable to get the project with id {id}, it is either not exist or is not valid currently.")
    {
    }

    public ProjectNotValidException(int id, string message) : base(message)
    {
    }
}