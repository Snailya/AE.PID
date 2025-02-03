using System;

namespace AE.PID.Client.Core;

public class UrlNotValidException(string message) : Exception(message);