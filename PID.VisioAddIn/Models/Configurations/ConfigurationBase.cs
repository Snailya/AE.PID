using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace AE.PID.Models.Configurations;

public class ConfigurationBase : INotifyPropertyChanged
{
    private TimeSpan _checkInterval = TimeSpan.FromDays(1);
    private DateTime _nextTime = DateTime.Now;

    protected ConfigurationBase()
    {
        CheckIntervalSubject = new BehaviorSubject<TimeSpan>(_checkInterval);
    }

    /// <summary>
    ///     Observers can subscribe to the subject to receive the last (or initial) value of <see cref="CheckInterval" />.
    /// </summary>
    [JsonIgnore]
    public BehaviorSubject<TimeSpan> CheckIntervalSubject { get; }

    /// <summary>
    ///     The next time checking for app update.
    /// </summary>
    public DateTime NextTime
    {
        get => _nextTime;
        set => SetField(ref _nextTime, value);
    }

    /// <summary>
    ///     The check interval for app update.
    /// </summary>
    public TimeSpan CheckInterval
    {
        get => _checkInterval;
        set
        {
            if (SetField(ref _checkInterval, value)) CheckIntervalSubject.OnNext(_checkInterval);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}