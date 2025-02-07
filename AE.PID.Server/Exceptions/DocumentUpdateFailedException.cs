namespace AE.PID.Server.Exceptions;

public class DocumentUpdateFailedException(string message) : Exception(message)
{
}

public class PagePartFailedException(string message) : Exception(message)
{
}

public class MasterPartFailedException(string message) : Exception(message)
{
}