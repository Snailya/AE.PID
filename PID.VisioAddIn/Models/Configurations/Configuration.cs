using System;

namespace AE.PID.Models.Configurations;

[Serializable]
public class Configuration
{
    public DateTime NextTime { get; set; } = DateTime.Today;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromDays(1);

    public LibraryConfiguration LibraryConfiguration { get; set; } = new();
}