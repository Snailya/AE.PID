using System;
using NLog;

namespace AE.PID.Controllers;

public static class LoggerExtension
{
    public static void LogUsefulException(this Logger logger, Exception ex)
    {
        // todo
        logger.Error(ex.Message);
    }
}