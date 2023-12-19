using System;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class FrequencyOptionViewModel : ReactiveObject
{
    public string Label { get; set; }
    public TimeSpan TimeSpan { get; set; }
}