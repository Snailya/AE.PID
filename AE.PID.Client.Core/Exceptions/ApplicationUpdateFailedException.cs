using System;

namespace AE.PID.Client.Core;

public class ApplicationUpdateFailedException(string message) : Exception(message)
{
}