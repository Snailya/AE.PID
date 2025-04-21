namespace AE.PID.Server;

public static class HttpContextExt
{
    public static string? GetClientIp(this HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip)) ip = context.Connection.RemoteIpAddress?.ToString();
        return ip;
    }
}